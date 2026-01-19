using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DCMS.Domain.Enums;
using DCMS.Domain.Models;
using DCMS.Infrastructure.Data;
using DCMS.WPF.Services;
using DCMS.WPF.Messages;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DCMS.Application.Interfaces;
using DCMS.Infrastructure.Services;

namespace DCMS.WPF.ViewModels;

public partial class DashboardViewModel : ViewModelBase, IRecipient<DashboardRefreshMessage>
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly ReportingService _reportingService;
    private readonly AiRoiReportService _aiRoiReportService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DashboardDataService _dashboardDataService;
    private readonly DashboardCacheService _dashboardCacheService; // EMERGENCY: Cache layer
    private readonly DashboardAiService _dashboardAiService;
    private readonly IAiDashboardService _aiDashboardService;
    private readonly CurrentUserService _currentUserService;
    private readonly NotificationService _notificationService;

    [ObservableProperty] private int _totalInboundToday;
    [ObservableProperty] private int _totalInboundMonth;
    [ObservableProperty] private int _totalOutboundToday;
    [ObservableProperty] private int _totalOutboundMonth;
    [ObservableProperty] private int _ongoingTasks;
    [ObservableProperty] private int _closedTasks;
    [ObservableProperty] private int _overdueTasks;
    [ObservableProperty] private int _upcomingMeetings;
    [ObservableProperty] private double _responseRate;
    [ObservableProperty] private double _averageResponseTime;
    [ObservableProperty] private ObservableCollection<UserPerformanceItem> _userPerformance = new();
    [ObservableProperty] private SlaSummary _slaSummary = new();
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(FteImpact))] private double _aiHoursSaved;
    [ObservableProperty] private int _aiTotalTokens;
    [ObservableProperty] private double _aiSuccessRate;
    [ObservableProperty] private string _aiInsights = string.Empty;
    [ObservableProperty] private string _managementInsights = "✨ اضغط 'تحليل' للحصول على توصيات إدارية من الذكاء الاصطناعي";
    [ObservableProperty] private SeriesCollection _externalDistributionSeries = new();
    [ObservableProperty] private string[] _externalDistributionLabels = Array.Empty<string>();
    [ObservableProperty] private SeriesCollection _inboundOutboundSeries = new();
    [ObservableProperty] private string[] _inboundOutboundLabels = Array.Empty<string>();
    [ObservableProperty] private SeriesCollection _statusSeries = new();
    [ObservableProperty] private SeriesCollection _taskAgingSeries = new();
    [ObservableProperty] private string[] _agingLabels = Array.Empty<string>();
    [ObservableProperty] private SeriesCollection _aiTaskBreakdownSeries = new();
    [ObservableProperty] private SeriesCollection _aiUsageTrendSeries = new();
    [ObservableProperty] private string[] _aiUsageTrendLabels = Array.Empty<string>();
    [ObservableProperty] private SeriesCollection _aiPerformanceComparisonSeries = new();
    [ObservableProperty] private string[] _aiPerformanceLabels = Array.Empty<string>();
    [ObservableProperty] private SeriesCollection _customEngineerSeries = new();
    [ObservableProperty] private string[] _customEngineerLabels = Array.Empty<string>();
    [ObservableProperty] private string _lastRefreshedText = "غير محدث"; // EMERGENCY: Display cache time

    public double FteImpact => AiHoursSaved / 176.0;
    public Func<double, string> YFormatter { get; } = value => value.ToString("N0");
    public ExecutiveAnalysisViewModel ExecutiveAnalysisViewModel { get; }
    public event EventHandler<SearchNavigationArgs>? RequestNavigation;

    public DashboardViewModel(
        IDbContextFactory<DCMSDbContext> contextFactory,
        ReportingService reportingService,
        ExecutiveAnalysisViewModel executiveAnalysisViewModel,
        AiRoiReportService aiRoiReportService,
        DashboardDataService dashboardDataService,
        DashboardCacheService dashboardCacheService, // EMERGENCY: Injected cache
        DashboardAiService dashboardAiService,
        IAiDashboardService aiDashboardService,
        CurrentUserService currentUserService,
        IServiceProvider serviceProvider,
        NotificationService notificationService)
    {
        _contextFactory = contextFactory;
        _reportingService = reportingService;
        _aiRoiReportService = aiRoiReportService;
        _dashboardDataService = dashboardDataService;
        _dashboardCacheService = dashboardCacheService; // EMERGENCY: Cache
        _dashboardAiService = dashboardAiService;
        _aiDashboardService = aiDashboardService;
        _currentUserService = currentUserService;
        _serviceProvider = serviceProvider;
        _notificationService = notificationService;
        ExecutiveAnalysisViewModel = executiveAnalysisViewModel;

        _ = Refresh();
        WeakReferenceMessenger.Default.Register<DashboardRefreshMessage>(this);
    }

    public void Receive(DashboardRefreshMessage message) => _ = Refresh();

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadDashboardData(forceRefresh: false);
    }

    // EMERGENCY: Manual force refresh command for UI button
    [RelayCommand]
    private async Task ForceRefresh()
    {
        _dashboardCacheService.InvalidateCache();
        await LoadDashboardData(forceRefresh: true);
    }

    private async Task LoadDashboardData(bool forceRefresh)
    {
        try {
            IsBusy = true;
            await Task.Delay(300); // UI Polish

            var currentUser = _currentUserService.CurrentUser;
            
            // EMERGENCY: Use CACHED data - no DB hit unless forceRefresh
            var kpis = await _dashboardCacheService.GetKpisAsync(
                currentUser?.FullName, currentUser?.Id ?? 0, 
                currentUser?.Role ?? UserRole.FollowUpStaff, forceRefresh);
            
            TotalInboundToday = kpis.TotalInboundToday; TotalInboundMonth = kpis.TotalInboundMonth;
            TotalOutboundToday = kpis.TotalOutboundToday; TotalOutboundMonth = kpis.TotalOutboundMonth;
            OngoingTasks = kpis.OngoingTasks; ClosedTasks = kpis.ClosedTasks; OverdueTasks = kpis.OverdueTasks;
            UpcomingMeetings = kpis.UpcomingMeetings; ResponseRate = kpis.ResponseRate; AverageResponseTime = kpis.AverageResponseTime;

            SlaSummary = await _dashboardCacheService.GetSlaSummaryAsync(forceRefresh);
            
            // EMERGENCY: Cache user performance
            var performance = await _dashboardCacheService.GetUserPerformanceAsync(forceRefresh);
            UserPerformance = new ObservableCollection<UserPerformanceItem>(performance);

            var chartData = await _dashboardCacheService.GetChartDataAsync(forceRefresh);
            PopulateCharts(chartData);

            var aiMetrics = await _dashboardCacheService.GetAiAnalyticsAsync(forceRefresh);
            PopulateAiCharts(aiMetrics);

            // Update last refreshed display
            if (_dashboardCacheService.LastRefreshed.HasValue)
            {
                LastRefreshedText = $"آخر تحديث: {_dashboardCacheService.LastRefreshed.Value.ToLocalTime():HH:mm:ss}";
            }
        } 
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Dashboard Refresh Error: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    private void PopulateCharts(DashboardChartData data)
    {
        InboundOutboundSeries = new SeriesCollection {
            new ColumnSeries { Title = "وارد", Values = new ChartValues<int>(data.InboundOutbound.Select(s => s.InboundCount)) },
            new ColumnSeries { Title = "صادر", Values = new ChartValues<int>(data.InboundOutbound.Select(s => s.OutboundCount)) }
        };
        InboundOutboundLabels = data.InboundOutbound.Select(s => s.Month).ToArray();

        StatusSeries = new SeriesCollection();
        foreach (var s in data.StatusDistribution) StatusSeries.Add(new PieSeries { Title = GetStatusArabicName(s.Status), Values = new ChartValues<int> { s.Count }, DataLabels = true });

        TaskAgingSeries = new SeriesCollection {
            new ColumnSeries { Title = "طبيعي (0-3 أيام)", Values = new ChartValues<int> { data.Aging.Normal }, Fill = System.Windows.Media.Brushes.Green },
            new ColumnSeries { Title = "متوسط (4-7 أيام)", Values = new ChartValues<int> { data.Aging.Warning }, Fill = System.Windows.Media.Brushes.Orange },
            new ColumnSeries { Title = "متأخر (> 7 أيام)", Values = new ChartValues<int> { data.Aging.Critical }, Fill = System.Windows.Media.Brushes.Red }
        };
        AgingLabels = new[] { "توزيع الأعمار" };

        ExternalDistributionSeries = new SeriesCollection();
        foreach (var e in data.ExternalDistribution) ExternalDistributionSeries.Add(new PieSeries { Title = e.Name, Values = new ChartValues<int> { e.Count }, DataLabels = true });
        ExternalDistributionLabels = data.ExternalDistribution.Select(e => e.Name).ToArray();

        CustomEngineerSeries = new SeriesCollection {
            new ColumnSeries { Title = "مفتوحة", Values = new ChartValues<int>(data.CustomEmployeeWorkloads.Select(w => w.OpenTasks)), Fill = System.Windows.Media.Brushes.DeepSkyBlue },
            new ColumnSeries { Title = "مغلقة", Values = new ChartValues<int>(data.CustomEmployeeWorkloads.Select(w => w.ClosedTasks)), Fill = System.Windows.Media.Brushes.MediumSeaGreen }
        };
        CustomEngineerLabels = data.CustomEmployeeWorkloads.Select(w => w.Name).ToArray();
    }

    private void PopulateAiCharts(AiAnalyticsMetrics metrics)
    {
        AiHoursSaved = metrics.HoursSaved; AiTotalTokens = metrics.TotalTokens; AiSuccessRate = metrics.SuccessRate;
        AiTaskBreakdownSeries = new SeriesCollection();
        foreach (var t in metrics.ToolBreakdown) AiTaskBreakdownSeries.Add(new PieSeries { Title = GetToolArabicName(t.ToolName), Values = new ChartValues<int> { t.Count }, DataLabels = true });

        AiUsageTrendSeries = new SeriesCollection { new LineSeries { Title = "عدد التفاعلات", Values = new ChartValues<int>(metrics.DailyUsage.Select(d => d.Count)) } };
        AiUsageTrendLabels = metrics.DailyUsage.Select(d => d.Date.ToString("MM/dd")).ToArray();

        AiPerformanceComparisonSeries = new SeriesCollection {
            new RowSeries { Title = "البحث اليدوي (تقديري)", Values = new ChartValues<double> { 300 }, Fill = System.Windows.Media.Brushes.Gray },
            new RowSeries { Title = "البحث بالذكاء الاصطناعي", Values = new ChartValues<double> { 5 }, Fill = System.Windows.Media.Brushes.Teal }
        };
        AiPerformanceLabels = new[] { "متوسط وقت الوصول للمعلومة (ثواني)" };
        
        var totalAiRequests = metrics.DailyUsage.Sum(d => d.Count);
        var savingImpact = metrics.HoursSaved >= 1 ? $"{metrics.HoursSaved:F1} ساعة" : $"{metrics.HoursSaved * 60:F0} دقيقة";
        
        AiInsights = $"🚀 لقد وفر النظام ذكاءً اصطناعياً قام بتوفير {savingImpact} عمل منذ التفعيل. " +
                     $"محرك البحث الذكي عالج {totalAiRequests} طلب بدقة بلغت {AiSuccessRate:F0}% " +
                     $"مما أدى لتقليل زمن الوصول للمعلومات بنسبة تقريبية 98% مقارنة بالبحث التقليدي.";
    }

    [RelayCommand]
    private void NavigateToSearch(object? parameter)
    {
        if (parameter is not string key) return;
        var args = new SearchNavigationArgs();
        var today = DateTime.UtcNow.Date;
        var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        switch (key) {
            case "InboundToday": args.FromDate = today; break;
            case "InboundMonth": args.FromDate = startOfMonth; break;
            case "Ongoing": args.Status = CorrespondenceStatus.InProgress; break;
            case "Closed": args.Status = CorrespondenceStatus.Closed; break;
            case "Overdue": args.OnlyOverdue = true; break;
            case "OutboundToday": args.OnlyOutbound = true; args.FromDate = today; break;
            case "UpcomingMeetings": args.FromDate = today; break;
        }
        RequestNavigation?.Invoke(this, args);
    }

    [RelayCommand]
    private void DataClick(object? p)
    {
        if (p is not ChartPoint point) return;
        if (point.SeriesView is PieSeries pieSeries) {
            var status = GetStatusFromArabicName(pieSeries.Title);
            RequestNavigation?.Invoke(this, status.HasValue ? new SearchNavigationArgs { Status = status } : new SearchNavigationArgs { Entity = pieSeries.Title });
        } else if (point.SeriesView is ColumnSeries colSeries && CustomEngineerSeries.Contains(colSeries)) {
            int idx = (int)point.X;
            if (idx >= 0 && idx < CustomEngineerLabels.Length) RequestNavigation?.Invoke(this, new SearchNavigationArgs { Engineer = CustomEngineerLabels[idx] });
        }
    }

    [RelayCommand]
    private async Task GenerateExecutiveSummary()
    {
        if (IsBusy) return; IsBusy = true; AiInsights = "⏳ جاري تجميع البيانات وتحليلها...";
        try { AiInsights = await _dashboardAiService.GenerateExecutiveSummaryAsync(); }
        catch (Exception ex) { AiInsights = $"❌ فشل توليد الملخص: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ExportRoiReport()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "PDF Files (*.pdf)|*.pdf", FileName = $"AI_ROI_{DateTime.Now:yyyyMMdd}.pdf" };
        if (dialog.ShowDialog() != true) return;
        try {
            var metrics = await _dashboardDataService.GetAiAnalyticsAsync();
            var aiData = await _aiDashboardService.GetAiDashboardDataAsync(_currentUserService.CurrentUser?.Id ?? 0, _currentUserService.CurrentUser?.Role.ToString(), _currentUserService.CurrentUser?.FullName, _currentUserService.CurrentUser?.Username);
            
            var reportData = new RoiReportData {
                TotalHoursSaved = metrics.HoursSaved, 
                FteImpact = metrics.HoursSaved / 176.0, 
                SuccessRate = metrics.SuccessRate, 
                TotalReceived = aiData.TotalReceived,
                TotalPresented = aiData.TotalPresented,
                TotalPending = aiData.TotalPending,
                TotalTransferred = aiData.TotalTransferred,
                Narrative = AiInsights,
                ToolUsage = metrics.ToolBreakdown.Select(t => new ToolUsageStats { ToolNameAR = GetToolArabicName(t.ToolName), Count = t.Count, HoursSaved = (t.Count * 300) / 3600.0 }).ToList()
            };
            _aiRoiReportService.GenerateReport(dialog.FileName, reportData);
            _notificationService.Success("تم تصدير تقرير تحليل ROI بنجاح");
            
            try 
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            } catch { /* Ignore open error */ }

        } catch (Exception ex) { _notificationService.Error($"فشل التصدير: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task ExportComprehensiveReport()
    {
        // Role Handling: Only visible/effective for Admin and OfficeManager
        var currentUserRole = _currentUserService.CurrentUser?.Role;
        if (currentUserRole != UserRole.Admin && currentUserRole != UserRole.OfficeManager)
        {
            _notificationService.Warning("يرجي الرجوع الي الادمن للحصول علي نسخة من التقرير");
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "PDF Files (*.pdf)|*.pdf", FileName = $"Executive_Report_{DateTime.Now:yyyyMMdd}.pdf" };
        if (dialog.ShowDialog() != true) return;
        IsBusy = true;
        try {
            if (string.IsNullOrWhiteSpace(AiInsights) || AiInsights.Contains("⏳")) await GenerateExecutiveSummary();
            var metrics = await _dashboardDataService.GetAiAnalyticsAsync();
            var aiData = await _aiDashboardService.GetAiDashboardDataAsync(_currentUserService.CurrentUser?.Id ?? 0, _currentUserService.CurrentUser?.Role.ToString(), _currentUserService.CurrentUser?.FullName, _currentUserService.CurrentUser?.Username);
            
            // Get workload data for comprehensive report
            var workloadData = await _dashboardDataService.GetEngineerWorkloadAsync();
            var externalData = await _dashboardDataService.GetExternalDistributionAsync();
            var staffPerformance = await _dashboardDataService.GetUserPerformanceAsync();
            
            _aiRoiReportService.GenerateReport(dialog.FileName, new RoiReportData { 
                TotalHoursSaved = metrics.HoursSaved, 
                FteImpact = metrics.HoursSaved / 176.0, 
                SuccessRate = metrics.SuccessRate, 
                TotalReceived = aiData.TotalReceived,
                TotalPresented = aiData.TotalPresented,
                TotalPending = aiData.TotalPending,
                TotalTransferred = aiData.TotalTransferred,
                Narrative = AiInsights,
                ManagementInsights = ManagementInsights,
                EngineerWorkload = workloadData,
                StaffPerformance = staffPerformance,
                ExternalDistribution = externalData
            });
            _notificationService.Success("تم تصدير التقرير الشامل بنجاح!");
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true }); } catch { }
        } catch (Exception ex) { _notificationService.Error(ex.Message); }
        finally { IsBusy = false; }
    }

    private string GetToolArabicName(string t) => t switch { "SearchCorrespondences" => "بحث مراسلات", "SearchMeetings" => "بحث اجتماعات", "GetCorrespondenceDetails" => "تلخيص مراسلة", "GetMeetingDetails" => "تلخيص اجتماع", "CategorizeCorrespondence" => "تصنيف آلي", _ => t };
    private string GetStatusArabicName(CorrespondenceStatus s) => s switch { CorrespondenceStatus.New => "جديد", CorrespondenceStatus.InProgress => "جاري", CorrespondenceStatus.Completed => "مكتمل", CorrespondenceStatus.Closed => "مغلق", _ => s.ToString() };
    private CorrespondenceStatus? GetStatusFromArabicName(string a) => a switch { "جديد" => CorrespondenceStatus.New, "جاري" => CorrespondenceStatus.InProgress, "مكتمل" => CorrespondenceStatus.Completed, "مغلق" => CorrespondenceStatus.Closed, _ => null };

    /// <summary>
    /// On-demand AI workload analysis - only runs when user clicks 'Analyze' button
    /// to save API tokens and database compute resources.
    /// </summary>
    [RelayCommand]
    private async Task AnalyzeWorkload()
    {
        if (IsBusy) return;
        IsBusy = true;
        ManagementInsights = "✨ جاري تحليل توزيع العمل واستخراج التوصيات...";
        try
        {
            ManagementInsights = await _dashboardAiService.GenerateManagementInsightsAsync();
            _notificationService.Success("تم تحليل البيانات بنجاح!");
        }
        catch (Exception ex)
        {
            ManagementInsights = $"❌ فشل التحليل: {ex.Message}";
            _notificationService.Error("فشل تحليل البيانات");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
