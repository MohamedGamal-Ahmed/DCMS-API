using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using DCMS.WPF.Helpers;
using Microsoft.EntityFrameworkCore;
using DCMS.WPF.Services;
using CommunityToolkit.Mvvm.Messaging;
using DCMS.WPF.Messages;

namespace DCMS.WPF.ViewModels;

public class InboundDetailsViewModel : ViewModelBase
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly NotificationService _notificationService;
    private readonly Services.CurrentUserService _currentUserService;
    private readonly User _currentUser;
    private Inbound _inbound;
    private ObservableCollection<Models.TimelineItem> _timeline;
    private bool _isBusy;
    private string _responsibleEngineersDisplay = string.Empty;
    private string _originalAttachmentUrl = string.Empty;
    private string _transferAttachmentUrl = string.Empty;
    private string _responseAttachmentUrl = string.Empty;

    public InboundDetailsViewModel(IDbContextFactory<DCMSDbContext> contextFactory, IServiceProvider serviceProvider, NotificationService notificationService, Services.CurrentUserService currentUserService, User currentUser, Inbound inbound)
    {
        _contextFactory = contextFactory;
        _serviceProvider = serviceProvider;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _currentUser = currentUser;
        _inbound = inbound;
        _timeline = new ObservableCollection<Models.TimelineItem>();

        EditCommand = new RelayCommand(ExecuteEdit);
        DeleteCommand = new RelayCommand(ExecuteDelete, _ => CanDelete);
        OpenAttachmentCommand = new RelayCommand(ExecuteOpenAttachment, _ => !string.IsNullOrEmpty(Inbound.AttachmentUrl));
        CloseCommand = new RelayCommand(ExecuteClose);
        SaveOriginalAttachmentCommand = new RelayCommand(ExecuteSaveOriginalAttachment);
        SaveTransferAttachmentCommand = new RelayCommand(ExecuteSaveTransferAttachment);
        SaveResponseAttachmentCommand = new RelayCommand(ExecuteSaveResponseAttachment);
        OpenTimelineAttachmentCommand = new RelayCommand(ExecuteOpenTimelineAttachment);
        EditTimelineItemCommand = new RelayCommand(ExecuteEditTimelineItem, _ => IsAdmin);

        // Register for updates
        WeakReferenceMessenger.Default.Register<CorrespondenceUpdatedMessage>(this, (r, m) =>
        {
            if (m.Value == _inbound.Id)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => _ = LoadDetailsAsync());
            }
        });

        _ = LoadDetailsAsync();
    }

    public Inbound Inbound
    {
        get => _inbound;
        set => SetProperty(ref _inbound, value);
    }

    public ObservableCollection<Models.TimelineItem> Timeline
    {
        get => _timeline;
        set => SetProperty(ref _timeline, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string ResponsibleEngineersDisplay
    {
        get => _responsibleEngineersDisplay;
        set => SetProperty(ref _responsibleEngineersDisplay, value);
    }

    public string OriginalAttachmentUrl
    {
        get => _originalAttachmentUrl;
        set => SetProperty(ref _originalAttachmentUrl, value);
    }

    public string TransferAttachmentUrl
    {
        get => _transferAttachmentUrl;
        set => SetProperty(ref _transferAttachmentUrl, value);
    }

    public string ResponseAttachmentUrl
    {
        get => _responseAttachmentUrl;
        set => SetProperty(ref _responseAttachmentUrl, value);
    }

    public bool CanDelete => _currentUser.CanDeleteCorrespondence();
    public bool IsAdmin => _currentUser.Role == UserRole.Admin || _currentUser.Role == UserRole.OfficeManager;

    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand OpenAttachmentCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand SaveOriginalAttachmentCommand { get; }
    public ICommand SaveTransferAttachmentCommand { get; }
    public ICommand SaveResponseAttachmentCommand { get; }
    public ICommand OpenTimelineAttachmentCommand { get; }
    public ICommand EditTimelineItemCommand { get; }

    public event Action? RequestClose;

    private async Task LoadDetailsAsync()
    {
        IsBusy = true;
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            // Load Inbound with related data
            var inboundDetails = await context.Inbounds
                .Include(i => i.Transfers)
                    .ThenInclude(t => t.Engineer)
                .Include(i => i.Transfers)
                    .ThenInclude(t => t.CreatedByUser)
                .Include(i => i.CreatedByUser)
                .Include(i => i.UpdatedByUser)
                .Include(i => i.ResponsibleEngineers)
                    .ThenInclude(re => re.Engineer)
                .FirstOrDefaultAsync(i => i.Id == _inbound.Id);

            if (inboundDetails != null)
            {
                Inbound = inboundDetails;
                
                // Build responsible engineers display string
                if (inboundDetails.ResponsibleEngineers != null && inboundDetails.ResponsibleEngineers.Any())
                {
                    ResponsibleEngineersDisplay = string.Join(", ", 
                        inboundDetails.ResponsibleEngineers.Select(re => re.Engineer.FullName));
                }
                else if (!string.IsNullOrEmpty(inboundDetails.ResponsibleEngineer))
                {
                    ResponsibleEngineersDisplay = inboundDetails.ResponsibleEngineer;
                }
                else
                {
                    ResponsibleEngineersDisplay = "غير محدد";
                }
                
                BuildTimeline();
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error($"حدث خطأ أثناء تحميل التفاصيل: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildTimeline()
    {
        var items = new List<Models.TimelineItem>();

        // Creation Event - with original attachment
        items.Add(new Models.TimelineItem
        {
            Date = Inbound.CreatedAt,
            Title = "إنشاء الموضوع",
            Description = $"تم إنشاء الموضوع بواسطة {Inbound.CreatedByUser?.Username ?? "System"}",
            Type = Models.TimelineItemType.Creation,
            AttachmentUrl = Inbound.OriginalAttachmentUrl ?? Inbound.AttachmentUrl // Fallback to old field
        });

        // Transfers - with transfer attachments
        foreach (var transfer in Inbound.Transfers)
        {
            var transferBy = transfer.CreatedByUser?.Username ?? "System";
            items.Add(new Models.TimelineItem
            {
                Id = 0, // Not used for creation
                Date = transfer.TransferDate,
                Title = "تحويل إلى مهندس",
                Description = $"تم التحويل بواسطة {transferBy} إلى المهندس: {transfer.Engineer.FullName}",
                Type = Models.TimelineItemType.Transfer,
                AttachmentUrl = transfer.TransferAttachmentUrl,
                RelatedEngineerId = transfer.EngineerId
            });

            if (!string.IsNullOrEmpty(transfer.Response))
            {
                items.Add(new Models.TimelineItem
                {
                    Date = transfer.ResponseDate ?? transfer.TransferDate,
                    Title = "رد المهندس",
                    Description = $"{transfer.Engineer.FullName}: {transfer.Response}",
                    Type = Models.TimelineItemType.Response,
                    AttachmentUrl = transfer.ResponseAttachmentUrl,
                    RelatedEngineerId = transfer.EngineerId
                });
            }
        }

        // Current Reply (if exists and not covered by transfers)
        if (!string.IsNullOrEmpty(Inbound.Reply))
        {
             var replyBy = Inbound.UpdatedByUser?.Username ?? "System";
             items.Add(new Models.TimelineItem
            {
                Date = Inbound.UpdatedAt,
                Title = "رد / إجراء",
                Description = $"{Inbound.Reply} (بواسطة: {replyBy})",
                Type = Models.TimelineItemType.Response,
                AttachmentUrl = Inbound.ReplyAttachmentUrl
            });
        }

        // Sort by date ascending (oldest first)
        Timeline = new ObservableCollection<Models.TimelineItem>(items.OrderBy(x => x.Date));
    }

    private void ExecuteEdit(object? parameter)
    {
        var editVm = new EditFollowUpViewModel(_contextFactory, _notificationService, _currentUserService, Inbound);
        var editDialog = new Views.EditFollowUpDialog(editVm);
        
        editVm.RequestClose += () => 
        {
            editDialog.Close();
            _ = LoadDetailsAsync(); // Refresh details
        };

        editDialog.ShowDialog();
    }

    private async void ExecuteDelete(object? parameter)
    {
        if (MessageBox.Show("هل أنت متأكد من حذف هذا الموضوع نهائياً؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var inboundToDelete = await context.Inbounds.FindAsync(Inbound.Id);
                if (inboundToDelete != null)
                {
                    context.Inbounds.Remove(inboundToDelete);
                    await context.SaveChangesAsync();
                    RequestClose?.Invoke(); // Close details view
                }
            }
            catch (Exception ex)
            {
                _notificationService.Error($"حدث خطأ أثناء الحذف: {ex.Message}");
            }
        }
    }

    private void ExecuteOpenAttachment(object? parameter)
    {
        if (!string.IsNullOrEmpty(Inbound.AttachmentUrl))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Inbound.AttachmentUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _notificationService.Error($"لا يمكن فتح الملف: {ex.Message}");
            }
        }
    }

    private void ExecuteClose(object? parameter)
    {
        RequestClose?.Invoke();
    }

    private async void ExecuteSaveOriginalAttachment(object? parameter)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var inbound = await context.Inbounds.FindAsync(Inbound.Id);
            if (inbound != null)
            {
                inbound.OriginalAttachmentUrl = OriginalAttachmentUrl;
                await context.SaveChangesAsync();
                _notificationService.Success("تم حفظ مرفق الوارد الأصلي بنجاح!");
                _ = LoadDetailsAsync(); // Refresh
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error($"حدث خطأ أثناء الحفظ: {ex.Message}");
        }
    }

    private async void ExecuteSaveTransferAttachment(object? parameter)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            // Get the most recent transfer for this inbound
            var latestTransfer = await context.InboundTransfers
                .Where(t => t.InboundId == Inbound.Id)
                .OrderByDescending(t => t.TransferDate)
                .FirstOrDefaultAsync();

            if (latestTransfer != null)
            {
                latestTransfer.TransferAttachmentUrl = TransferAttachmentUrl;
                await context.SaveChangesAsync();
                _notificationService.Success("تم حفظ مرفق التأشيرة/التحويل بنجاح!");
                _ = LoadDetailsAsync(); // Refresh
            }
            else
            {
                _notificationService.Warning("لا يوجد تحويلات لحفظ المرفق!");
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error($"حدث خطأ أثناء الحفظ: {ex.Message}");
        }
    }

    private async void ExecuteSaveResponseAttachment(object? parameter)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var inbound = await context.Inbounds.FindAsync(Inbound.Id);
            bool saved = false;

            // 1. Try Saving to Main Inbound Reply (Priority if closed/replied)
            if (inbound != null && !string.IsNullOrEmpty(inbound.Reply))
            {
                inbound.ReplyAttachmentUrl = ResponseAttachmentUrl;
                saved = true;
            }

            // 2. Also Try Saving to Latest Transfer Response (if exists)
            // We do this so the attachment is linked to where the user expects it
            var latestResponseTransfer = await context.InboundTransfers
                .Where(t => t.InboundId == Inbound.Id && !string.IsNullOrEmpty(t.Response))
                .OrderByDescending(t => t.ResponseDate ?? t.TransferDate)
                .FirstOrDefaultAsync();

            if (latestResponseTransfer != null)
            {
                latestResponseTransfer.ResponseAttachmentUrl = ResponseAttachmentUrl;
                saved = true;
            }

            if (saved)
            {
                await context.SaveChangesAsync();
                _notificationService.Success("تم حفظ مرفق الرد بنجاح!");
                _ = LoadDetailsAsync(); // Refresh
            }
            else
            {
                _notificationService.Warning("لا يوجد ردود (نهائية أو تحويلات) لحفظ المرفق!");
            }
        }
        catch (Exception ex)
        {
            _notificationService.Error($"حدث خطأ أثناء الحفظ: {ex.Message}");
        }
    }

    private void ExecuteOpenTimelineAttachment(object? parameter)
    {
        if (parameter is Models.TimelineItem timelineItem && !string.IsNullOrEmpty(timelineItem.AttachmentUrl))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = timelineItem.AttachmentUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _notificationService.Error($"لا يمكن فتح الملف: {ex.Message}");
            }
        }
    }

    private void ExecuteEditTimelineItem(object? parameter)
    {
        if (parameter is Models.TimelineItem item && (item.Type == Models.TimelineItemType.Transfer || item.Type == Models.TimelineItemType.Response))
        {
            if (item.RelatedEngineerId.HasValue)
            {
                // Find the transfer record
                var transfer = Inbound.Transfers.FirstOrDefault(t => t.EngineerId == item.RelatedEngineerId.Value);
                if (transfer != null)
                {
                    var editTransferVm = new EditTransferViewModel(_contextFactory, _notificationService, _currentUserService, transfer);
                    var editTransferDialog = new Views.EditTransferDialog(editTransferVm);
                    
                    editTransferVm.RequestClose += () => 
                    {
                        editTransferDialog.Close();
                        System.Windows.Application.Current.Dispatcher.Invoke(() => _ = LoadDetailsAsync());
                    };

                    editTransferDialog.ShowDialog();
                }
            }
        }
    }
}

