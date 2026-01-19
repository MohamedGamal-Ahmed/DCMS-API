using System;

namespace DCMS.Application.Models;

public class InventoryItem
{
    public string SubjectNumber { get; set; } = "";
    public string Subject { get; set; } = "";
    public DateTime Date { get; set; }
    public string FromEntity { get; set; } = "";
    public string ResponsibleEngineer { get; set; } = "";
    public string Status { get; set; } = "";
}

public class TransmittalItem
{
    public string Subject { get; set; } = "";
    public DateTime Date { get; set; }
    public string Sender { get; set; } = "";
}

public class EngineerPerformanceItem
{
    public string EngineerName { get; set; } = "";
    public int OpenCount { get; set; }
    public int DelayedCount { get; set; }
    public double CompletionRate { get; set; }
}

public class SearchItem
{
    public string SubjectNumber { get; set; } = "";
    public string Code { get; set; } = "";
    public string Subject { get; set; } = "";
    public DateTime Date { get; set; }
    public string FromEntity { get; set; } = "";
    public string ResponsibleEngineer { get; set; } = "";
    public string TransferredTo { get; set; } = "";
    public string Reply { get; set; } = "";
    public string Status { get; set; } = "";
}

public class SlaItem
{
    public string SubjectNumber { get; set; } = "";
    public string Subject { get; set; } = "";
    public DateTime RegistrationDate { get; set; }
    public DateTime? FirstActionDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    
    public TimeSpan? ResponseTime => FirstActionDate - RegistrationDate;
    public TimeSpan? CycleTime => CompletionDate - RegistrationDate;

    public string ResponseTimeDisplay => ResponseTime.HasValue ? FormatTimeSpan(ResponseTime.Value) : "-";
    public string CycleTimeDisplay => CycleTime.HasValue ? FormatTimeSpan(CycleTime.Value) : "-";

    public string SlaStatus => IsDelayed ? "متأخر" : "منضبط";
    public bool IsDelayed => (ResponseTime.HasValue && ResponseTime.Value.TotalHours > 48) || 
                             (CycleTime.HasValue && CycleTime.Value.TotalDays > 7);

    private string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays} يوم و {ts.Hours} ساعة";
        return $"{ts.Hours} ساعة و {ts.Minutes} دقيقة";
    }
}
