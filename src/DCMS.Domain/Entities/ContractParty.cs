namespace DCMS.Domain.Entities;

public class ContractParty
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public string PartyType { get; set; } = string.Empty; // طرف أول, طرف ثاني, طرف إضافي
    public string PartyName { get; set; } = string.Empty;
    public string? PartyRole { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Contract Contract { get; set; } = null!;
}
