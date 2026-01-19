using DCMS.Domain.Enums;

namespace DCMS.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? PublicKeyCredential { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Inbound> CreatedInbounds { get; set; } = new List<Inbound>();
    public ICollection<Outbound> CreatedOutbounds { get; set; } = new List<Outbound>();
    public ICollection<Contract> CreatedContracts { get; set; } = new List<Contract>();
    public ICollection<CalendarEvent> CreatedEvents { get; set; } = new List<CalendarEvent>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
