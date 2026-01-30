using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using DCMS.Domain.Entities;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using DCMS.WPF.Services;
using System.Collections.ObjectModel;

namespace DCMS.WPF.ViewModels;

public class OutboundDetailsViewModel : ViewModelBase
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly Services.CurrentUserService _currentUserService;
    private Outbound _outbound;
    private bool _isBusy;
    private string _inboundSubject = string.Empty;
    private int? _inboundId;
    private string _originalAttachmentUrl = string.Empty;
    private string _replyAttachmentUrl = string.Empty;
    private ObservableCollection<Models.TimelineItem> _timeline = new();

    public OutboundDetailsViewModel(IDbContextFactory<DCMSDbContext> contextFactory, IServiceProvider serviceProvider, Services.CurrentUserService currentUserService, Outbound outbound)
    {
        _contextFactory = contextFactory;
        _serviceProvider = serviceProvider;
        _currentUserService = currentUserService;
        _outbound = outbound;
        
        CloseCommand = new RelayCommand(ExecuteClose);
        OpenAttachmentCommand = new RelayCommand(ExecuteOpenAttachment);
        GoToInboundCommand = new RelayCommand(ExecuteGoToInbound, _ => _inboundId.HasValue);
        SaveOriginalAttachmentCommand = new RelayCommand(ExecuteSaveOriginalAttachment);
        SaveReplyAttachmentCommand = new RelayCommand(ExecuteSaveReplyAttachment);
        OpenTimelineAttachmentCommand = new RelayCommand(ExecuteOpenTimelineAttachment);
        
        DeleteCommand = new RelayCommand(ExecuteDelete, _ => IsAdmin);
        
        _ = LoadDetailsAsync();
    }

    public bool IsAdmin => _currentUserService.CurrentUser?.Role == Domain.Enums.UserRole.Admin || _currentUserService.CurrentUser?.Role == Domain.Enums.UserRole.OfficeManager;

    public ICommand DeleteCommand { get; }

    public Outbound Outbound
    {
        get => _outbound;
        set => SetProperty(ref _outbound, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string InboundSubject
    {
        get => _inboundSubject;
        set 
        {
            if (SetProperty(ref _inboundSubject, value))
            {
                ((RelayCommand)GoToInboundCommand).RaiseCanExecuteChanged();
            }
        }
    }
    
    public bool HasInboundLink => _inboundId.HasValue;

    public ICommand CloseCommand { get; }
    public ICommand OpenAttachmentCommand { get; }
    public ICommand GoToInboundCommand { get; }
    
    public string OriginalAttachmentUrl
    {
        get => _originalAttachmentUrl;
        set => SetProperty(ref _originalAttachmentUrl, value);
    }

    public string ReplyAttachmentUrl
    {
        get => _replyAttachmentUrl;
        set => SetProperty(ref _replyAttachmentUrl, value);
    }

    public ObservableCollection<Models.TimelineItem> Timeline
    {
        get => _timeline;
        set => SetProperty(ref _timeline, value);
    }
    
    public ICommand SaveOriginalAttachmentCommand { get; }
    public ICommand SaveReplyAttachmentCommand { get; }
    public ICommand OpenTimelineAttachmentCommand { get; }

    public event Action? RequestClose;
    public event Action<int>? RequestNavigationToInbound;
    
    private async Task LoadDetailsAsync()
    {
        IsBusy = true;
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            // Reload outbound with creator
            var detailed = await context.Outbounds
                .Include(o => o.CreatedByUser)
                .FirstOrDefaultAsync(o => o.Id == _outbound.Id);
                
            if (detailed != null)
            {
                Outbound = detailed;
                OriginalAttachmentUrl = detailed.OriginalAttachmentUrl ?? string.Empty;
                ReplyAttachmentUrl = detailed.ReplyAttachmentUrl ?? string.Empty;
                
                BuildTimeline();
            }

            // Find linked inbound
            if (!string.IsNullOrEmpty(Outbound.RelatedInboundNo))
            {
                var inbound = await context.Inbounds
                    .FirstOrDefaultAsync(i => i.SubjectNumber == Outbound.RelatedInboundNo);
                    
                if (inbound != null)
                {
                    _inboundId = inbound.Id;
                    InboundSubject = $"رقم الموضوع: {inbound.SubjectNumber} - {inbound.Subject}";
                    OnPropertyChanged(nameof(HasInboundLink));
                }
                else
                {
                    InboundSubject = "لم يتم العثور على موضوع وارد بهذا الرقم";
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل التفاصيل: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildTimeline()
    {
        var items = new List<Models.TimelineItem>();

        // Creation Event
        items.Add(new Models.TimelineItem
        {
            Date = Outbound.CreatedAt,
            Title = "إنشاء الموضوع",
            Description = $"تم إنشاء الموضوع بواسطة {Outbound.CreatedByUser?.Username ?? "System"}",
            Type = Models.TimelineItemType.Creation,
            AttachmentUrl = Outbound.OriginalAttachmentUrl
        });

        // Current Reply (if exists)
        if (!string.IsNullOrEmpty(Outbound.ReplyAttachmentUrl))
        {
             items.Add(new Models.TimelineItem
            {
                Date = Outbound.UpdatedAt,
                Title = "رد / إجراء",
                Description = "تمت إضافة مرفق الرد",
                Type = Models.TimelineItemType.Response,
                AttachmentUrl = Outbound.ReplyAttachmentUrl
            });
        }

        // Sort by date ascending (oldest first)
        Timeline = new ObservableCollection<Models.TimelineItem>(items.OrderBy(x => x.Date));
    }

    private async void ExecuteSaveOriginalAttachment(object? param)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var outbound = await context.Outbounds.FindAsync(_outbound.Id);
            if (outbound != null)
            {
                outbound.OriginalAttachmentUrl = OriginalAttachmentUrl;
                outbound.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                MessageBox.Show("تم حفظ مرفق الصادر الأصلي بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                _ = LoadDetailsAsync();
            }
        }
        catch (Exception ex)
        {
             MessageBox.Show($"خطأ في حفظ المرفق: {ex.Message}");
        }
    }

    private async void ExecuteSaveReplyAttachment(object? param)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var outbound = await context.Outbounds.FindAsync(_outbound.Id);
            if (outbound != null)
            {
                outbound.ReplyAttachmentUrl = ReplyAttachmentUrl;
                outbound.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                MessageBox.Show("تم حفظ مرفق الرد بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                _ = LoadDetailsAsync();
            }
        }
        catch (Exception ex)
        {
             MessageBox.Show($"خطأ في حفظ المرفق: {ex.Message}");
        }
    }

    private void ExecuteOpenTimelineAttachment(object? param)
    {
        if (param is Models.TimelineItem item && !string.IsNullOrEmpty(item.AttachmentUrl))
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = item.AttachmentUrl, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"لا يمكن فتح الملف: {ex.Message}");
            }
        }
    }

    // ... existing Execute methods ...

    private async void ExecuteDelete(object? param)
    {
        var result = MessageBox.Show("هل أنت متأكد من حذف هذا الموضوع الصادر؟ لا يمكن التراجع عن هذا الإجراء.", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var outbound = await context.Outbounds.FindAsync(Outbound.Id);
            if (outbound != null)
            {
                context.Outbounds.Remove(outbound);
                await context.SaveChangesAsync();
                MessageBox.Show("تم حذف الموضوع بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                ExecuteClose(null);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في الحذف: {ex.Message}");
        }
    }

    private void ExecuteClose(object? param)
    {
        RequestClose?.Invoke();
    }

    private void ExecuteOpenAttachment(object? param)
    {
        if (param is string path && !string.IsNullOrEmpty(path))
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"لا يمكن فتح الملف: {ex.Message}");
            }
        }
    }

    private void ExecuteGoToInbound(object? param)
    {
        if (_inboundId.HasValue)
        {
            RequestNavigationToInbound?.Invoke(_inboundId.Value);
        }
    }
}
