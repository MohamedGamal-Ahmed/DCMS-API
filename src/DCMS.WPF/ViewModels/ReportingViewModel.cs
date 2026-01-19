using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using DCMS.Infrastructure.Data;
using DCMS.WPF.Services;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DCMS.Application.Models;

namespace DCMS.WPF.ViewModels;

public partial class ReportingViewModel : ViewModelBase
{
    private readonly ReportingService _reportingService;
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;

    [ObservableProperty]
    private DateTime _fromDate = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime _toDate = DateTime.Today;

    [ObservableProperty]
    private string _recipientName = "";

    [ObservableProperty]
    private TransmittalItem? _selectedItem;

    public ObservableCollection<string> ReportTypes { get; } = new()
    {
        "تقرير تسليم مراسلات (Transmittal)",
        "تقرير الموقف التنفيذي للمهندسين",
        "تقرير حصر شامل"
    };

    [ObservableProperty]
    private string _selectedReportType;

    public ReportingViewModel(ReportingService reportingService, IDbContextFactory<DCMSDbContext> contextFactory)
    {
        _reportingService = reportingService;
        _contextFactory = contextFactory;
        SelectedReportType = ReportTypes.First();
    }

    [RelayCommand]
    private async Task GenerateReport()
    {
        try
        {
            if (SelectedReportType == "تقرير تسليم مراسلات (Transmittal)")
            {
                if (string.IsNullOrWhiteSpace(RecipientName))
                {
                    MessageBox.Show("يرجى إدخال اسم المستلم", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await PreviewReport();
                if (HasData) ExportReport();
            }
            else if (SelectedReportType == "تقرير حصر شامل")
            {
                await PreviewReport();
                if (HasData) ExportReport();
            }
            else if (SelectedReportType == "تقرير الموقف التنفيذي للمهندسين")
            {
                await PreviewReport();
                if (HasData) ExportReport();
            }
            else
            {
                MessageBox.Show("عذراً، هذا التقرير قيد التطوير حالياً", "قريباً", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"حدث خطأ أثناء إنشاء التقرير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task GenerateEngineerPerformance()
    {
        // This method is now superseded by PreviewReport and ExportReport
        // Its logic has been moved to FetchEngineerPerformanceData and ExportReport
    }

    private async Task GenerateTransmittal()
    {
        // This method is now superseded by PreviewReport and ExportReport
        // Its logic has been moved to FetchTransmittalData and ExportReport
    }

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private DCMS.Domain.Enums.CorrespondenceStatus? _selectedStatus;

    public List<DCMS.Domain.Enums.CorrespondenceStatus> Statuses { get; } = Enum.GetValues(typeof(DCMS.Domain.Enums.CorrespondenceStatus)).Cast<DCMS.Domain.Enums.CorrespondenceStatus>().ToList();

    public bool IsInventoryReport => SelectedReportType == "تقرير حصر شامل";
    public bool IsTransmittalReport => SelectedReportType == "تقرير تسليم مراسلات (Transmittal)";

    partial void OnSelectedReportTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsInventoryReport));
        OnPropertyChanged(nameof(IsTransmittalReport));
    }

    private async Task GenerateInventory()
    {
        // This method is now superseded by PreviewReport and ExportReport
        // Its logic has been moved to FetchInventoryData and ExportReport
    }

    [ObservableProperty]
    private ObservableCollection<object> _reportData = new();
    
    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private bool _isBusy;

    partial void OnReportDataChanged(ObservableCollection<object> value)
    {
        HasData = value != null && value.Any();
        if (value != null)
        {
            value.CollectionChanged += (s, e) => HasData = _reportData.Any();
        }
    }

    [RelayCommand]
    private async Task PreviewReport()
    {
        IsBusy = true;
        ReportData.Clear();
        try
        {
            if (SelectedReportType == "تقرير تسليم مراسلات (Transmittal)")
            {
               var items = await FetchTransmittalData();
               foreach(var item in items) ReportData.Add(item);
            }
            else if (SelectedReportType == "تقرير حصر شامل")
            {
               var items = await FetchInventoryData();
               foreach(var item in items) ReportData.Add(item);
            }
            else if (SelectedReportType == "تقرير الموقف التنفيذي للمهندسين")
            {
               var items = await FetchEngineerPerformanceData();
               foreach(var item in items) ReportData.Add(item);
            }
        }
        catch (Exception ex)
        {
             MessageBox.Show($"خطأ أثناء البحث: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            HasData = ReportData.Any();
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ExportReport()
    {
        if (!HasData) return;

        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"Report_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                if (IsTransmittalReport)
                {
                    if (string.IsNullOrWhiteSpace(RecipientName))
                    {
                        MessageBox.Show("يرجى إدخال اسم المستلم", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                     _reportingService.GenerateTransmittalReport(dialog.FileName, "كشف تسليم مراسلات", ReportData.Cast<TransmittalItem>(), RecipientName);
                }
                else if (IsInventoryReport)
                {
                     var headerTitle = "كشف حصر شامل للمراسلات";
                     if (SelectedStatus.HasValue) headerTitle += $" - {SelectedStatus}";
                     _reportingService.GenerateInventoryReport(dialog.FileName, headerTitle, ReportData.Cast<InventoryItem>(), FromDate, ToDate);
                }
                else
                {
                     _reportingService.GenerateEngineerPerformanceReport(dialog.FileName, "تقرير الموقف التنفيذي للمهندسين", ReportData.Cast<EngineerPerformanceItem>(), FromDate, ToDate);
                }

                var p = new System.Diagnostics.Process();
                p.StartInfo = new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true };
                p.Start();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ أثناء التصدير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    // Fetch Methods
    private async Task<List<TransmittalItem>> FetchTransmittalData()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Inbounds
            .Where(i => i.InboundDate >= FromDate.ToUniversalTime() && i.InboundDate <= ToDate.ToUniversalTime())
            .OrderByDescending(i => i.InboundDate)
            .Select(i => new TransmittalItem
            {
                Subject = i.Subject,
                Date = i.InboundDate.ToLocalTime(),
                Sender = i.FromEntity ?? "غير محدد"
            })
            .ToListAsync();
    }

    private async Task<List<InventoryItem>> FetchInventoryData()
    {
        using var context = _contextFactory.CreateDbContext();
        
        var query = context.Inbounds
            .Include(i => i.ResponsibleEngineers)
            .ThenInclude(re => re.Engineer)
            .Where(i => i.InboundDate >= FromDate.ToUniversalTime() && i.InboundDate <= ToDate.ToUniversalTime());

        if (SelectedStatus.HasValue)
            query = query.Where(i => i.Status == SelectedStatus.Value);

        if (!string.IsNullOrWhiteSpace(SearchText))
            query = query.Where(i => i.Subject.Contains(SearchText) || 
                                     (i.Code != null && i.Code.Contains(SearchText)) ||
                                     i.SubjectNumber.Contains(SearchText));

        return await query
            .OrderByDescending(i => i.InboundDate)
            .Select(i => new InventoryItem
            {
                SubjectNumber = i.SubjectNumber,
                Subject = i.Subject,
                Date = i.InboundDate.ToLocalTime(),
                FromEntity = i.FromEntity ?? "غير محدد",
                Status = i.Status.ToString(),
                ResponsibleEngineer = i.ResponsibleEngineers.Any() 
                    ? string.Join(", ", i.ResponsibleEngineers.Select(re => re.Engineer.FullName))
                    : i.ResponsibleEngineer ?? "-"
            })
            .ToListAsync();
    }

    private async Task<List<EngineerPerformanceItem>> FetchEngineerPerformanceData()
    {
        using var context = _contextFactory.CreateDbContext();
        
        var data = await context.Inbounds
            .Where(i => i.InboundDate >= FromDate.ToUniversalTime() &&
                        i.InboundDate <= ToDate.ToUniversalTime())
            .Include(i => i.ResponsibleEngineers)
            .ThenInclude(re => re.Engineer)
            .Select(i => new 
            {
                i.InboundDate,
                i.Status,
                LegacyName = i.ResponsibleEngineer,
                LinkedEngineers = i.ResponsibleEngineers.Select(re => re.Engineer.FullName).ToList()
            })
            .ToListAsync();

        var flatList = new List<(string Name, dynamic Item)>();

        foreach (var item in data)
        {
            if (item.LinkedEngineers.Any())
            {
                foreach (var name in item.LinkedEngineers) flatList.Add((name, item));
            }
            else if (!string.IsNullOrWhiteSpace(item.LegacyName))
            {
                flatList.Add((item.LegacyName, item));
            }
        }

        return flatList
            .GroupBy(x => x.Name)
            .Select(g => 
            {
                var total = g.Count();
                var open = g.Count(x => x.Item.Status != DCMS.Domain.Enums.CorrespondenceStatus.Closed);
                var delayed = g.Count(x => x.Item.Status != DCMS.Domain.Enums.CorrespondenceStatus.Closed && 
                                           ((DateTime)x.Item.InboundDate).AddDays(7) < DateTime.UtcNow);

                return new EngineerPerformanceItem
                {
                    EngineerName = g.Key,
                    OpenCount = open,
                    DelayedCount = delayed,
                    CompletionRate = total > 0 ? (double)(total - open) / total : 0
                };
            })
            .OrderByDescending(x => x.OpenCount)
            .ToList();
    }
}
