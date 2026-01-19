namespace DCMS.Domain.Entities;

/// <summary>
/// Junction table for many-to-many relationship between Inbound and Engineers (Responsible)
/// </summary>
public class InboundResponsibleEngineer
{
    public int InboundId { get; set; }
    public Inbound Inbound { get; set; } = null!;
    
    public int EngineerId { get; set; }
    public Engineer Engineer { get; set; } = null!;
}
