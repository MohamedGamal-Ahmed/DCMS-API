using System;

namespace DCMS.Application.DTOs;

public class AiSearchResultDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Inbound or Outbound
    public string SubjectNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string? FromOrTo { get; set; }
    public string? Status { get; set; }
    public string? Summary { get; set; }
    public string? ResponsibleEngineer { get; set; }
    public string? AttachmentUrl { get; set; }
}
