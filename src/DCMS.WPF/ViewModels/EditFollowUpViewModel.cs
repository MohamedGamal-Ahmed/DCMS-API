using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using DCMS.WPF.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using DCMS.WPF.Services;
using CommunityToolkit.Mvvm.Messaging;
using DCMS.WPF.Messages;

namespace DCMS.WPF.ViewModels;

public class EditFollowUpViewModel : ViewModelBase
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly NotificationService _notificationService;
    private readonly Services.CurrentUserService _currentUserService;
    private Inbound _inbound;
    private Engineer? _selectedEngineer;
    private DateTime _transferDate = DateTime.Today;
    private string _reply = string.Empty;
    private CorrespondenceStatus _status;
    private ObservableCollection<Engineer> _engineers;
    private ObservableCollection<Engineer> _selectedEngineers = new();
    private ObservableCollection<Engineer> _allActiveEngineers = new();
    private bool _isBusy;
    private string _adminSubjectNumber = string.Empty;
    private string _adminCode = string.Empty;
    private string _adminSubject = string.Empty;
    private string _adminResponsibleEngineer = string.Empty;
    private string _adminFromEntity = string.Empty;
    private string _adminFromEngineer = string.Empty;
    private DateTime _adminInboundDate = DateTime.Today;

    public EditFollowUpViewModel(IDbContextFactory<DCMSDbContext> contextFactory, NotificationService notificationService, Services.CurrentUserService currentUserService, Inbound inbound)
    {
        _contextFactory = contextFactory;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _inbound = inbound;
        _status = inbound.Status;
        _reply = inbound.Reply ?? string.Empty;
        
        // Admin properties
        _adminSubjectNumber = inbound.SubjectNumber;
        _adminCode = inbound.Code ?? string.Empty;
        _adminSubject = inbound.Subject;
        _adminResponsibleEngineer = inbound.ResponsibleEngineer ?? string.Empty;
        _adminFromEntity = inbound.FromEntity ?? string.Empty;
        _adminFromEngineer = inbound.FromEngineer ?? string.Empty;
        _adminInboundDate = inbound.InboundDate.ToLocalTime();
        
        Engineers = new ObservableCollection<Engineer>();
        SelectedEngineers = new ObservableCollection<Engineer>();
        
        SaveCommand = new RelayCommand(ExecuteSave);
        CancelCommand = new RelayCommand(ExecuteCancel);
        AddEngineerCommand = new RelayCommand(ExecuteAddEngineer); // Always enabled to support new engineer names
        RemoveEngineerCommand = new RelayCommand(ExecuteRemoveEngineer);
        DeleteCommand = new RelayCommand(ExecuteDelete);

        _ = LoadEngineersAsync();
    }

    private ObservableCollection<Engineer> _filteredEngineers;
    private List<Engineer> _allEngineers = new(); // For local filtering
    private string _searchText;

    public ObservableCollection<Engineer> Engineers
    {
        get => _engineers;
        set => SetProperty(ref _engineers, value);
    }

    public ObservableCollection<Engineer> FilteredEngineers
    {
        get => _filteredEngineers;
        set => SetProperty(ref _filteredEngineers, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FilterEngineers();
            }
        }
    }

    public ObservableCollection<Engineer> SelectedEngineers
    {
        get => _selectedEngineers;
        set => SetProperty(ref _selectedEngineers, value);
    }

    public ObservableCollection<Engineer> AllActiveEngineers
    {
        get => _allActiveEngineers;
        set => SetProperty(ref _allActiveEngineers, value);
    }

     private void FilterEngineers()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredEngineers = new ObservableCollection<Engineer>(_allEngineers);
            return;
        }

        var normalizedSearch = NormalizeName(SearchText);
        var filtered = _allEngineers.Where(e => NormalizeName(e.FullName).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)).ToList();
        FilteredEngineers = new ObservableCollection<Engineer>(filtered);
    }

    private string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        return name.Replace("أ", "ا")
                   .Replace("إ", "ا")
                   .Replace("آ", "ا")
                   .Replace("ة", "ه")
                   .Replace("ى", "ي")
                   .Replace("  ", " ") // remove double spaces
                   .Trim();
    }

    public Engineer? SelectedEngineer
    {
        get => _selectedEngineer;
        set
        {
            if (SetProperty(ref _selectedEngineer, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public DateTime TransferDate
    {
        get => _transferDate;
        set => SetProperty(ref _transferDate, value);
    }

    public string Reply
    {
        get => _reply;
        set => SetProperty(ref _reply, value);
    }

    public CorrespondenceStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string AdminSubjectNumber
    {
        get => _adminSubjectNumber;
        set => SetProperty(ref _adminSubjectNumber, value);
    }

    public string AdminCode
    {
        get => _adminCode;
        set => SetProperty(ref _adminCode, value);
    }

    public string AdminSubject
    {
        get => _adminSubject;
        set => SetProperty(ref _adminSubject, value);
    }

    public string AdminResponsibleEngineer
    {
        get => _adminResponsibleEngineer;
        set => SetProperty(ref _adminResponsibleEngineer, value);
    }

    public string AdminFromEntity
    {
        get => _adminFromEntity;
        set => SetProperty(ref _adminFromEntity, value);
    }

    public string AdminFromEngineer
    {
        get => _adminFromEngineer;
        set => SetProperty(ref _adminFromEngineer, value);
    }

    public DateTime AdminInboundDate
    {
        get => _adminInboundDate;
        set => SetProperty(ref _adminInboundDate, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddEngineerCommand { get; }
    public ICommand RemoveEngineerCommand { get; }
    public ICommand DeleteCommand { get; }

    public bool IsDeleteVisible => _currentUserService.CurrentUser?.Role == Domain.Enums.UserRole.Admin || _currentUserService.CurrentUser?.Role == Domain.Enums.UserRole.OfficeManager;

    public event Action? RequestClose;

    private async void ExecuteAddEngineer(object? parameter)
    {
        // If a known engineer is selected, add them
        if (SelectedEngineer != null)
        {
            if (!SelectedEngineers.Contains(SelectedEngineer))
            {
                SelectedEngineers.Add(SelectedEngineer);
            }
            return;
        }

        // If no engineer is selected but text is entered (New Engineer)
        if (parameter is string newEngineerName && !string.IsNullOrWhiteSpace(newEngineerName))
        {
            // Check if already in SelectedEngineers
            if (SelectedEngineers.Any(e => e.FullName.Equals(newEngineerName, StringComparison.OrdinalIgnoreCase)))
                return;

            // Check if exists in DB but not selected in ComboBox
            var existingEngineer = Engineers.FirstOrDefault(e => e.FullName.Equals(newEngineerName, StringComparison.OrdinalIgnoreCase));
            if (existingEngineer != null)
            {
                if (!SelectedEngineers.Contains(existingEngineer))
                    SelectedEngineers.Add(existingEngineer);
                return;
            }

            // Create new engineer
            var newEngineer = new Engineer { FullName = newEngineerName, IsActive = true };
            
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                context.Engineers.Add(newEngineer);
                await context.SaveChangesAsync();

                // Add to main list and selected list
                Engineers.Add(newEngineer);
                SelectedEngineers.Add(newEngineer);
                SelectedEngineer = newEngineer; // Select it
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إضافة المهندس الجديد: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExecuteRemoveEngineer(object? parameter)
    {
        if (parameter is Engineer engineer)
        {
            SelectedEngineers.Remove(engineer);
        }
    }

    private async void ExecuteDelete(object? parameter)
    {
        if (MessageBox.Show("هل أنت متأكد من حذف هذا الموضوع نهائياً؟ لا يمكن التراجع عن هذا الإجراء.", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var inboundToDelete = await context.Inbounds.FindAsync(_inbound.Id);
                if (inboundToDelete != null)
                {
                    context.Inbounds.Remove(inboundToDelete);
                    await context.SaveChangesAsync();
                    RequestClose?.Invoke();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء الحذف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task LoadEngineersAsync()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var engineers = await context.Engineers.Where(e => e.IsActive).ToListAsync();
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Engineers = new ObservableCollection<Engineer>(engineers);
                AllActiveEngineers = new ObservableCollection<Engineer>(engineers);
                _allEngineers = engineers;
                FilterEngineers();
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل المهندسين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExecuteSave(object? parameter)
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var inbound = await context.Inbounds.FindAsync(_inbound.Id);
            
            if (inbound != null)
            {
                // Update Status
                inbound.Status = Status;
                
                // Update Reply
                inbound.Reply = Reply;

                // Update Last Modified By
                inbound.UpdatedByUserId = _currentUserService.CurrentUser?.Id;

                // Admin specific changes
                if (IsDeleteVisible) // Using this as an IsAdmin check
                {
                    inbound.SubjectNumber = AdminSubjectNumber;
                    inbound.Code = AdminCode;
                    inbound.Subject = AdminSubject;
                    inbound.FromEntity = AdminFromEntity;
                    inbound.FromEngineer = AdminFromEngineer;
                    inbound.InboundDate = AdminInboundDate.ToUniversalTime();
                    
                    if (inbound.ResponsibleEngineer != AdminResponsibleEngineer)
                    {
                        inbound.ResponsibleEngineer = AdminResponsibleEngineer;
                        
                        // Sync with junction table InboundResponsibleEngineers
                        if (!string.IsNullOrWhiteSpace(AdminResponsibleEngineer))
                        {
                            var engineer = await context.Engineers.FirstOrDefaultAsync(e => e.FullName == AdminResponsibleEngineer);
                            if (engineer != null)
                            {
                                // Clear existing ones and add this one
                                var existing = await context.InboundResponsibleEngineers
                                    .Where(ire => ire.InboundId == inbound.Id)
                                    .ToListAsync();
                                context.InboundResponsibleEngineers.RemoveRange(existing);
                                
                                context.InboundResponsibleEngineers.Add(new InboundResponsibleEngineer
                                {
                                    InboundId = inbound.Id,
                                    EngineerId = engineer.Id
                                });
                            }
                        }
                    }
                }

                // Handle Transfers (Multiple Delegation)
                if (SelectedEngineers.Any())
                {
                    foreach (var engineer in SelectedEngineers)
                    {
                        // Check if already transferred to this engineer
                        var exists = await context.InboundTransfers
                            .AnyAsync(t => t.InboundId == inbound.Id && t.EngineerId == engineer.Id);
                            
                        if (!exists)
                        {
                            var transfer = new InboundTransfer
                            {
                                InboundId = inbound.Id,
                                EngineerId = engineer.Id,
                                TransferDate = DateTime.SpecifyKind(TransferDate, DateTimeKind.Utc),
                                CreatedByUserId = _currentUserService.CurrentUser?.Id
                            };
                            context.InboundTransfers.Add(transfer);
                        }
                    }
                }

                inbound.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                // Notify other views that this inbound has been updated
                WeakReferenceMessenger.Default.Send(new CorrespondenceUpdatedMessage(inbound.Id));

                await _notificationService.AddNotification($"تم تحديث الموضوع: {inbound.Subject}", inbound.Id.ToString(), NotificationType.Success);
                
                // Notify Dashboard to refresh (real-time update)
                WeakReferenceMessenger.Default.Send(new DashboardRefreshMessage(inbound.Id));
                
                MessageBox.Show("تم حفظ التعديلات بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                RequestClose?.Invoke();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في الحفظ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ExecuteCancel(object? parameter)
    {
        RequestClose?.Invoke();
    }
}
