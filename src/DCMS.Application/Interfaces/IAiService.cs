using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DCMS.Application.Interfaces;

public interface IAiService
{
    Task<AiResponse> GetResponseAsync(string userPrompt, List<ChatMessage> history, Action<string>? onStatusUpdate = null);
    Task<AiResponse> GetResponseAsync(string userPrompt, List<ChatMessage> history, string systemPrompt, Action<string>? onStatusUpdate = null);
    IAsyncEnumerable<string> GetResponseStreamAsync(string userPrompt, List<ChatMessage> history, Action<string>? onStatusUpdate = null);
}

public class AiResponse
{
    public string Content { get; set; } = string.Empty;
    public int LogId { get; set; }
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
}
