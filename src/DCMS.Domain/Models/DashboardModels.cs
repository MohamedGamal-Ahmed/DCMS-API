using DCMS.Domain.Enums;

namespace DCMS.Domain.Models;

public class UserPerformanceItem
{
    public string UserName { get; set; } = string.Empty;
    public int Registrations { get; set; }
    public int Actions { get; set; }
    public int Closures { get; set; }
    public int TotalActivity { get; set; }
}

public class SlaSummary
{
    public int TotalInbounds { get; set; }
    public int ClosedWithinSla { get; set; }
    public double SlaComplianceRate => TotalInbounds == 0 ? 1 : (double)ClosedWithinSla / TotalInbounds;
    public double AverageLeadTimeDays { get; set; }
}

public class DashboardKpis
{
    public int TotalInboundToday { get; set; }
    public int TotalInboundMonth { get; set; }
    public int TotalOutboundToday { get; set; }
    public int TotalOutboundMonth { get; set; }
    public int OngoingTasks { get; set; }
    public int ClosedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int UpcomingMeetings { get; set; }
    public double ResponseRate { get; set; }
    public double AverageResponseTime { get; set; }
}

public class MonthlyStats
{
    public string Month { get; set; } = string.Empty;
    public int InboundCount { get; set; }
    public int OutboundCount { get; set; }
}

public class StatusStats
{
    public CorrespondenceStatus Status { get; set; }
    public int Count { get; set; }
}

public class EngineerWorkloadStats
{
    public string Name { get; set; } = string.Empty;
    public int OpenTasks { get; set; }
    public int ClosedTasks { get; set; }
}

public class AgingStats
{
    public int Normal { get; set; }
    public int Warning { get; set; }
    public int Critical { get; set; }
}

public class ExternalDistributionStats
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardChartData
{
    public List<MonthlyStats> InboundOutbound { get; set; } = new();
    public List<StatusStats> StatusDistribution { get; set; } = new();
    public List<EngineerWorkloadStats> EngineerWorkloads { get; set; } = new();
    public AgingStats Aging { get; set; } = new();
    public List<ExternalDistributionStats> ExternalDistribution { get; set; } = new();
    public List<EngineerWorkloadStats> CustomEmployeeWorkloads { get; set; } = new();
}

public class AiAnalyticsMetrics
{
    public double HoursSaved { get; set; }
    public int TotalTokens { get; set; }
    public double SuccessRate { get; set; }
    public List<AiToolUsage> ToolBreakdown { get; set; } = new();
    public List<AiUsageDay> DailyUsage { get; set; } = new();
}

public class AiToolUsage
{
    public string ToolName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AiUsageDay
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class SearchNavigationArgs : EventArgs
{
    public CorrespondenceStatus? Status { get; set; }
    public string? Engineer { get; set; }
    public bool OnlyOverdue { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Entity { get; set; }
    public bool OnlyOutbound { get; set; }
}
