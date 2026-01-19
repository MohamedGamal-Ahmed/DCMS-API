using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Domain.Models;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DCMS.Infrastructure.Services;

public class DashboardDataService
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;

    public DashboardDataService(IDbContextFactory<DCMSDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<DashboardKpis> GetGeneralKpisAsync(string? engineerFullName, int currentUserId, UserRole role)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var today = now.Date;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var sevenDaysLater = now.AddDays(7);

        var kpis = new DashboardKpis();
        
        // OPTIMIZED: Gather all counts in fewer round-trips using Conditional Counting
        var inboundCounts = await context.Inbounds
            .Where(i => i.InboundDate >= startOfYear)
            .GroupBy(i => 1)
            .Select(g => new {
                Today = g.Count(i => i.InboundDate >= today),
                Month = g.Count(i => i.InboundDate >= startOfMonth),
                Ongoing = g.Count(i => i.Status == CorrespondenceStatus.New || i.Status == CorrespondenceStatus.InProgress),
                Closed = g.Count(i => i.Status == CorrespondenceStatus.Completed || i.Status == CorrespondenceStatus.Closed),
                TotalYear = g.Count()
            })
            .FirstOrDefaultAsync();

        if (inboundCounts != null)
        {
            kpis.TotalInboundToday = inboundCounts.Today;
            kpis.TotalInboundMonth = inboundCounts.Month;
            kpis.OngoingTasks = inboundCounts.Ongoing;
            kpis.ClosedTasks = inboundCounts.Closed;
            kpis.ResponseRate = inboundCounts.TotalYear > 0 ? (double)inboundCounts.Closed / inboundCounts.TotalYear * 100 : 0;
        }

        var outboundCounts = await context.Outbounds
            .Where(o => o.OutboundDate >= startOfMonth)
            .GroupBy(o => 1)
            .Select(g => new {
                Today = g.Count(o => o.OutboundDate >= today),
                Month = g.Count()
            })
            .FirstOrDefaultAsync();

        if (outboundCounts != null)
        {
            kpis.TotalOutboundToday = outboundCounts.Today;
            kpis.TotalOutboundMonth = outboundCounts.Month;
        }
        
        kpis.OverdueTasks = await context.Inbounds
            .Where(i => i.InboundDate >= startOfYear &&
                       (i.Status == CorrespondenceStatus.New || i.Status == CorrespondenceStatus.InProgress) &&
                       context.InboundTransfers.Any(t => t.InboundId == i.Id))
            .CountAsync();
        
        kpis.UpcomingMeetings = await context.Meetings.CountAsync(m => 
            m.StartDateTime >= now && m.StartDateTime <= sevenDaysLater);
        
        // AVERAGE RESPONSE TIME: Minimize SELECT for calculation
        var completedItems = await context.Inbounds
            .Where(i => (i.Status == CorrespondenceStatus.Completed || i.Status == CorrespondenceStatus.Closed) && 
                       i.InboundDate >= startOfYear)
            .Select(i => new { i.InboundDate, i.UpdatedAt })
            .Take(50) 
            .ToListAsync();

        if (completedItems.Any())
        {
            var totalDays = completedItems.Sum(i => (i.UpdatedAt - i.InboundDate).TotalDays);
            kpis.AverageResponseTime = totalDays / completedItems.Count;
        }

        return kpis;
    }

    public async Task<SlaSummary> GetSlaSummaryAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var last30Days = now.AddDays(-30);
        
        var recentInbounds = await context.Inbounds
            .Include(i => i.Transfers)
            .Where(i => i.InboundDate >= last30Days)
            .AsNoTracking()
            .ToListAsync();

        var sla = new SlaSummary { TotalInbounds = recentInbounds.Count };
        
        int closedWithinSla = 0;
        double totalLeadTimeDays = 0;
        int leadTimeCount = 0;

        foreach (var i in recentInbounds)
        {
            var firstAction = i.Transfers.OrderBy(t => t.TransferDate).FirstOrDefault()?.TransferDate;
            if (firstAction.HasValue)
            {
                totalLeadTimeDays += (firstAction.Value - i.InboundDate).TotalDays;
                leadTimeCount++;
            }

            bool isCompleted = i.Status == CorrespondenceStatus.Completed || i.Status == CorrespondenceStatus.Closed;
            if (isCompleted)
            {
                var leadTimeHours = firstAction.HasValue ? (firstAction.Value - i.InboundDate).TotalHours : (i.UpdatedAt - i.InboundDate).TotalHours;
                var cycleTimeDays = (i.UpdatedAt - i.InboundDate).TotalDays;

                if (leadTimeHours <= 48 && cycleTimeDays <= 7)
                {
                    closedWithinSla++;
                }
            }
        }

        sla.ClosedWithinSla = closedWithinSla;
        sla.AverageLeadTimeDays = leadTimeCount > 0 ? totalLeadTimeDays / leadTimeCount : 0;
        return sla;
    }

    public async Task<List<UserPerformanceItem>> GetUserPerformanceAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var last30Days = now.AddDays(-30);

        var relevantUsers = await context.Users
            .Where(u => u.Role == UserRole.FollowUpStaff)
            .ToListAsync();
            
        var performanceList = new List<UserPerformanceItem>();

        foreach (var user in relevantUsers)
        {
            var registered = await context.Inbounds.CountAsync(i => i.CreatedByUserId == user.Id && i.CreatedAt >= last30Days);
            var actions = await context.InboundTransfers.CountAsync(t => t.CreatedByUserId == user.Id && t.TransferDate >= last30Days);
            var closures = await context.Inbounds.CountAsync(i => i.UpdatedByUserId == user.Id && 
                                                               i.UpdatedAt >= last30Days && 
                                                               (i.Status == CorrespondenceStatus.Closed || i.Status == CorrespondenceStatus.Completed));

            int activityCount = registered + actions + closures;

            performanceList.Add(new UserPerformanceItem
            {
                UserName = user.FullName ?? user.Username,
                Registrations = registered,
                Actions = actions,
                Closures = closures,
                TotalActivity = activityCount
            });
        }

        return performanceList.OrderByDescending(p => p.TotalActivity).ToList();
    }

    public async Task<DashboardChartData> GetChartDataAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var startOf2026 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var chartData = new DashboardChartData();

        // 1. Inbound vs Outbound (Last 6 Months)
        var last6Months = Enumerable.Range(0, 6).Select(i => now.AddMonths(-i)).OrderBy(d => d).ToList();
        foreach (var date in last6Months)
        {
            var start = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1).AddTicks(-1);
            
            chartData.InboundOutbound.Add(new MonthlyStats
            {
                Month = date.ToString("MMM"),
                InboundCount = await context.Inbounds.CountAsync(i => i.InboundDate >= start && i.InboundDate <= end && i.InboundDate >= startOf2026),
                OutboundCount = await context.Outbounds.CountAsync(o => o.OutboundDate >= start && o.OutboundDate <= end && o.OutboundDate >= startOf2026)
            });
        }

        // 2. Status Distribution
        chartData.StatusDistribution = await context.Inbounds
            .Where(i => i.InboundDate >= startOf2026)
            .GroupBy(i => i.Status)
            .Select(g => new StatusStats { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // 3. Engineer Workload
        var engineers = await context.Engineers
            .Where(e => e.IsResponsibleEngineer)
            .OrderBy(e => e.FullName)
            .ToListAsync();

        foreach (var engineer in engineers)
        {
            chartData.EngineerWorkloads.Add(new EngineerWorkloadStats
            {
                Name = engineer.FullName,
                OpenTasks = await context.InboundResponsibleEngineers.Include(ire => ire.Inbound).CountAsync(ire => ire.EngineerId == engineer.Id && (ire.Inbound.Status == CorrespondenceStatus.New || ire.Inbound.Status == CorrespondenceStatus.InProgress) && ire.Inbound.InboundDate >= startOf2026),
                ClosedTasks = await context.InboundResponsibleEngineers.Include(ire => ire.Inbound).CountAsync(ire => ire.EngineerId == engineer.Id && (ire.Inbound.Status == CorrespondenceStatus.Completed || ire.Inbound.Status == CorrespondenceStatus.Closed) && ire.Inbound.InboundDate >= startOf2026)
            });
        }

        // 4. Task Aging
        var openTasksList = await context.Inbounds
            .Where(i => (i.Status == CorrespondenceStatus.New || i.Status == CorrespondenceStatus.InProgress) && i.InboundDate >= startOf2026)
            .Select(i => i.InboundDate)
            .ToListAsync();

        foreach (var date in openTasksList)
        {
            var age = (now - date).TotalDays;
            if (age <= 3) chartData.Aging.Normal++;
            else if (age <= 7) chartData.Aging.Warning++;
            else chartData.Aging.Critical++;
        }

        // 5. External Distribution
        var externalTransfers = await context.InboundTransfers
            .Include(t => t.Engineer)
            .Where(t => t.Engineer.IsResponsibleEngineer == false && t.TransferDate >= startOf2026)
            .GroupBy(t => t.Engineer.FullName)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToListAsync();

        var top10 = externalTransfers.Take(10).ToList();
        var others = externalTransfers.Skip(10).Sum(e => e.Count);

        foreach (var item in top10)
        {
            chartData.ExternalDistribution.Add(new ExternalDistributionStats { Name = item.Name, Count = item.Count });
        }
        if (others > 0)
        {
            chartData.ExternalDistribution.Add(new ExternalDistributionStats { Name = "أخرى", Count = others });
        }

        // 6. Custom Employee Distribution (All Active Enginners)
        // We'll rename this logic-wise to follow the dynamic requirement
        chartData.CustomEmployeeWorkloads.Clear();
        var activeEngineers = await context.Engineers
            .Where(e => e.IsActive && e.IsResponsibleEngineer)
            .OrderBy(e => e.FullName)
            .ToListAsync();

        foreach (var eng in activeEngineers)
        {
            chartData.CustomEmployeeWorkloads.Add(new EngineerWorkloadStats
            {
                Name = eng.FullName,
                OpenTasks = await context.Inbounds.CountAsync(i => 
                    (i.Status == CorrespondenceStatus.New || i.Status == CorrespondenceStatus.InProgress) &&
                    i.InboundDate >= startOf2026 &&
                    (i.ResponsibleEngineer.Contains(eng.FullName) || 
                     context.InboundResponsibleEngineers.Any(ire => ire.InboundId == i.Id && ire.EngineerId == eng.Id))),
                
                ClosedTasks = await context.Inbounds.CountAsync(i => 
                    (i.Status == CorrespondenceStatus.Completed || i.Status == CorrespondenceStatus.Closed) &&
                    i.InboundDate >= startOf2026 &&
                    (i.ResponsibleEngineer.Contains(eng.FullName) || 
                     context.InboundResponsibleEngineers.Any(ire => ire.InboundId == i.Id && ire.EngineerId == eng.Id)))
            });
        }

        return chartData;
    }

    public async Task<AiAnalyticsMetrics> GetAiAnalyticsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var logs = await context.AiRequestLogs.AsNoTracking().ToListAsync();
        
        var metrics = new AiAnalyticsMetrics();
        if (!logs.Any()) return metrics;

        metrics.HoursSaved = logs.Sum(l => l.SecondsSaved) / 3600.0;
        metrics.TotalTokens = logs.Sum(l => l.PromptTokens + l.CompletionTokens);
        metrics.SuccessRate = (double)logs.Count(l => l.IsSuccess) / logs.Count * 100;

        metrics.ToolBreakdown = logs
            .Where(l => !string.IsNullOrEmpty(l.ActionExecuted) && l.ActionExecuted != "None")
            .SelectMany(l => l.ActionExecuted!.Split(", "))
            .GroupBy(a => a)
            .Select(g => new AiToolUsage { ToolName = g.Key, Count = g.Count() })
            .OrderByDescending(t => t.Count)
            .ToList();

        for (int i = 29; i >= 0; i--)
        {
            var date = now.AddDays(-i).Date;
            var dayLogCount = logs.Count(l => l.CreatedAt.ToUniversalTime().Date == date);
            metrics.DailyUsage.Add(new AiUsageDay { Date = date, Count = dayLogCount });
        }

        return metrics;
    }
    public async Task<List<EngineerWorkloadStats>> GetEngineerWorkloadAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var startOf2026 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        var engineers = await context.Engineers.Where(e => e.IsResponsibleEngineer).ToListAsync();
        var stats = new List<EngineerWorkloadStats>();

        foreach (var eng in engineers)
        {
            var open = await context.InboundResponsibleEngineers.Include(ire => ire.Inbound).CountAsync(ire => ire.EngineerId == eng.Id && (ire.Inbound.Status == CorrespondenceStatus.New || ire.Inbound.Status == CorrespondenceStatus.InProgress) && ire.Inbound.InboundDate >= startOf2026);
            var closed = await context.InboundResponsibleEngineers.Include(ire => ire.Inbound).CountAsync(ire => ire.EngineerId == eng.Id && (ire.Inbound.Status == CorrespondenceStatus.Completed || ire.Inbound.Status == CorrespondenceStatus.Closed) && ire.Inbound.InboundDate >= startOf2026);
            
            stats.Add(new EngineerWorkloadStats 
            { 
                Name = eng.FullName,
                OpenTasks = open,
                ClosedTasks = closed 
            });
        }
        return stats.OrderByDescending(s => s.OpenTasks + s.ClosedTasks).ToList();
    }

    public async Task<List<ExternalDistributionStats>> GetExternalDistributionAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var startOf2026 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var data = await context.InboundTransfers
            .Include(t => t.Engineer)
            .Where(t => t.Engineer.IsResponsibleEngineer == false && t.TransferDate >= startOf2026)
            .GroupBy(t => t.Engineer.FullName)
            .Select(g => new ExternalDistributionStats { Name = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(20)
            .ToListAsync();

        return data;
    }

    public async Task<List<Inbound>> GetLatestTopicsAsync(int count)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Inbounds
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new Inbound { Id = i.Id, Subject = i.Subject, Status = i.Status, SubjectNumber = i.SubjectNumber })
            .Take(Math.Min(count, 5))
            .ToListAsync();
    }

    public async Task<List<Inbound>> SearchTopicsAsync(string query, int count)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Inbounds
            .Where(i => i.Subject.Contains(query) || i.SubjectNumber.Contains(query) || (i.Code != null && i.Code.Contains(query)))
            .OrderByDescending(i => i.InboundDate)
            .Select(i => new Inbound { Id = i.Id, Subject = i.Subject, Status = i.Status, SubjectNumber = i.SubjectNumber })
            .Take(Math.Min(count, 5))
            .ToListAsync();
    }

    public async Task<List<Meeting>> GetMeetingsByDateAsync(DateTime date)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var startOfDay = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        return await context.Meetings
            .Where(m => m.StartDateTime >= startOfDay && m.StartDateTime <= endOfDay)
            .OrderBy(m => m.StartDateTime)
            .ToListAsync();
    }

    public async Task<List<Meeting>> GetUpcomingMeetingsByMonthAsync(int year, int month, int count)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddTicks(-1);
        var now = DateTime.UtcNow;

        return await context.Meetings
            .Where(m => m.StartDateTime >= startDate && m.StartDateTime <= endDate && m.StartDateTime >= now)
            .OrderBy(m => m.StartDateTime)
            .Take(count)
            .ToListAsync();
    }
}
