namespace DCMS.Domain.Entities;

/// <summary>
/// Junction table for many-to-many relationship between Inbound and Engineers (Transfers)
/// </summary>
public class InboundTransfer
{
    public int InboundId { get; set; }
    public Inbound Inbound { get; set; } = null!;
    
    public int EngineerId { get; set; }
    public Engineer Engineer { get; set; } = null!;
    
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    
    public string? Response { get; set; } // رد الشخص المحول إليه
    public DateTime? ResponseDate { get; set; } // تاريخ الرد
    
    public string? TransferAttachmentUrl { get; set; } // مرفق التأشيرة/التحويل
    public string? ResponseAttachmentUrl { get; set; } // مرفق الرد
    
    public int? CreatedByUserId { get; set; } // قام بالتحويل
    public User? CreatedByUser { get; set; }
}
