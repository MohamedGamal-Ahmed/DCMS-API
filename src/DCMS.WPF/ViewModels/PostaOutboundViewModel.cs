using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using DCMS.Domain.Entities;
using DCMS.Infrastructure.Data;
using DCMS.WPF.Helpers;
using Microsoft.EntityFrameworkCore;
using DCMS.WPF.Services;
using DCMS.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using DCMS.Infrastructure.Services;

namespace DCMS.WPF.ViewModels;

public class PostaOutboundViewModel : ViewModelBase
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly NotificationService _notificationService;
    private readonly NumberingService _numberingService;
    private readonly CodeLookupService _codeLookupService;
    private readonly Services.CurrentUserService _currentUserService;
    private readonly IServiceProvider _serviceProvider;
    private string _subjectNumber = string.Empty;
    private string _code = string.Empty;
    private string _toEntity = string.Empty;
    private string _toEngineer = string.Empty;
    private string _subject = string.Empty;
    private string _relatedInboundNo = string.Empty;
    private DateTime _outboundDate = DateTime.Now;
    private string _attachmentUrl = string.Empty;
    private bool _isSaving;

    public PostaOutboundViewModel(IDbContextFactory<DCMSDbContext> contextFactory, NotificationService notificationService, Services.CurrentUserService currentUserService, NumberingService numberingService, CodeLookupService codeLookupService, IServiceProvider serviceProvider)
    {
        _contextFactory = contextFactory;
        _notificationService = notificationService;
        _numberingService = numberingService;
        _codeLookupService = codeLookupService;
        _currentUserService = currentUserService;
        _serviceProvider = serviceProvider;

        SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
        ClearCommand = new RelayCommand(ExecuteClear);
        ManageCodesCommand = new RelayCommand(ExecuteManageCodes);

        // Initialize Engineer Selectors
        ResponsibleEngineerSelector = new EngineerSelectorViewModel(_contextFactory, responsibleEngineersOnly: true);
        TransferredToSelector = new EngineerSelectorViewModel(_contextFactory, responsibleEngineersOnly: false);

        LoadNextSubjectNumber();
    }

    // Code Lookup Integration
    public ObservableCollection<CodeEntry> AvailableCodes => _codeLookupService.AvailableCodes;
    
    private CodeEntry? _selectedCodeEntry;
    public CodeEntry? SelectedCodeEntry
    {
        get => _selectedCodeEntry;
        set
        {
            if (SetProperty(ref _selectedCodeEntry, value))
            {
                if (value != null)
                {
                    // Auto-fill logic (For Outbound, we fill "To" fields)
                    Code = value.Code;
                    ToEntity = value.Entity;
                    ToEngineer = value.Engineer;
                }
            }
        }
    }

    public EngineerSelectorViewModel ResponsibleEngineerSelector { get; }
    public EngineerSelectorViewModel TransferredToSelector { get; }

    public string SubjectNumber
    {
        get => _subjectNumber;
        set => SetProperty(ref _subjectNumber, value);
    }

    public string Code
    {
        get => _code;
        set => SetProperty(ref _code, value?.ToUpperInvariant() ?? string.Empty);
    }

    public string ToEntity { get => _toEntity; set => SetProperty(ref _toEntity, value); }
    public string ToEngineer { get => _toEngineer; set => SetProperty(ref _toEngineer, value); }
    public string Subject { get => _subject; set => SetProperty(ref _subject, value); }
    public string RelatedInboundNo { get => _relatedInboundNo; set => SetProperty(ref _relatedInboundNo, value); }
    public DateTime OutboundDate { get => _outboundDate; set => SetProperty(ref _outboundDate, value); }
    public string AttachmentUrl { get => _attachmentUrl; set => SetProperty(ref _attachmentUrl, value); }
    public bool IsSaving { get => _isSaving; set => SetProperty(ref _isSaving, value); }

    public ICommand SaveCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand ManageCodesCommand { get; }

    public bool IsAdmin => _currentUserService.CurrentUser?.Role == UserRole.Admin || _currentUserService.CurrentUser?.Role == UserRole.OfficeManager;

    private void ExecuteManageCodes(object? parameter)
    {
        var viewModel = _serviceProvider.GetRequiredService<ViewModels.CodesManagerViewModel>();
        var view = new Views.CodesManagerView { DataContext = viewModel };
        view.ShowDialog();
    }

    private void LoadNextSubjectNumber()
    {
        SubjectNumber = "تلقائي";
    }

    private bool CanExecuteSave(object? parameter) => !string.IsNullOrWhiteSpace(Subject) && !IsSaving;

    private async void ExecuteSave(object? parameter)
    {
        IsSaving = true;

        try
        {
            if (!ResponsibleEngineerSelector.SelectedEngineers.Any())
            {
                MessageBox.Show("يرجى اختيار مهندس مسئول واحد على الأقل.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsSaving = false;
                return;
            }

            // Generate number on save
            var generatedNumber = await _numberingService.GenerateNextOutboundNumberAsync();
            SubjectNumber = generatedNumber;

            var outbound = new Outbound
            {
                SubjectNumber = generatedNumber,
                Code = Code,
                ToEntity = ToEntity,
                ToEngineer = ToEngineer,
                Subject = Subject,
                RelatedInboundNo = RelatedInboundNo,
                OutboundDate = OutboundDate.ToUniversalTime(),
                AttachmentUrls = !string.IsNullOrEmpty(AttachmentUrl) ? new List<string> { AttachmentUrl } : new List<string>(),
                ResponsibleEngineer = string.Join(", ", ResponsibleEngineerSelector.SelectedEngineers.Select(e => e.FullName)),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            using var context = await _contextFactory.CreateDbContextAsync();
            context.Outbounds.Add(outbound);
            await context.SaveChangesAsync();

            // Add notification
            await _notificationService.AddNotification($"تم إضافة صادر جديد: {outbound.Subject}", outbound.Id.ToString(), NotificationType.Success);

            MessageBox.Show($"تم حفظ الصادر بنجاح!\nرقم الصادر: {generatedNumber}", "نجاح", 
                MessageBoxButton.OK, MessageBoxImage.Information);

            ExecuteClear(null);
            LoadNextSubjectNumber();
        }
        catch (Exception ex)
        {
            var errorMessage = $"حدث خطأ أثناء الحفظ:\n{ex.Message}";
            if (ex.InnerException != null) errorMessage += $"\n\nتفاصيل الخطأ:\n{ex.InnerException.Message}";
            MessageBox.Show(errorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void ExecuteClear(object? parameter)
    {
        Code = string.Empty;
        ToEntity = string.Empty;
        ToEngineer = string.Empty;
        Subject = string.Empty;
        RelatedInboundNo = string.Empty;
        OutboundDate = DateTime.Now;
        AttachmentUrl = string.Empty;
        ResponsibleEngineerSelector.SelectedEngineers.Clear();
        TransferredToSelector.SelectedEngineers.Clear();
        LoadNextSubjectNumber();
    }
}
