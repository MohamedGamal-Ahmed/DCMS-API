namespace DCMS.Domain.Entities;

public class Engineer
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; } // م/, د/, ا/
    public bool IsActive { get; set; } = true;
    public bool IsResponsibleEngineer { get; set; } = false; // True for the 5 main engineers
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<InboundResponsibleEngineer> InboundResponsibleEngineers { get; set; } = new List<InboundResponsibleEngineer>();
    public ICollection<InboundTransfer> InboundTransfers { get; set; } = new List<InboundTransfer>();
}
