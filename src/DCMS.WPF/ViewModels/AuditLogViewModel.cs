using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using DCMS.Application.Interfaces;

namespace DCMS.WPF.ViewModels;

public class AuditLogViewModel : ViewModelBase
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly Services.ExcelExportService _excelExportService;

    private ObservableCollection<AuditLog> _logs = new();
    private ObservableCollection<string> _userNames = new() { "الكل" };
    private ObservableCollection<string> _actionTypes = new() { "الكل", "إضافة", "تعديل", "حذف" };
    private ObservableCollection<string> _entityTypes = new() { "الكل", "Inbound", "Outbound", "Meeting", "Engineer", "User" };
    private string? _selectedUserName = "الكل";
    private string? _selectedAction = "الكل";
    private string? _selectedEntityType = "الكل";
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private AuditLog? _selectedLog;
    private bool _isLoading;

    public ObservableCollection<AuditLog> Logs
    {
        get => _logs;
        set => SetProperty(ref _logs, value);
    }

    public ObservableCollection<string> UserNames
    {
        get => _userNames;
        set => SetProperty(ref _userNames, value);
    }

    public ObservableCollection<string> ActionTypes
    {
        get => _actionTypes;
        set => SetProperty(ref _actionTypes, value);
    }

    public ObservableCollection<string> EntityTypes
    {
        get => _entityTypes;
        set => SetProperty(ref _entityTypes, value);
    }

    public string? SelectedUserName
    {
        get => _selectedUserName;
        set
        {
            SetProperty(ref _selectedUserName, value);
            _ = LoadLogsAsync();
        }
    }

    public string? SelectedAction
    {
        get => _selectedAction;
        set
        {
            SetProperty(ref _selectedAction, value);
            _ = LoadLogsAsync();
        }
    }

    public string? SelectedEntityType
    {
        get => _selectedEntityType;
        set
        {
            SetProperty(ref _selectedEntityType, value);
            _ = LoadLogsAsync();
        }
    }

    public DateTime? FromDate
    {
        get => _fromDate;
        set
        {
            SetProperty(ref _fromDate, value);
            _ = LoadLogsAsync();
        }
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set
        {
            SetProperty(ref _toDate, value);
            _ = LoadLogsAsync();
        }
    }

    public AuditLog? SelectedLog
    {
        get => _selectedLog;
        set => SetProperty(ref _selectedLog, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand ViewDetailsCommand { get; }
    public ICommand ExportCommand { get; }

    public ObservableCollection<AuditLog> FilteredLogs => Logs;

    public AuditLogViewModel(
        IDbContextFactory<DCMSDbContext> contextFactory,
        ICurrentUserService currentUserService,
        Services.ExcelExportService excelExportService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _excelExportService = excelExportService;

        RefreshCommand = new RelayCommand(async _ => await LoadLogsAsync());
        ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
        ViewDetailsCommand = new RelayCommand(param => ViewDetails(param as AuditLog));
        ExportCommand = new RelayCommand(_ => ExportToExcel());

        // Check permissions
        if (!HasPermission())
        {
            System.Windows.MessageBox.Show(
                "ليس لديك صلاحية للوصول إلى سجل العمليات",
                "تنبيه",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        _ = LoadLogsAsync();
        _ = LoadUserNamesAsync();
    }

    private bool HasPermission()
    {
        return _currentUserService.IsLoggedIn;
    }

    private void ResetFilters()
    {
        SelectedUserName = "الكل";
        SelectedAction = "الكل";
        SelectedEntityType = "الكل";
        FromDate = null;
        ToDate = null;
        _ = LoadLogsAsync();
    }

    private void ViewDetails(AuditLog? log)
    {
        if (log == null) return;

        try
        {
            var detailsWindow = new Views.AuditLogDetailsDialog
            {
                DataContext = log
            };
            
            var mainWindow = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (mainWindow != null)
            {
                detailsWindow.Owner = mainWindow;
            }
            
            detailsWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"حدث خطأ أثناء عرض التفاصيل:\n{ex.Message}",
                "خطأ",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void ExportToExcel()
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                var dataToExport = Logs.ToList();
                _excelExportService.ExportAuditLog(dataToExport, dialog.FileName);
                
                System.Windows.MessageBox.Show(
                    "تم تصدير البيانات بنجاح!",
                    "نجاح",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"حدث خطأ أثناء التصدير:\n{ex.Message}",
                "خطأ",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task LoadLogsAsync()
    {
        IsLoading = true;
        
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            var query = context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(SelectedUserName) && SelectedUserName != "الكل")
            {
                query = query.Where(log => log.UserName == SelectedUserName);
            }

            if (!string.IsNullOrEmpty(SelectedAction) && SelectedAction != "الكل")
            {
                var actionType = SelectedAction switch
                {
                    "إضافة" => AuditActionType.Create,
                    "تعديل" => AuditActionType.Update,
                    "حذف" => AuditActionType.Delete,
                    _ => (AuditActionType?)null
                };

                if (actionType.HasValue)
                {
                    query = query.Where(log => log.Action == actionType.Value);
                }
            }

            if (!string.IsNullOrEmpty(SelectedEntityType) && SelectedEntityType != "الكل")
            {
                query = query.Where(log => log.EntityType == SelectedEntityType);
            }

            if (FromDate.HasValue)
            {
                query = query.Where(log => log.Timestamp >= FromDate.Value);
            }

            if (ToDate.HasValue)
            {
                var endOfDay = ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(log => log.Timestamp <= endOfDay);
            }

            var logs = await query
                .OrderByDescending(log => log.Timestamp)
                .Take(1000)
                .ToListAsync();

            Logs.Clear();
            foreach (var log in logs)
            {
                Logs.Add(log);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"حدث خطأ أثناء تحميل السجلات:\n{ex.Message}",
                "خطأ",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadUserNamesAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            var users = await context.AuditLogs
                .Select(log => log.UserName)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();

            UserNames.Clear();
            UserNames.Add("الكل");
            foreach (var user in users)
            {
                UserNames.Add(user);
            }
        }
        catch
        {
            // Ignore errors
        }
    }
}
