using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using DCMS.Infrastructure.Services;
using DCMS.WPF.Helpers;
using DCMS.WPF.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace DCMS.WPF.ViewModels;

public class RequestInboundViewModel : ViewModelBase
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly NotificationService _notificationService;
    private readonly NumberingService _numberingService;
    private readonly CodeLookupService _codeLookupService;
    private readonly CurrentUserService _currentUserService;
    private string _subjectNumber = string.Empty;
    private string _code = string.Empty;
    private string _subject = string.Empty;
    private string _fromEntity = string.Empty;
    private string _fromEngineer = string.Empty;
    private string _responsibleEngineer = string.Empty;
    private DateTime _inboundDate = DateTime.Now;
    private string _attachmentUrl = string.Empty;
    private string _reply = string.Empty;
    private bool _isSaving;

    private readonly IServiceProvider _serviceProvider;

    public RequestInboundViewModel(IDbContextFactory<DCMSDbContext> contextFactory, NotificationService notificationService, NumberingService numberingService, CodeLookupService codeLookupService, CurrentUserService currentUserService, IServiceProvider serviceProvider)
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
        
        // Initialize collections
        ResponsibleEngineers = new ObservableCollection<string>(EngineerLists.ResponsibleEngineers);
        
        // Generate next subject number on load
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
                    // Auto-fill logic
                    Code = value.Code;
                    FromEntity = value.Entity;
                    FromEngineer = value.Engineer;
                }
            }
        }
    }

    public ObservableCollection<string> ResponsibleEngineers { get; set; }

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

    public string Subject
    {
        get => _subject;
        set => SetProperty(ref _subject, value);
    }

    public string FromEntity
    {
        get => _fromEntity;
        set => SetProperty(ref _fromEntity, value);
    }

    public string FromEngineer
    {
        get => _fromEngineer;
        set => SetProperty(ref _fromEngineer, value);
    }

    public string ResponsibleEngineer
    {
        get => _responsibleEngineer;
        set => SetProperty(ref _responsibleEngineer, value);
    }

    public DateTime InboundDate
    {
        get => _inboundDate;
        set => SetProperty(ref _inboundDate, value);
    }

    public string AttachmentUrl
    {
        get => _attachmentUrl;
        set => SetProperty(ref _attachmentUrl, value);
    }

    public string Reply
    {
        get => _reply;
        set => SetProperty(ref _reply, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        set => SetProperty(ref _isSaving, value);
    }

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

    private bool CanExecuteSave(object? parameter)
    {
        return !string.IsNullOrWhiteSpace(Subject) && 
               !string.IsNullOrWhiteSpace(SubjectNumber) && 
               !IsSaving;
    }

    private async void ExecuteSave(object? parameter)
    {
        IsSaving = true;

        try
        {
            if (string.IsNullOrWhiteSpace(ResponsibleEngineer))
            {
                _notificationService.Warning("يرجى اختيار المهندس المسئول.");
                IsSaving = false;
                return;
            }

            // Generate number on save
            var generatedNumber = await _numberingService.GenerateNextInboundNumberAsync();
            SubjectNumber = generatedNumber;

            var inbound = new Inbound
            {
                SubjectNumber = generatedNumber,
                Code = Code,
                Subject = Subject,
                FromEntity = FromEntity,
                FromEngineer = FromEngineer,
                ResponsibleEngineer = ResponsibleEngineer,
                InboundDate = InboundDate.ToUniversalTime(),
                Category = InboundCategory.Request,
                Status = CorrespondenceStatus.InProgress,
                AttachmentUrl = AttachmentUrl,
                Reply = Reply,
                // Notes removed
                // CreatedByUserId removed
            };

            using var context = await _contextFactory.CreateDbContextAsync();
            context.Inbounds.Add(inbound);
            await context.SaveChangesAsync();

            // Add notification
            await _notificationService.AddNotification($"تم إضافة طلب/شكوى جديدة: {inbound.Subject}", inbound.Id.ToString(), Domain.Enums.NotificationType.Success);
            _notificationService.Success($"✅ تم الحفظ بنجاح! رقم الوارد: {generatedNumber}");


            ExecuteClear(null);
            LoadNextSubjectNumber();
        }
        catch (Exception ex)
        {
            var errorMessage = $"حدث خطأ أثناء الحفظ:\n{ex.Message}";
            if (ex.InnerException != null) errorMessage += $"\n\nتفاصيل الخطأ:\n{ex.InnerException.Message}";
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
        Subject = string.Empty;
        FromEntity = string.Empty;
        FromEngineer = string.Empty;
        ResponsibleEngineer = string.Empty;
        InboundDate = DateTime.Now;
        AttachmentUrl = string.Empty;
        Reply = string.Empty;
    }
}
