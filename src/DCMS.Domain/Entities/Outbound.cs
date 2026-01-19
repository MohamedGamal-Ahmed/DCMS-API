using DCMS.Domain.Enums;

namespace DCMS.Domain.Entities;

public class Outbound
{
    public int Id { get; set; }
    public string SubjectNumber { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? ToEntity { get; set; }
    public string? ToEngineer { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? RelatedInboundNo { get; set; }
    public string? ResponsibleEngineer { get; set; }
    public string? TransferredTo { get; set; } // محول إلى
    public DateTime OutboundDate { get; set; }
    public List<string> AttachmentUrls { get; set; } = new();
    public string? OriginalAttachmentUrl { get; set; } // مرفق الصادر الأصلي
    public string? ReplyAttachmentUrl { get; set; } // مرفق الرد (إن وجد)
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? CreatedByUser { get; set; }
}
