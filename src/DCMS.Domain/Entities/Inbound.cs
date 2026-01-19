using DCMS.Domain.Enums;

namespace DCMS.Domain.Entities;

public class Inbound
{
    public int Id { get; set; }
    public string SubjectNumber { get; set; } = string.Empty; // IN-0001 (auto-generated)
    public string? Code { get; set; } // كود
    public InboundCategory Category { get; set; }
    public string? FromEntity { get; set; } // وارد من جهة
    public string? FromEngineer { get; set; } // وارد من مهندس/م
    public string Subject { get; set; } = string.Empty; // الموضوع
    public string? ResponsibleEngineer { get; set; } // المهندس المسئول
    public DateTime InboundDate { get; set; } // وارد بتاريخ
    public string? TransferredTo { get; set; } // محول إلى
    public DateTime? TransferDate { get; set; } // تاريخ التحويل
    public string? Reply { get; set; } // الرد
    public CorrespondenceStatus Status { get; set; } = CorrespondenceStatus.New;
    public ContractType? ContractType { get; set; } // نوع العقد (للعقود فقط)
    public string? AttachmentUrl { get; set; } // رابط المرفقات (URL) - قديم للتوافق
    public string? OriginalAttachmentUrl { get; set; } // مرفق الوارد الأصلي (نظام جديد)
    public string? ReplyAttachmentUrl { get; set; } // مرفق الرد النهائي/المباشر
    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; } // قام بالتعديل/الإغلاق
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public ICollection<InboundResponsibleEngineer> ResponsibleEngineers { get; set; } = new List<InboundResponsibleEngineer>();
    public ICollection<InboundTransfer> Transfers { get; set; } = new List<InboundTransfer>();
}
