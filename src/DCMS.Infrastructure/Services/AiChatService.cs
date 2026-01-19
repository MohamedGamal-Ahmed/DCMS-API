using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DCMS.Application.Interfaces;
using DCMS.Domain.Entities;
using DCMS.Infrastructure.Data;
using DCMS.Infrastructure.Models.Groq;
using DCMS.Infrastructure.Ai.Plugins;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DCMS.Infrastructure.Services;

public class AiChatService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelId;
    private readonly ICorrespondenceService _correspondenceService;
    private readonly IMeetingService _meetingService;
    private readonly IAiHistoryService _historyService;
    private readonly IAiContextService _contextService;

    public AiChatService(
        IConfiguration configuration,
        ICorrespondenceService correspondenceService,
        IMeetingService meetingService,
        IAiHistoryService historyService,
        IAiContextService contextService)
    {
        _correspondenceService = correspondenceService;
        _meetingService = meetingService;
        _historyService = historyService;
        _contextService = contextService;

        _apiKey = configuration["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq API Key not configured");
        _modelId = configuration["Groq:ModelId"] ?? "llama-3.3-70b-versatile";

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.groq.com/openai/v1/"),
            Timeout = TimeSpan.FromMinutes(5)
        };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<AiResponse> GetResponseAsync(string userPrompt, List<ChatMessage> history, Action<string>? onStatusUpdate = null)
    {
        return await GetResponseInternalAsync(userPrompt, history, _modelId, onStatusUpdate);
    }

    public async Task<AiResponse> GetResponseAsync(string userPrompt, List<ChatMessage> history, string systemPrompt, Action<string>? onStatusUpdate = null)
    {
        return await GetResponseInternalAsync(userPrompt, history, _modelId, onStatusUpdate, false, systemPrompt);
    }

    private async Task<AiResponse> GetResponseInternalAsync(string userPrompt, List<ChatMessage> history, string modelId, Action<string>? onStatusUpdate = null, bool isRetry = false, string? systemPrompt = null)
    {
        try
        {
            onStatusUpdate?.Invoke(isRetry ? "جاري استخدام المحرك الاحتياطي..." : "جاري التفكير...");
            
            var messages = await ConvertHistoryToMessagesAsync(history);
            
            // Inject system prompt if provided
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Insert(0, new GroqMessage { Role = "system", Content = systemPrompt });
            }
            
            messages.Add(new GroqMessage
            {
                Role = "user",
                Content = userPrompt
            });

            var request = new GroqRequest
            {
                Model = modelId,
                Messages = messages,
                Tools = BuildToolDefinitions(),
                ToolChoice = "auto",
                Temperature = 0.1,
                MaxTokens = 1024
            };

            System.Diagnostics.Debug.WriteLine($"[GROQ] Sending request to {modelId}");

            var response = await _httpClient.PostAsJsonAsync("chat/completions", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[GROQ ERROR] {errorContent}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && !isRetry)
                {
                    // Fallback to a lighter model if rate limited
                    var fallbackModel = "llama-3.1-8b-instant";
                    return await GetResponseInternalAsync(userPrompt, history, fallbackModel, onStatusUpdate, true);
                }

                var error = JsonSerializer.Deserialize<GroqErrorResponse>(errorContent);
                return new AiResponse 
                { 
                    Content = $"خطأ في الاتصال: {error?.Error?.Message ?? response.StatusCode.ToString()}",
                    LogId = 0 
                };
            }

            var groqResponse = await response.Content.ReadFromJsonAsync<GroqResponse>();
            var choice = groqResponse?.Choices?.FirstOrDefault();

            if (choice == null)
            {
                return new AiResponse { Content = "عذراً، لم أتمكن من معالجة طلبك.", LogId = 0 };
            }

            int promptTokens = 0;
            int completionTokens = 0;
            int totalSecondsSaved = 0;
            var executedTools = new List<string>();

            if (groqResponse?.Usage != null)
            {
                promptTokens = groqResponse.Usage.PromptTokens;
                completionTokens = groqResponse.Usage.CompletionTokens;
            }

            if (choice.Message.ToolCalls != null && choice.Message.ToolCalls.Count > 0)
            {
                messages.Add(choice.Message); 

                foreach (var toolCall in choice.Message.ToolCalls)
                {
                    var functionName = toolCall.Function.Name;
                    executedTools.Add(functionName);
                    
                    totalSecondsSaved += CalculateSecondsSaved(functionName, string.Empty);

                    onStatusUpdate?.Invoke("جاري البحث في قاعدة البيانات...");
                    var functionResult = await ExecuteFunctionAsync(
                        functionName,
                        toolCall.Function.Arguments);

                    messages.Add(new GroqMessage
                    {
                        Role = "tool",
                        ToolCallId = toolCall.Id,
                        Content = JsonSerializer.Serialize(functionResult)
                    });
                }

                // CHECK: If tools were creation tools, we might want to inject a custom message
                if (executedTools.Any(t => t.StartsWith("Create")))
                {
                    messages.Add(new GroqMessage { Role = "system", Content = "The record has been created successfully. Tell the user it is done and provide the link." });
                }

                // Call Groq again with tool results - Use the CURRENT modelId
                var finalRequest = new GroqRequest
                {
                    Model = modelId,
                    Messages = messages,
                    Temperature = 0.1,
                    MaxTokens = 1024
                };

                onStatusUpdate?.Invoke("جاري صياغة الرد...");
                var finalResponse = await _httpClient.PostAsJsonAsync("chat/completions", finalRequest);
                
                if (!finalResponse.IsSuccessStatusCode)
                {
                    var errorDetails = await finalResponse.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[GROQ SECOND CALL ERROR] {errorDetails}");

                    // SECOND CALL FALLBACK: If the final summarization fails due to limits, retry with lighter model
                    if (finalResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests && !isRetry)
                    {
                        var fallbackModel = "llama-3.1-8b-instant";
                        System.Diagnostics.Debug.WriteLine($"[GROQ FALLBACK] Retrying second call with {fallbackModel}");
                        onStatusUpdate?.Invoke("جاري استخدام المحرك الاحتياطي لتلخيص الرد...");
                        
                        finalRequest.Model = fallbackModel;
                        finalResponse = await _httpClient.PostAsJsonAsync("chat/completions", finalRequest);
                    }
                }

                if (finalResponse.IsSuccessStatusCode)
                {
                    var finalGroqResponse = await finalResponse.Content.ReadFromJsonAsync<GroqResponse>();
                    var finalChoice = finalGroqResponse?.Choices.FirstOrDefault();
                    
                    if (finalGroqResponse?.Usage != null)
                    {
                        promptTokens += finalGroqResponse.Usage.PromptTokens;
                        completionTokens += finalGroqResponse.Usage.CompletionTokens;
                    }

                    if (finalChoice?.Message.Content != null)
                    {
                        var finalResponseText = finalChoice.Message.Content;
                        var logId = await _historyService.LogAiRequestAsync(userPrompt, finalResponseText, string.Join(", ", executedTools), promptTokens, completionTokens, totalSecondsSaved, true);
                        return new AiResponse { Content = finalResponseText, LogId = logId };
                    }
                }
                
                return new AiResponse 
                { 
                    Content = "عذراً، انتهت حصة الاستخدام (Usage Limit) أو حدث خطأ أثناء صياغة الرد النهائي. يرجى المحاولة لاحقاً.", 
                    LogId = 0 
                };
            }

            var responseText = choice.Message.Content ?? "عذراً، لم أتمكن من معالجة طلبك.";
            bool isSuccess = !responseText.Contains("عذراً، تخصصي");

            var finalLogId = await _historyService.LogAiRequestAsync(userPrompt, responseText, "None", promptTokens, completionTokens, 0, isSuccess);
            return new AiResponse { Content = responseText, LogId = finalLogId };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GROQ ERROR] {ex.Message}");
            return new AiResponse { Content = $"عذراً، حدث خطأ في الاتصال بالذكاء الاصطناعي: {ex.Message}", LogId = 0 };
        }
    }

    private int CalculateSecondsSaved(string toolName, string responseContent)
    {
        return toolName switch
        {
            "SearchCorrespondences" or "SearchMeetings" => 120, // 2 minutes
            "GetCorrespondenceDetails" or "GetMeetingDetails" => 300, // 5 minutes (summarization)
            "CategorizeCorrespondence" => 180, // 3 minutes
            _ => 30 // General chat
        };
    }



    public async IAsyncEnumerable<string> GetResponseStreamAsync(string userPrompt, List<ChatMessage> history, Action<string>? onStatusUpdate = null)
    {
        // For now, use non-streaming
        var result = await GetResponseAsync(userPrompt, history, onStatusUpdate);
        yield return result.Content;
    }

    private async Task<List<GroqMessage>> ConvertHistoryToMessagesAsync(List<ChatMessage> history)
    {
        var systemPrompt = await _contextService.GetSystemPromptAsync();
        
        var messages = new List<GroqMessage>
        {
            new GroqMessage
            {
                Role = "system",
                Content = systemPrompt
            }
        };

        // Limit to last 10 history items for efficiency
        var recentHistory = history.TakeLast(10);
        foreach (var msg in recentHistory)
        {
            messages.Add(new GroqMessage
            {
                Role = msg.Role == "user" ? "user" : "assistant",
                Content = msg.Content
            });
        }

        return messages;
    }

    private List<GroqTool> BuildToolDefinitions()
    {
        // Tools are disabled for now as per user request to avoid complexity.
        return new List<GroqTool>();
    }

    private async Task<object> ExecuteFunctionAsync(string functionName, string argumentsJson)
    {
        System.Diagnostics.Debug.WriteLine($"[GROQ] Executing: {functionName}");
        System.Diagnostics.Debug.WriteLine($"[GROQ] Args: {argumentsJson}");

        if (functionName == "SearchCorrespondences")
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);

            var query = GetArgString(args, "query");
            var recordType = GetArgString(args, "recordType");
            var status = GetArgString(args, "status");
            var startDate = GetArgString(args, "startDate");
            var endDate = GetArgString(args, "endDate");
            var entity = GetArgString(args, "entity");

            var plugin = new CorrespondencePlugin(_correspondenceService);
            var searchResults = await plugin.SearchCorrespondences(query, recordType, status, startDate, endDate, entity);

            System.Diagnostics.Debug.WriteLine($"[GROQ] Found {searchResults.Count} results");
            return new { results = searchResults, count = searchResults.Count };
        }
        else if (functionName == "GetCorrespondenceDetails")
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
            
            var idStr = "0";
            if (args != null && args.TryGetValue("id", out var idElement))
            {
                if (idElement.ValueKind == JsonValueKind.Number) idStr = idElement.GetInt32().ToString();
                else if (idElement.ValueKind == JsonValueKind.String) idStr = idElement.GetString() ?? "0";
            }

            var type = GetArgString(args, "type") ?? "Inbound";

            var plugin = new CorrespondencePlugin(_correspondenceService);
            var result = await plugin.GetCorrespondenceDetails(0, type); // We actually need to fix plugin too or call service direct
            
            // To be more robust, call service directly since plugin expects int
            var resultDto = await _correspondenceService.GetByIdAsync(idStr, type);

            return new { result = resultDto };
        }
        else if (functionName == "SearchMeetings")
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);

            var query = GetArgString(args, "query");
            DateTime? start = null;
            if (DateTime.TryParse(GetArgString(args, "startDate"), out var s)) start = s;
            DateTime? end = null;
            if (DateTime.TryParse(GetArgString(args, "endDate"), out var e)) end = e;
            var location = GetArgString(args, "location");
            var attendee = GetArgString(args, "attendee");
            var project = GetArgString(args, "project");

            var meetingResults = await _meetingService.SearchMeetingsAsync(query, start, end, location, attendee, project);
            return new { results = meetingResults, count = meetingResults.Count };
        }
        else if (functionName == "GetMeetingDetails")
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
            var id = 0;
            if (args != null && args.TryGetValue("id", out var idElement))
            {
                if (idElement.ValueKind == JsonValueKind.Number) id = idElement.GetInt32();
                else if (idElement.ValueKind == JsonValueKind.String && int.TryParse(idElement.GetString(), out var parsedId)) id = parsedId;
            }

            var meeting = await _meetingService.GetByIdAsync(id);
            return new { meeting };
        }
        else if (functionName == "CategorizeCorrespondence")
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
            var subject = GetArgString(args, "subject");
            
            // AI-driven categorization helper
            return new { 
                suggestedCategories = new[] { "Posta", "Email", "Contract", "Complaint", "Request", "Custody" },
                suggestedEngineers = new[] { "Eng. Nada", "Eng. Azza", "Eng. Karam", "Eng. Hadeer", "Eng. Engy" },
                note = "Recommend the most appropriate category and engineer based on the subject keywords."
            };
        }
        else if (functionName == "CreateInboundCorrespondence")
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
            var id = await _correspondenceService.CreateInboundAsync(
                GetArgString(args, "subject") ?? "موضوع جديد",
                GetArgString(args, "code") ?? "N/A",
                GetArgString(args, "fromEntity") ?? "جهة غير معروفة",
                GetArgString(args, "assignedEngineer") ?? "غير محدد"
            );
            return new { success = true, id, type = "Inbound", link = $"record://inbound/{id}" };
        }
        else if (functionName == "CreateOutboundCorrespondence")
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
            var id = await _correspondenceService.CreateOutboundAsync(
                GetArgString(args, "subject") ?? "صادر جديد",
                GetArgString(args, "code") ?? "N/A",
                GetArgString(args, "toEntity") ?? "جهة غير معروفة"
            );
            return new { success = true, id, type = "Outbound", link = $"record://outbound/{id}" };
        }
        else if (functionName == "CreateMeeting")
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
            var dateStr = GetArgString(args, "date") ?? DateTime.Today.ToString("yyyy-MM-dd");
            var timeStr = GetArgString(args, "time") ?? "10:00";
            
            if (DateTime.TryParse($"{dateStr} {timeStr}", out var start))
            {
                var id = await _meetingService.CreateMeetingAsync(
                    GetArgString(args, "title") ?? "اجتماع جديد",
                    start,
                    GetArgString(args, "location") ?? "المكتب",
                    GetArgString(args, "description") ?? ""
                );
                return new { success = true, id, link = $"record://meeting/{id}" };
            }
            return new { error = "Invalid date/time format" };
        }

        return new { error = "Unknown function" };
    }

    private string? GetArgString(Dictionary<string, JsonElement>? args, string key)
    {
        if (args != null && args.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }
        return null;
    }
}


