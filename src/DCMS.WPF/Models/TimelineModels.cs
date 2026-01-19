using System;

namespace DCMS.WPF.Models;

public class TimelineItem
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TimelineItemType Type { get; set; }
    public string? AttachmentUrl { get; set; }
    public int? RelatedEngineerId { get; set; } // For Transfers/Responses
    public bool HasAttachment => !string.IsNullOrEmpty(AttachmentUrl);
}

public enum TimelineItemType
{
    Creation,
    Transfer,
    Response,
    StatusChange
}
