using DCMS.Domain.Enums;

namespace DCMS.Domain.Entities;

/// <summary>
/// Represents an audit log entry that tracks system operations
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Name of the user who performed the action
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of action performed (Create, Update, Delete, Read)
    /// </summary>
    public AuditActionType Action { get; set; }
    
    /// <summary>
    /// Type of entity affected (e.g. "Inbound", "Outbound", "Meeting")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the affected entity (stored as string for flexibility)
    /// </summary>
    public string EntityId { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when the action occurred (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// JSON representation of the entity's values before the change (for Update/Delete)
    /// </summary>
    public string? OldValues { get; set; }
    
    /// <summary>
    /// JSON representation of the entity's values after the change (for Create/Update)
    /// </summary>
    public string? NewValues { get; set; }
    
    /// <summary>
    /// IP address of the client that performed the action (optional)
    /// </summary>
    public string? IPAddress { get; set; }
    
    /// <summary>
    /// Human-readable description of the action
    /// </summary>
    public string? Description { get; set; }
}
