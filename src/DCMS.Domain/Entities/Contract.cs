using DCMS.Domain.Enums;

namespace DCMS.Domain.Entities;

public class Contract
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime SigningDate { get; set; }
    public string? ResponsibleEngineer { get; set; }
    public string? TransferredTo { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.New;
    public string? Notes { get; set; }
    public List<string> AttachmentUrls { get; set; } = new();
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? CreatedByUser { get; set; }
    public ICollection<ContractParty> Parties { get; set; } = new List<ContractParty>();
}
