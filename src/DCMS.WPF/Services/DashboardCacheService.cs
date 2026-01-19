using DCMS.Domain.Models;
using DCMS.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace DCMS.WPF.Services;

/// <summary>
/// EMERGENCY CACHE SERVICE: Created to stop database overload at 95% bandwidth.
/// Uses in-memory cache with VERY long TTL (until end of month or manual refresh).
/// </summary>
public class DashboardCacheService
{
    private readonly DashboardDataService _dashboardDataService;
    private readonly IMemoryCache _cache;
    
    // Cache keys
    private const string KPI_CACHE_KEY = "dashboard_kpis";
    private const string CHART_CACHE_KEY = "dashboard_charts";
    private const string SLA_CACHE_KEY = "dashboard_sla";
    private const string AI_CACHE_KEY = "dashboard_ai";
    private const string PERFORMANCE_CACHE_KEY = "dashboard_performance";
    
    // EMERGENCY: Cache for 5 minutes instead of 30 days to ensure data visibility
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    
    public DateTime? LastRefreshed { get; private set; }
    public bool IsCacheValid => LastRefreshed.HasValue && (DateTime.UtcNow - LastRefreshed.Value) < CacheDuration;

    public DashboardCacheService(DashboardDataService dashboardDataService, IMemoryCache cache)
    {
        _dashboardDataService = dashboardDataService;
        _cache = cache;
    }

    public async Task<DashboardKpis> GetKpisAsync(string? engineerFullName, int currentUserId, DCMS.Domain.Enums.UserRole role, bool forceRefresh = false)
    {
        if (!forceRefresh && _cache.TryGetValue(KPI_CACHE_KEY, out DashboardKpis? cachedKpis) && cachedKpis != null)
        {
            return cachedKpis;
        }

        var kpis = await _dashboardDataService.GetGeneralKpisAsync(engineerFullName, currentUserId, role);
        _cache.Set(KPI_CACHE_KEY, kpis, CacheDuration);
        LastRefreshed = DateTime.UtcNow;
        return kpis;
    }

    public async Task<DashboardChartData> GetChartDataAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cache.TryGetValue(CHART_CACHE_KEY, out DashboardChartData? cachedData) && cachedData != null)
        {
            return cachedData;
        }

        var data = await _dashboardDataService.GetChartDataAsync();
        _cache.Set(CHART_CACHE_KEY, data, CacheDuration);
        LastRefreshed = DateTime.UtcNow;
        return data;
    }

    public async Task<SlaSummary> GetSlaSummaryAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cache.TryGetValue(SLA_CACHE_KEY, out SlaSummary? cachedSla) && cachedSla != null)
        {
            return cachedSla;
        }

        var sla = await _dashboardDataService.GetSlaSummaryAsync();
        _cache.Set(SLA_CACHE_KEY, sla, CacheDuration);
        return sla;
    }

    public async Task<AiAnalyticsMetrics> GetAiAnalyticsAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cache.TryGetValue(AI_CACHE_KEY, out AiAnalyticsMetrics? cachedAi) && cachedAi != null)
        {
            return cachedAi;
        }

        var ai = await _dashboardDataService.GetAiAnalyticsAsync();
        _cache.Set(AI_CACHE_KEY, ai, CacheDuration);
        return ai;
    }

    public async Task<List<UserPerformanceItem>> GetUserPerformanceAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cache.TryGetValue(PERFORMANCE_CACHE_KEY, out List<UserPerformanceItem>? cachedPerformance))
        {
            return cachedPerformance!;
        }

        var performance = await _dashboardDataService.GetUserPerformanceAsync();
        _cache.Set(PERFORMANCE_CACHE_KEY, performance, CacheDuration);
        return performance;
    }

    /// <summary>
    /// Manual refresh - only use when user explicitly clicks refresh button
    /// </summary>
    public void InvalidateCache()
    {
        _cache.Remove(KPI_CACHE_KEY);
        _cache.Remove(CHART_CACHE_KEY);
        _cache.Remove(SLA_CACHE_KEY);
        _cache.Remove(AI_CACHE_KEY);
        _cache.Remove(PERFORMANCE_CACHE_KEY);
        LastRefreshed = null;
    }
}
