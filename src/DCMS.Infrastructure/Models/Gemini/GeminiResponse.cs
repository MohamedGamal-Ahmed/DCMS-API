using System.Text.Json.Serialization;

namespace DCMS.Infrastructure.Models.Gemini;

public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; set; }
    
    [JsonPropertyName("promptFeedback")]
    public GeminiPromptFeedback? PromptFeedback { get; set; }
}

public class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }
    
    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }
    
    [JsonPropertyName("index")]
    public int Index { get; set; }
}

public class GeminiPromptFeedback
{
    [JsonPropertyName("blockReason")]
    public string? BlockReason { get; set; }
    
    [JsonPropertyName("safetyRatings")]
    public List<GeminiSafetyRating>? SafetyRatings { get; set; }
}

public class GeminiSafetyRating
{
    [JsonPropertyName("category")]
    public string? Category { get; set; }
    
    [JsonPropertyName("probability")]
    public string? Probability { get; set; }
}

public class GeminiErrorResponse
{
    [JsonPropertyName("error")]
    public GeminiError? Error { get; set; }
}

public class GeminiError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
