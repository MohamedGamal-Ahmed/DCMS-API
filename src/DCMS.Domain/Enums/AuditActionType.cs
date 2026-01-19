namespace DCMS.Domain.Enums;

/// <summary>
/// Types of actions that can be audited in the system
/// </summary>
public enum AuditActionType
{
    /// <summary>
    /// Entity was created
    /// </summary>
    Create = 1,
    
    /// <summary>
    /// Entity was updated/modified
    /// </summary>
    Update = 2,
    
    /// <summary>
    /// Entity was deleted
    /// </summary>
    Delete = 3,
    
    /// <summary>
    /// Sensitive data was accessed/read (optional, for future use)
    /// </summary>
    Read = 4
}
