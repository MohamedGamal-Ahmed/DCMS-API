using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DCMS.Infrastructure.Services;
using DCMS.WPF.Services;

using System.Linq;
using System.Timers;
using DCMS.Application.DTOs;
using DCMS.Application.Interfaces;

namespace DCMS.WPF.ViewModels;

public class PostaInboundViewModel : ViewModelBase
{
    public string Title => "إضافة بوسطة وارد";
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly NotificationService _notificationService;
    private readonly Services.CurrentUserService _currentUserService;
    private readonly NumberingService _numberingService;
    private readonly CodeLookupService _codeLookupService;
    private string _subjectNumber = string.Empty;
    private string _code = string.Empty;
    private string _fromEntity = string.Empty;
    private string _fromEngineer = string.Empty;
    private string _subject = string.Empty;
    private DateTime _inboundDate = DateTime.Now;
    private string _reply = string.Empty;
    private string _status = "جديد";
    private string _attachmentUrl = string.Empty;
    private bool _isSaving;
    private readonly System.Timers.Timer? _debounceTimer;
    private string _duplicateWarning = string.Empty;
    private List<AiSearchResultDto> _similarItems = new();

    private readonly IServiceProvider _serviceProvider;

    public PostaInboundViewModel(IDbContextFactory<DCMSDbContext> contextFactory, NotificationService notificationService, Services.CurrentUserService currentUserService, NumberingService numberingService, CodeLookupService codeLookupService, IServiceProvider serviceProvider)
    {
        _contextFactory = contextFactory;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _numberingService = numberingService;
        _codeLookupService = codeLookupService;
        _serviceProvider = serviceProvider;

        SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
        ClearCommand = new RelayCommand(ExecuteClear);
        CancelCommand = new RelayCommand(_ => OnRequestClose());
        ManageCodesCommand = new RelayCommand(ExecuteManageCodes);
        
        ResponsibleEngineerSelector = new EngineerSelectorViewModel(_contextFactory, responsibleEngineersOnly: true);
        
        // Setup Debounce Timer for Duplicate Detection
        _debounceTimer = new System.Timers.Timer(500); // 500ms debounce
        _debounceTimer.AutoReset = false;
        _debounceTimer.Elapsed += async (s, e) => await CheckForDuplicates();

        // Generate auto-number
        LoadNextSubjectNumber();
    }

    private void LoadNextSubjectNumber()
    {
        SubjectNumber = "تلقائي";
    }

    public string SubjectNumber { get => _subjectNumber; set => SetProperty(ref _subjectNumber, value); }

    public string Code { get => _code; set => SetProperty(ref _code, value?.ToUpperInvariant() ?? string.Empty); }
    public string FromEntity { get => _fromEntity; set => SetProperty(ref _fromEntity, value); }
    public string FromEngineer { get => _fromEngineer; set => SetProperty(ref _fromEngineer, value); }
    public string Subject 
    { 
        get => _subject; 
        set 
        {
            if (SetProperty(ref _subject, value))
            {
                // Trigger debounce timer
                _debounceTimer?.Stop();
                _debounceTimer?.Start();
            }
        } 
    }
    
    public string DuplicateWarning { get => _duplicateWarning; set => SetProperty(ref _duplicateWarning, value); }
    public List<AiSearchResultDto> SimilarItems { get => _similarItems; set => SetProperty(ref _similarItems, value); }
    public DateTime InboundDate { get => _inboundDate; set => SetProperty(ref _inboundDate, value); }
    public string Reply { get => _reply; set => SetProperty(ref _reply, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public string AttachmentUrl { get => _attachmentUrl; set => SetProperty(ref _attachmentUrl, value); }
    public bool IsSaving { get => _isSaving; set => SetProperty(ref _isSaving, value); }

    public EngineerSelectorViewModel ResponsibleEngineerSelector { get; set; }

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
                    // Auto-fill logic
                    Code = value.Code;
                    FromEntity = value.Entity;
                    FromEngineer = value.Engineer;
                }
            }
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ManageCodesCommand { get; }

    public bool IsAdmin => _currentUserService.CurrentUser?.Role == UserRole.Admin || _currentUserService.CurrentUser?.Role == UserRole.OfficeManager;

    private void ExecuteManageCodes(object? parameter)
    {
        var viewModel = _serviceProvider.GetRequiredService<ViewModels.CodesManagerViewModel>();
        var view = new Views.CodesManagerView { DataContext = viewModel };
        view.ShowDialog();
    }

    private bool CanExecuteSave(object? parameter) => !string.IsNullOrWhiteSpace(Subject) && !IsSaving;

    private async void ExecuteSave(object? parameter)
    {
        IsSaving = true;
        try
        {
            if (!ResponsibleEngineerSelector.SelectedEngineers.Any())
            {
                _notificationService.Warning("يرجى اختيار مهندس مسئول واحد على الأقل.");
                IsSaving = false;
                return;
            }

            // Generate number on save
            var generatedNumber = await _numberingService.GenerateNextInboundNumberAsync();
            SubjectNumber = generatedNumber;

            var statusEnum = CorrespondenceStatus.New;
            if (Status == "جاري العمل") statusEnum = CorrespondenceStatus.InProgress;
            if (Status == "منتهي") statusEnum = CorrespondenceStatus.Completed;

            var inbound = new Inbound
            {
                SubjectNumber = generatedNumber,
                Code = Code,
                Category = InboundCategory.Posta,
                FromEntity = FromEntity,
                FromEngineer = FromEngineer,
                Subject = Subject,
                InboundDate = InboundDate.ToUniversalTime(),
                Reply = Reply,
                Status = statusEnum,
                AttachmentUrl = AttachmentUrl,
                ResponsibleEngineer = string.Join(", ", ResponsibleEngineerSelector.SelectedEngineers.Select(e => e.FullName)),
                CreatedByUserId = _currentUserService.CurrentUser?.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            using var context = await _contextFactory.CreateDbContextAsync();
            context.Inbounds.Add(inbound);
            await context.SaveChangesAsync();

            foreach (var engineer in ResponsibleEngineerSelector.SelectedEngineers)
            {
                // Re-attach engineer to current context to avoid tracking issues
                var engineerInContext = await context.Engineers.FindAsync(engineer.Id);
                if (engineerInContext != null)
                {
                    context.InboundResponsibleEngineers.Add(new InboundResponsibleEngineer
                    {
                        InboundId = inbound.Id,
                        EngineerId = engineerInContext.Id
                    });
                }
            }

            await context.SaveChangesAsync();

            // Send Notification
            // Use current user name for notification message if available
            var creatorName = _currentUserService.CurrentUser?.Username ?? "System";
            _notificationService.Success($"✅ تم الحفظ بنجاح! رقم الوارد: {generatedNumber}");
            await _notificationService.AddNotification($"تم إضافة وارد جديد بواسطة {creatorName}: {inbound.Subject}", inbound.Id.ToString(), NotificationType.Success);
            ExecuteClear(null);
            OnRequestClose();
        }
        catch (Exception ex)
        {
            var errorMessage = $"حدث خطأ أثناء الحفظ:\n{ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nتفاصيل الخطأ:\n{ex.InnerException.Message}";
            }
            _notificationService.Error(errorMessage);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void ExecuteClear(object? parameter)
    {
        Code = string.Empty;
        FromEntity = string.Empty;
        FromEngineer = string.Empty;
        Subject = string.Empty;
        InboundDate = DateTime.Now;
        Reply = string.Empty;
        ResponsibleEngineerSelector.SelectedEngineers.Clear();
        Status = "جديد";
        AttachmentUrl = string.Empty;
        DuplicateWarning = string.Empty;
        SimilarItems.Clear();
        LoadNextSubjectNumber();
    }

    private async Task CheckForDuplicates()
    {
        if (string.IsNullOrWhiteSpace(Subject) || Subject.Length < 3)
        {
            DuplicateWarning = string.Empty;
            return;
        }

        try
        {
            // We need to resolve ICorrespondenceService
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ICorrespondenceService>();
            
            var matches = await service.GetSimilarInboundsAsync(Subject);
            
            if (matches.Any())
            {
                SimilarItems = matches;
                var first = matches.First();
                DuplicateWarning = $"⚠️ تنبيه: يوجد موضوع مشابه مسجل برقم ({first.SubjectNumber}) بتاريخ {first.Date}";
            }
            else
            {
                DuplicateWarning = string.Empty;
                SimilarItems.Clear();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Duplicate check error: {ex.Message}");
        }
    }
}
