using System.Text.Json.Serialization;

namespace DCMS.Infrastructure.Models.Groq;

public class GroqResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("choices")]
    public List<GroqChoice> Choices { get; set; } = new();
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("usage")]
    public GroqUsage? Usage { get; set; }
}

public class GroqChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("message")]
    public GroqMessage Message { get; set; } = new();
    
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public class GroqUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public class GroqErrorResponse
{
    [JsonPropertyName("error")]
    public GroqError? Error { get; set; }
}

public class GroqError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("code")]
    public string? Code { get; set; }
}
