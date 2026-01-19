using DCMS.Domain.Enums;

namespace DCMS.Domain.Entities;

public class Meeting
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? Location { get; set; }
    public string? Attendees { get; set; } // Comma-separated names
    public MeetingType MeetingType { get; set; } = MeetingType.Meeting;

    // New Analysis Fields
    public bool IsOnline { get; set; }
    public string? Country { get; set; }        // e.g. Jordan, Ghana
    public string? RelatedProject { get; set; } // e.g. Dabaa
    public string? RelatedPartner { get; set; } // e.g. Samsung
    public string? OnlineMeetingLink { get; set; }

    // Recurrence
    public bool IsRecurring { get; set; }
    public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
    public int? RecurrenceCount { get; set; } // Number of times to repeat (null = infinite)
    public DateTime? RecurrenceEndDate { get; set; }

    // Reminders
    public int? ReminderMinutesBefore { get; set; } // e.g., 15 minutes
    public bool IsNotificationSent { get; set; }

    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? CreatedByUser { get; set; }
}
