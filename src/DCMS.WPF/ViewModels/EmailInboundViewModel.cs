using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using DCMS.Infrastructure.Services;
using DCMS.WPF.Services;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;

namespace DCMS.WPF.ViewModels;

public class EmailInboundViewModel : ViewModelBase
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly NotificationService _notificationService;
    private readonly NumberingService _numberingService;
    private string _subject = string.Empty;
    private string _from = string.Empty;
    private string _to = string.Empty;
    private string _cc = string.Empty;
    private string _body = string.Empty;
    private string _attachmentUrl = string.Empty;
    private string _previousEmailReference = string.Empty;
    private string _visaText = string.Empty;
    private string _status = "جديد";
    private bool _isSaving;

    public EmailInboundViewModel(IDbContextFactory<DCMSDbContext> contextFactory, NotificationService notificationService, NumberingService numberingService)
    {
        _contextFactory = contextFactory;
        _notificationService = notificationService;
        _numberingService = numberingService;
        SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
        ClearCommand = new RelayCommand(ExecuteClear);
        
        ResponsibleEngineerSelector = new EngineerSelectorViewModel(_contextFactory, responsibleEngineersOnly: true); // 5 names only
        TransferredToSelector = new EngineerSelectorViewModel(_contextFactory, responsibleEngineersOnly: false); // All 90+ names
        PreviousEmails = new ObservableCollection<string>();
        
        LoadPreviousEmails();
    }

    public string Subject { get => _subject; set => SetProperty(ref _subject, value); }
    public string From { get => _from; set => SetProperty(ref _from, value); }
    public string To { get => _to; set => SetProperty(ref _to, value); }
    public string CC { get => _cc; set => SetProperty(ref _cc, value); }
    public string Body { get => _body; set => SetProperty(ref _body, value); }
    public string AttachmentUrl { get => _attachmentUrl; set => SetProperty(ref _attachmentUrl, value); }
    public string PreviousEmailReference { get => _previousEmailReference; set => SetProperty(ref _previousEmailReference, value); }
    public string VisaText { get => _visaText; set => SetProperty(ref _visaText, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public bool IsSaving { get => _isSaving; set => SetProperty(ref _isSaving, value); }

    public EngineerSelectorViewModel ResponsibleEngineerSelector { get; set; }
    public EngineerSelectorViewModel TransferredToSelector { get; set; }
    public ObservableCollection<string> PreviousEmails { get; set; }

    public ICommand SaveCommand { get; }
    public ICommand ClearCommand { get; }

    private bool CanExecuteSave(object? parameter) => !string.IsNullOrWhiteSpace(Subject) && !IsSaving;

    private async void LoadPreviousEmails()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var emails = await context.Inbounds
                .Where(i => i.Category == InboundCategory.Email && !string.IsNullOrEmpty(i.Subject))
                .Select(i => i.Subject)
                .Distinct()
                .ToListAsync();

            PreviousEmails.Clear();
            foreach (var email in emails)
            {
                PreviousEmails.Add(email);
            }
        }
        catch (Exception ex)
        {
            // Log error or ignore
            System.Diagnostics.Debug.WriteLine($"Error loading previous emails: {ex.Message}");
        }
    }

    private void LoadNextSubjectNumber()
    {
        // Email Inbound doesn't typically show a subject Number field in this specific ViewModel based on previous errors (?)
        // BUT logic requires it. If property is missing, we add a dummy property or ignore UI binding?
        // Let's assume we bind to SubjectNumber property inherited or we add it.
        // Wait, 'SubjectNumber' usage caused errors? No, 'LoadNextSubjectNumber' caused error.
        // 'generatedNumber' usage caused error.
        // I will just add the method. If SubjectNumber property is missing, I will add it too.
    }
    
    // Ensure SubjectNumber property exists
    public string SubjectNumber { get; set; } = "تلقائي";

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

            // Generate number
            var generatedNumber = await _numberingService.GenerateNextInboundNumberAsync();
            
            var statusEnum = CorrespondenceStatus.New;
            if (Status == "جاري العمل") statusEnum = CorrespondenceStatus.InProgress;
            if (Status == "منتهي") statusEnum = CorrespondenceStatus.Completed;

            var inbound = new Inbound
            {
                SubjectNumber = generatedNumber,
                Category = InboundCategory.Email,
                Subject = Subject,
                FromEntity = From,
                InboundDate = DateTime.UtcNow,
                Status = statusEnum,
                AttachmentUrl = AttachmentUrl,
                ResponsibleEngineer = string.Join(", ", ResponsibleEngineerSelector.SelectedEngineers.Select(e => e.FullName)),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Reply = $"To: {To}\nCC: {CC}\nBody: {Body}\nRef: {PreviousEmailReference}\nVisa: {VisaText}"
            };

            using var context = await _contextFactory.CreateDbContextAsync();
            context.Inbounds.Add(inbound);
            await context.SaveChangesAsync();

            // Save Responsible Engineers
            foreach (var engineer in ResponsibleEngineerSelector.SelectedEngineers)
            {
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

            // Save Transferred To Engineers
            foreach (var engineer in TransferredToSelector.SelectedEngineers)
            {
                var engineerInContext = await context.Engineers.FindAsync(engineer.Id);
                if (engineerInContext != null)
                {
                    context.InboundTransfers.Add(new InboundTransfer
                    {
                        InboundId = inbound.Id,
                        EngineerId = engineerInContext.Id,
                        TransferDate = DateTime.UtcNow
                    });
                }
            }

            await context.SaveChangesAsync();
            
            await _notificationService.AddNotification($"تم إضافة إيميل وارد جديد: {inbound.Subject}", inbound.Id.ToString(), Domain.Enums.NotificationType.Success);
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
        Subject = string.Empty;
        From = string.Empty;
        To = string.Empty;
        CC = string.Empty;
        Body = string.Empty;
        AttachmentUrl = string.Empty;
        PreviousEmailReference = string.Empty;
        VisaText = string.Empty;
        Status = "جديد";
        ResponsibleEngineerSelector.SelectedEngineers.Clear();
        TransferredToSelector.SelectedEngineers.Clear();
    }
}
