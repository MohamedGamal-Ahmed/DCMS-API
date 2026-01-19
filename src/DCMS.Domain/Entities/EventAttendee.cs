namespace DCMS.Domain.Entities;

public class EventAttendee
{
    public int EventId { get; set; }
    public int UserId { get; set; }

    // Navigation properties
    public CalendarEvent Event { get; set; } = null!;
    public User User { get; set; } = null!;
}
