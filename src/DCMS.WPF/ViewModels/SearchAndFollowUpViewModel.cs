using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using DCMS.WPF.Helpers;
using Microsoft.Extensions.DependencyInjection;
using DCMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using DCMS.WPF.Services;
using DCMS.WPF.Views;
using DCMS.Domain.Models;
using DCMS.Application.Models;
using DCMS.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DCMS.WPF.ViewModels;

public partial class SearchAndFollowUpViewModel : ViewModelBase
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly NotificationService _notificationService;
    private readonly CurrentUserService _currentUserService;
    private readonly ExcelExportService _excelExportService;
    private readonly NumberingService _numberingService;
    private readonly RecentItemsService _recentItemsService;
    private readonly ISearchService _searchService;
    private readonly IEngineerService _engineerService;
    private readonly ReportingService _reportingService;

    [ObservableProperty] private ObservableCollection<object> _searchResults = new();
    [ObservableProperty] private object? _selectedRecord;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private SearchRecordType? _selectedRecordType;
    [ObservableProperty] private CorrespondenceStatus? _selectedStatus;
    [ObservableProperty] private DateTime? _startDate;
    [ObservableProperty] private DateTime? _endDate;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(PaginationInfo))] private bool _isBusy;
    [ObservableProperty] private string _searchCode = string.Empty;
    [ObservableProperty] private string _searchSubject = string.Empty;
    [ObservableProperty] private string _searchFrom = string.Empty;
    [ObservableProperty] private string _searchTo = string.Empty;
    [ObservableProperty] private System.Windows.Controls.ComboBoxItem? _selectedContractType;
    [ObservableProperty] private string _searchTransferredTo = string.Empty;
    [ObservableProperty] private ObservableCollection<Engineer> _availableEngineers = new();
    [ObservableProperty] private ObservableCollection<Engineer> _mainEngineers = new();
    [ObservableProperty] private ObservableCollection<int> _availableYears = new();
    [ObservableProperty] private int _selectedYear;
    [ObservableProperty] private string _searchSubjectNumber = string.Empty;
    [ObservableProperty] private string _searchResponsibleEngineer = string.Empty;
    [ObservableProperty] private ViewModelBase? _activeEntryViewModel;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(FirstPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(LastPageCommand))]
    private int _currentPage = 1;

    [ObservableProperty] private int _pageSize = 50;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(LastPageCommand))]
    private int _totalPages = 1;

    [ObservableProperty] private int _totalRecords = 0;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public bool IsOutboundSelected => SelectedRecordType == SearchRecordType.Outbound;
    public string PaginationInfo => $"صفحة {CurrentPage} من {TotalPages} (إجمالي: {TotalRecords} سجل)";

    partial void OnSelectedRecordTypeChanged(SearchRecordType? value) => _ = Search(null);
    partial void OnSelectedContractTypeChanged(System.Windows.Controls.ComboBoxItem? value) => _ = Search(null);
    partial void OnSelectedYearChanged(int value) => _ = Search(null);
    partial void OnPageSizeChanged(int value) { CurrentPage = 1; _ = Search(null); }
    partial void OnTotalRecordsChanged(int value) => OnPropertyChanged(nameof(PaginationInfo));
    partial void OnTotalPagesChanged(int value) => OnPropertyChanged(nameof(PaginationInfo));
    partial void OnCurrentPageChanged(int value) => OnPropertyChanged(nameof(PaginationInfo));

    public SearchAndFollowUpViewModel(
        IDbContextFactory<DCMSDbContext> contextFactory, 
        IServiceProvider serviceProvider, 
        NotificationService notificationService, 
        CurrentUserService currentUserService, 
        ExcelExportService excelExportService, 
        NumberingService numberingService, 
        RecentItemsService recentItemsService,
        ISearchService searchService,
        IEngineerService engineerService)
    {
        _contextFactory = contextFactory;
        _serviceProvider = serviceProvider;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _excelExportService = excelExportService;
        _numberingService = numberingService;
        _recentItemsService = recentItemsService;
        _searchService = searchService;
        _engineerService = engineerService;
        _reportingService = serviceProvider.GetService(typeof(ReportingService)) as ReportingService ?? new ReportingService();
        
        _ = Task.Run(async () => { await LoadEngineers(); await LoadYears(); await Search(null); });
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void NextPage() { CurrentPage++; _ = Search(null); }
    private bool CanGoNext() => CurrentPage < TotalPages;

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private void PreviousPage() { CurrentPage--; _ = Search(null); }
    private bool CanGoPrevious() => CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private void FirstPage() { CurrentPage = 1; _ = Search(null); }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void LastPage() { CurrentPage = TotalPages; _ = Search(null); }

    [RelayCommand]
    private void Edit(object? parameter)
    {
        var record = parameter ?? SelectedRecord;
        if (record is Inbound inbound)
        {
            var editVm = new EditFollowUpViewModel(_contextFactory, _notificationService, _currentUserService, inbound);
            var editDialog = new EditFollowUpDialog(editVm);
            
            editVm.RequestClose += () => 
            {
                editDialog.Close();
                _ = Search(null); // Refresh results
            };

            editDialog.ShowDialog();
        }
        else if (record is Outbound outbound)
        {
            MessageBox.Show("تعديل المراسلات الصادرة غير مدعوم حالياً", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    [RelayCommand]
    private void ViewDetails(object? parameter)
    {
        var record = parameter ?? SelectedRecord;
        if (record == null) return;

        if (record is Inbound inbound)
        {
            var detailsVm = new InboundDetailsViewModel(_contextFactory, _serviceProvider, _notificationService, _currentUserService, _currentUserService.CurrentUser, inbound);
            var detailsView = new InboundDetailsView();
            detailsView.DataContext = detailsVm;
            
            detailsVm.RequestClose += () => detailsView.Close();

            detailsView.ShowDialog();
            
            // Add to Recent Items
            _recentItemsService.AddToRecent(inbound.Id.ToString(), inbound.Subject, RecentItemType.Inbound);
        }
        else if (record is Outbound outbound)
        {
            var detailsVm = new OutboundDetailsViewModel(_contextFactory, _serviceProvider, _currentUserService, outbound);
            var detailsView = new OutboundDetailsView();
            detailsView.DataContext = detailsVm;
            
            // Allow close from VM
            detailsVm.RequestClose += () => detailsView.Close();
            
            detailsView.ShowDialog();
            
            // Add to Recent Items
            _recentItemsService.AddToRecent(outbound.Id.ToString(), outbound.Subject, RecentItemType.Outbound);
        }
    }

    private async Task LoadEngineers()
    {
        try {
            var engineers = await _engineerService.GetActiveEngineersAsync();
            var responsible = await _engineerService.GetResponsibleEngineersAsync();
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                AvailableEngineers.Clear(); MainEngineers.Clear();
                var allOption = new Engineer { Id = 0, FullName = "الكل" };
                AvailableEngineers.Add(allOption); MainEngineers.Add(allOption);
                foreach (var e in engineers) AvailableEngineers.Add(e);
                foreach (var e in responsible) MainEngineers.Add(e);
            });
        } catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    private async Task LoadYears()
    {
        try {
            var years = await _numberingService.GetAvailableYearsAsync();
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                AvailableYears.Clear(); AvailableYears.Add(0);
                foreach (var year in years) AvailableYears.Add(year);
            });
        } catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private async Task Search(object? parameter)
    {
        if (IsBusy) return;
        IsBusy = true; ErrorMessage = string.Empty;
        try {
            await Task.Delay(500); // UI Polish: Ensure skeleton is visible for a moment
            var (items, totalCount) = await _searchService.SearchAsync(GetSearchCriteria(), CurrentPage, PageSize);
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                TotalRecords = totalCount;
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
                SearchResults = new ObservableCollection<object>(items);
            });
        } catch (Exception ex) { ErrorMessage = $"حدث خطأ في البحث: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    private SearchCriteria GetSearchCriteria() => new SearchCriteria {
        RecordType = SelectedRecordType, Status = SelectedStatus, StartDate = StartDate, EndDate = EndDate,
        Code = SearchCode, Subject = SearchSubject, SubjectNumber = SearchSubjectNumber, From = SearchFrom, To = SearchTo,
        ResponsibleEngineer = SearchResponsibleEngineer, TransferredTo = SearchTransferredTo, SelectedYear = SelectedYear,
        SearchQuery = SearchQuery, ContractType = SelectedContractType?.Tag?.ToString()
    };

    [RelayCommand]
    private void ClearFilters()
    {
        SearchQuery = SearchCode = SearchSubject = SearchSubjectNumber = SearchFrom = SearchTo = string.Empty;
        SearchTransferredTo = SearchResponsibleEngineer = "الكل";
        StartDate = null; EndDate = null; SelectedStatus = null; SelectedRecordType = null;
        SelectedYear = 0; _ = Search(null);
    }

    public void ApplyDashboardFilters(CorrespondenceStatus? status, string? engineer, bool onlyOverdue, DateTime? fromDate = null, DateTime? toDate = null, string? entity = null, bool onlyOutbound = false)
    {
        ClearFilters();
        if (onlyOutbound) SelectedRecordType = SearchRecordType.Outbound;
        if (status.HasValue) SelectedStatus = status.Value;
        if (!string.IsNullOrEmpty(engineer)) SearchTransferredTo = engineer;
        if (!string.IsNullOrEmpty(entity)) SearchFrom = entity;
        if (onlyOverdue) { SelectedStatus = CorrespondenceStatus.InProgress; EndDate = DateTime.Now.AddDays(-7); }
        if (fromDate.HasValue) StartDate = fromDate;
        if (toDate.HasValue) EndDate = toDate;
        _ = Search(null);
    }

    [RelayCommand]
    private async Task UpdateStatus(object? parameter)
    {
        if (SelectedRecord is not Inbound selectedInbound || parameter is not CorrespondenceStatus newStatus) return;
        try {
            using var context = await _contextFactory.CreateDbContextAsync();
            var inbound = await context.Inbounds.FindAsync(selectedInbound.Id);
            if (inbound != null) {
                inbound.Status = newStatus; await context.SaveChangesAsync();
                selectedInbound.Status = newStatus; MessageBox.Show("تم تحديث الحالة بنجاح");
            }
        } catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    [RelayCommand]
    private async Task SaveResponse(object? parameter)
    {
        if (parameter is not InboundTransfer transfer) return;
        try {
            using var context = await _contextFactory.CreateDbContextAsync();
            var trackedTransfer = await context.InboundTransfers.FindAsync(transfer.InboundId, transfer.EngineerId);
            if (trackedTransfer != null) {
                trackedTransfer.Response = transfer.Response; trackedTransfer.ResponseDate = DateTime.UtcNow;
                await context.SaveChangesAsync(); transfer.ResponseDate = DateTime.UtcNow;
                MessageBox.Show("تم حفظ الرد بنجاح");
            }
        } catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    [RelayCommand]
    private async Task Export()
    {
        if (SearchResults.Count == 0) return;
        string reportTitle = string.IsNullOrWhiteSpace(SearchTransferredTo) || SearchTransferredTo == "الكل" ? "التقرير العام للمراسلات" : $"تقرير المراسلات المحولة للمهندس / {SearchTransferredTo}";
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "PDF Files (*.pdf)|*.pdf", FileName = $"Report_{DateTime.Now:yyyyMMdd}.pdf" };
        if (dialog.ShowDialog() == true) {
            IsBusy = true;
            try {
                await _searchService.ExportToPdfAsync(GetSearchCriteria(), dialog.FileName, reportTitle);
                new System.Diagnostics.Process { StartInfo = new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true } }.Start();
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
            finally { IsBusy = false; }
        }
    }

    [RelayCommand]
    private void OpenInboundForm(string type)
    {
        try
        {
            ViewModelBase? viewModel = type switch
            {
                "Posta" or "بوسطة" => _serviceProvider.GetRequiredService<PostaInboundViewModel>(),
                "Email" or "إيميل" => _serviceProvider.GetRequiredService<EmailInboundViewModel>(),
                "Request" or "طلبات" or "شكاوى" => _serviceProvider.GetRequiredService<RequestInboundViewModel>(),
                "Mission" or "مأموريات" or "عهدة" or "تفويضات" => _serviceProvider.GetRequiredService<MissionInboundViewModel>(),
                "Contract" or "عقود" => _serviceProvider.GetRequiredService<ContractInboundViewModel>(),
                _ => null
            };

            if (viewModel != null)
            {
                viewModel.RequestClose += () =>
                {
                    ActiveEntryViewModel = null;
                    _ = Search(null);
                };
                ActiveEntryViewModel = viewModel;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في فتح النافذة ({type}): {ex.Message}\n{ex.InnerException?.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void OpenOutboundForm()
    {
        var viewModel = _serviceProvider.GetRequiredService<PostaOutboundViewModel>();
        viewModel.RequestClose += () =>
        {
            ActiveEntryViewModel = null;
            _ = Search(null);
        };
        ActiveEntryViewModel = viewModel;
    }

    [RelayCommand]
    private void CloseEntryForm()
    {
        ActiveEntryViewModel = null;
    }

}
