namespace DCMS.Domain.Entities;

public class Email
{
    public int Id { get; set; }
    public string? FromEmail { get; set; }
    public string? ToEmail { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Body { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string? ResponsibleEngineer { get; set; }
    public string Status { get; set; } = "New";
    public List<string> AttachmentUrls { get; set; } = new();
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? CreatedByUser { get; set; }
}
