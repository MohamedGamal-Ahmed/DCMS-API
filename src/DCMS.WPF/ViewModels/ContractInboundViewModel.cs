using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using DCMS.Infrastructure.Services;
using DCMS.WPF.Services;
using Microsoft.EntityFrameworkCore;

namespace DCMS.WPF.ViewModels;

public class ContractInboundViewModel : ViewModelBase
{
    public string Title => "إضافة عقد وارد";
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly NotificationService _notificationService;
    private readonly NumberingService _numberingService;
    private string _projectName = string.Empty;
    private DateTime _signingDate = DateTime.Now;
    private string _party1Name = string.Empty;
    private string _party1Role = string.Empty;
    private string _party2Name = string.Empty;
    private string _party2Role = string.Empty;
    public EngineerSelectorViewModel ResponsibleEngineerSelector { get; }
    public EngineerSelectorViewModel TransferredToSelector { get; }
    private string _status = "ساري"; // Default status
    private string _notes = string.Empty;
    private string _attachmentUrl = string.Empty;
    private System.Windows.Controls.ComboBoxItem? _selectedContractType;
    private bool _isSaving;
    private string _subjectNumber = string.Empty; // Added backing field
    private string _code = string.Empty; // Added backing field

    public ContractInboundViewModel(IDbContextFactory<DCMSDbContext> contextFactory, NotificationService notificationService, NumberingService numberingService)
    {
        _contextFactory = contextFactory;
        _notificationService = notificationService;
        _numberingService = numberingService;
        SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
        ClearCommand = new RelayCommand(ExecuteClear);
        CancelCommand = new RelayCommand(_ => OnRequestClose());
        
        ResponsibleEngineerSelector = new EngineerSelectorViewModel(_contextFactory, responsibleEngineersOnly: true);
        TransferredToSelector = new EngineerSelectorViewModel(_contextFactory, responsibleEngineersOnly: false);

        LoadNextSubjectNumber(); // Initialize
    }

    public string ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value);
    }

    public DateTime SigningDate
    {
        get => _signingDate;
        set => SetProperty(ref _signingDate, value);
    }

    public string Party1Name { get => _party1Name; set => SetProperty(ref _party1Name, value); }
    public string Party1Role { get => _party1Role; set => SetProperty(ref _party1Role, value); }
    public string Party2Name { get => _party2Name; set => SetProperty(ref _party2Name, value); }
    public string Party2Role { get => _party2Role; set => SetProperty(ref _party2Role, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public string Notes { get => _notes; set => SetProperty(ref _notes, value); }
    public string AttachmentUrl { get => _attachmentUrl; set => SetProperty(ref _attachmentUrl, value); }
    public System.Windows.Controls.ComboBoxItem? SelectedContractType 
    { 
        get => _selectedContractType; 
        set => SetProperty(ref _selectedContractType, value); 
    }
    public bool IsSaving { get => _isSaving; set => SetProperty(ref _isSaving, value); }
    public string SubjectNumber { get => _subjectNumber; set => SetProperty(ref _subjectNumber, value); }
    public string Code { get => _code; set => SetProperty(ref _code, value); }

    public ICommand SaveCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand CancelCommand { get; }

    private bool CanExecuteSave(object? parameter) => !string.IsNullOrWhiteSpace(ProjectName) && !IsSaving;

    private void LoadNextSubjectNumber()
    {
        SubjectNumber = "تلقائي";
    }

    private async void ExecuteSave(object? parameter)
    {
        IsSaving = true;
        try
        {
            var statusEnum = CorrespondenceStatus.New; 

            // Parse contract type
            ContractType? contractType = null;
            if (SelectedContractType?.Tag is string tagValue)
            {
                Enum.TryParse<ContractType>(tagValue, out var parsedType);
                contractType = parsedType;
            }
            if (!ResponsibleEngineerSelector.SelectedEngineers.Any())
            {
                _notificationService.Warning("يرجى اختيار مهندس مسئول واحد على الأقل.");
                IsSaving = false;
                return;
            }

            // Generate auto-number on save
            var generatedNumber = await _numberingService.GenerateNextInboundNumberAsync();
            SubjectNumber = generatedNumber;

            var inbound = new Inbound
            {
                SubjectNumber = generatedNumber,
                Code = Code,
                Subject = ProjectName, 
                InboundDate = SigningDate.ToUniversalTime(),
                Category = InboundCategory.Contract,
                Status = statusEnum,
                // Notes not in Inbound
                AttachmentUrl = AttachmentUrl,
                ContractType = contractType,
                // FirstParty/SecondParty not in Inbound
                // CreatedByUserId = _currentUser?.Id, // Missing _currentUser
                ResponsibleEngineer = string.Join(", ", ResponsibleEngineerSelector.SelectedEngineers.Select(e => e.FullName)),
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow 
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
            
            // Save TransferredTo
             foreach (var engineer in TransferredToSelector.SelectedEngineers)
            {
                 context.InboundTransfers.Add(new InboundTransfer
                 {
                     InboundId = inbound.Id,
                     EngineerId = engineer.Id,
                     TransferDate = DateTime.UtcNow
                     // Status property might not exist, removed
                 });
            }

            await context.SaveChangesAsync();
        
            // Add notification
            await _notificationService.AddNotification($"تم إضافة عقد جديد: {inbound.Subject}", inbound.Id.ToString(), Domain.Enums.NotificationType.Success);
            _notificationService.Success($"✅ تم الحفظ بنجاح! رقم العقد: {generatedNumber}");
            
            ExecuteClear(null);
            LoadNextSubjectNumber();
            OnRequestClose();
        }
        catch (Exception ex)
        {
            _notificationService.Error($"حدث خطأ أثناء الحفظ: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void ExecuteClear(object? parameter)
    {
        ProjectName = string.Empty;
        SigningDate = DateTime.Now;
        Party1Name = string.Empty;
        Party1Role = string.Empty;
        Party2Name = string.Empty;
        Party2Role = string.Empty;
        ResponsibleEngineerSelector.SelectedEngineers.Clear();
        TransferredToSelector.SelectedEngineers.Clear();
        Status = "ساري";
        Notes = string.Empty;
        AttachmentUrl = string.Empty;
        SelectedContractType = null;
    }
}
