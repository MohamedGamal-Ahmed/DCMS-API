using System;

namespace DCMS.Domain.Entities;

public class AiRequestLog
{
    public int Id { get; set; }
    public string UserPrompt { get; set; } = string.Empty;
    public string AiResponse { get; set; } = string.Empty;
    public string? ActionExecuted { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int SecondsSaved { get; set; }
    public bool IsSuccess { get; set; } = true;
    public bool? UserFeedback { get; set; } // true = 👍, false = 👎
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? UserId { get; set; }
    public User? User { get; set; }
}
