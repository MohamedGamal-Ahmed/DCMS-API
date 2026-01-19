using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using DCMS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Collections.Specialized;
using System;

namespace DCMS.WPF;

public partial class MainWindow : Window
{
    private readonly ViewModels.MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private readonly NotificationService _notificationService;
    private readonly IdleDetectorService _idleDetector;
    private readonly DatabasePollingService _databasePollingService;
    private readonly ViewModels.GlobalSearchViewModel _globalSearchVm;
    private readonly ViewModels.AiChatViewModel _aiChatVm;
    public NotificationService NotificationService => _notificationService;
    private readonly ViewModels.RecentItemsViewModel _recentItemsVm;
    private Views.LockScreenView? _lockScreenView;
    private ViewModels.LockScreenViewModel? _lockScreenVm;

    public MainWindow(IServiceProvider serviceProvider, ViewModels.MainViewModel viewModel, IdleDetectorService idleDetector, 
        DatabasePollingService databasePollingService,
        ViewModels.GlobalSearchViewModel globalSearchVm, ViewModels.RecentItemsViewModel recentItemsVm,
        ViewModels.AiChatViewModel aiChatVm)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _viewModel = viewModel;
        _aiChatVm = aiChatVm;
        DataContext = _viewModel;
        
        _notificationService = serviceProvider.GetRequiredService<NotificationService>();
        _idleDetector = idleDetector;
        _databasePollingService = databasePollingService;
        _globalSearchVm = globalSearchVm;
        _recentItemsVm = recentItemsVm;

        // Setup Chat Overlay
        GlobalChatOverlay.DataContext = _aiChatVm;
        
        // Auto-scroll for Global Chat
        if (_aiChatVm.FilteredMessages is INotifyCollectionChanged notifyChat)
        {
            notifyChat.CollectionChanged += OnFilteredMessagesChanged;
        }

        // Subscribe to Logout
        _viewModel.RequestLogout += (s, e) => ExecuteLogout();

        // Setup Global Search
        searchControl.DataContext = _globalSearchVm;
        _globalSearchVm.ResultSelected += OnSearchResultSelected;
        
        // Setup Recent Items
        recentItemsControl.DataContext = _recentItemsVm;
        _recentItemsVm.ItemSelected += OnRecentItemSelected;

        // Start Services for this session
        _notificationService.Start();
        
        _idleDetector.IdleDetected += OnIdleDetected;
        _idleDetector.Start();

        // Setup Notifications
        SetupNotifications();

        // Subscribe to Navigation Changes for Animation
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.CurrentView))
            {
                Dispatcher.Invoke(TriggerSlideAnimation);
            }
        };

        // Handle window closing for proper cleanup
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Stop services
        _notificationService?.Stop();
        _idleDetector?.Stop();
        _databasePollingService?.Suspend();
        _aiChatVm?.Cleanup();

        // Unhook local events
        if (_aiChatVm?.FilteredMessages is INotifyCollectionChanged notifyChat)
        {
            notifyChat.CollectionChanged -= OnFilteredMessagesChanged;
        }

        // Unhook events to prevent memory leaks
        _viewModel.RequestLogout -= (s, ev) => ExecuteLogout();
        _globalSearchVm.ResultSelected -= OnSearchResultSelected;
        _recentItemsVm.ItemSelected -= OnRecentItemSelected;
        _idleDetector.IdleDetected -= OnIdleDetected;
    }

    private void OnFilteredMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            Dispatcher.InvokeAsync(() =>
            {
                ChatScrollViewer?.ScrollToEnd();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    private void BtnNotifications_Click(object sender, RoutedEventArgs e)
    {
        popNotifications.IsOpen = !popNotifications.IsOpen;
    }

    private void SetupNotifications()
    {
        // Subscribe to notifications
        _notificationService.NotificationAdded += (s, e) => Dispatcher.Invoke(UpdateNotifications);
        
        // EMERGENCY: Auto-refresh DISABLED to save bandwidth (95% capacity reached)
        // Notifications will only refresh on manual popup open or app restart
        // var notificationTimer = new System.Windows.Threading.DispatcherTimer();
        // notificationTimer.Interval = TimeSpan.FromSeconds(30);
        // notificationTimer.Tick += (s, e) => UpdateNotifications();
        // notificationTimer.Start();
        
        UpdateNotifications();
    }

    private async void BtnClearNotifications_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            await _notificationService.MarkAllAsRead(_viewModel.CurrentUser.Id);
            UpdateNotifications();

            if (btn != null) btn.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"حدث خطأ أثناء مسح الإشعارات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void UpdateNotifications()
    {
        try
        {
            var notifications = await _notificationService.GetUnreadNotifications(_viewModel.CurrentUser.Id);
            var count = notifications.Count;
            
            txtNotificationCount.Text = count > 9 ? "9+" : count.ToString();
            bdrNotificationBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            
            // Update popup content
            stkNotificationsList.Children.Clear();
            
            if (count == 0)
            {
                stkNotificationsList.Children.Add(new TextBlock 
                { 
                    Text = "لا توجد إشعارات جديدة", 
                    HorizontalAlignment = HorizontalAlignment.Center, 
                    Margin = new Thickness(0, 20, 0, 0),
                    Foreground = Brushes.Gray
                });
            }
            else
            {
                foreach (var note in notifications)
                {
                    var border = new Border
                    {
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Padding = new Thickness(10),
                        Background = Brushes.White
                    };
                    
                    var grid = new Grid();
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    
                    var msgText = new TextBlock 
                    { 
                        Text = note.Message, 
                        TextWrapping = TextWrapping.Wrap, 
                        FontWeight = FontWeights.SemiBold 
                    };
                    
                    var timeText = new TextBlock 
                    { 
                        Text = GetTimeAgo(note.CreatedAt), 
                        FontSize = 10, 
                        Foreground = Brushes.Gray, 
                        Margin = new Thickness(0, 5, 0, 0) 
                    };
                    
                    Grid.SetRow(msgText, 0);
                    Grid.SetRow(timeText, 1);
                    
                    grid.Children.Add(msgText);
                    grid.Children.Add(timeText);
                    
                    border.Child = grid;
                    
                    border.MouseLeftButtonUp += async (s, e) => 
                    {
                        await _notificationService.MarkAsRead(note.Id);
                        UpdateNotifications();
                    };
                    border.Cursor = System.Windows.Input.Cursors.Hand;
                    
                    stkNotificationsList.Children.Add(border);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating notifications: {ex.Message}");
        }
    }

    private string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;
        if (span.TotalMinutes < 1) return "الآن";
        if (span.TotalMinutes < 60) return $"منذ {span.TotalMinutes:0} دقيقة";
        return $"منذ {span.TotalDays:0} يوم";
    }

    private void TriggerSlideAnimation()
    {
        if (ContentArea == null) return;

        var storyboard = new System.Windows.Media.Animation.Storyboard();
        
        // Horizontal Slide Animation
        var slideAnimation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 300,
            To = 0,
            Duration = new Duration(TimeSpan.FromSeconds(0.4)),
            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
        };

        // Opacity Animation
        var opacityAnimation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0.5,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromSeconds(0.3))
        };

        System.Windows.Media.Animation.Storyboard.SetTarget(slideAnimation, ContentArea);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(slideAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

        System.Windows.Media.Animation.Storyboard.SetTarget(opacityAnimation, ContentArea);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));

        // Ensure RenderTransform is set
        if (ContentArea.RenderTransform is not TranslateTransform)
        {
            ContentArea.RenderTransform = new TranslateTransform();
        }

        storyboard.Children.Add(slideAnimation);
        storyboard.Children.Add(opacityAnimation);
        storyboard.Begin();
    }



    private void ExecuteLogout()
    {
        var result = MessageBox.Show("هل تريد تسجيل الخروج؟", "تأكيد", 
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            var loginView = _serviceProvider.GetRequiredService<Views.LoginView>();
            loginView.Show();
            this.Close();
        }
    }

    private void BtnAbout_Click(object sender, RoutedEventArgs e)
    {
        var aboutDialog = new Views.Dialogs.AboutDialog
        {
            Owner = this
        };
        aboutDialog.ShowDialog();
    }

    private void OnSearchResultSelected(ViewModels.GlobalSearchResultItem item)
    {
        Dispatcher.Invoke(() =>
        {
            if (item.Type == ViewModels.SearchResultType.Inbound && item.OriginalObject is Inbound inbound)
            {
                var detailsVm = new ViewModels.InboundDetailsViewModel(_serviceProvider.GetRequiredService<IDbContextFactory<DCMS.Infrastructure.Data.DCMSDbContext>>(), 
                    _serviceProvider, _notificationService, _serviceProvider.GetRequiredService<Services.CurrentUserService>(), _viewModel.CurrentUser, inbound);
                
                var detailsView = new Views.InboundDetailsView();
                detailsView.DataContext = detailsVm;
                detailsView.Owner = this;
                
                detailsVm.RequestClose += () => detailsView.Close();
                detailsView.ShowDialog();
                
                // Add to Recent Items
                _recentItemsVm.AddToRecent(inbound.Id.ToString(), inbound.Subject, Services.RecentItemType.Inbound);
            }
            else if (item.Type == ViewModels.SearchResultType.Outbound && item.OriginalObject is Outbound outbound)
            {
                 // Create View and ViewModel
                 var vm = new ViewModels.OutboundDetailsViewModel(
                     _serviceProvider.GetRequiredService<IDbContextFactory<DCMS.Infrastructure.Data.DCMSDbContext>>(),
                     _serviceProvider,
                     _serviceProvider.GetRequiredService<Services.CurrentUserService>(),
                     outbound);
                     
                 var view = new Views.OutboundDetailsView { DataContext = vm, Owner = this };
                 
                 vm.RequestClose += () => view.Close();
                 vm.RequestNavigationToInbound += (inboundId) => 
                 {
                     view.Close();
                     OpenInboundDetailsById(inboundId.ToString());
                 };
                 
                 view.ShowDialog();

                 // Add to Recent Items
                 _recentItemsVm.AddToRecent(outbound.Id.ToString(), outbound.Subject, Services.RecentItemType.Outbound);
            }
        });
    }

    private void OnRecentItemSelected(Services.RecentItem item)
    {
        Dispatcher.Invoke(() => 
        {
            if (item.Type == Services.RecentItemType.Inbound)
            {
                // We need to fetch the full object. For now we might need a method to get by ID.
                // Or we can just use the ID to open details if the DetailsViewModel supports loading by ID.
                // Currently InboundDetailsViewModel takes an Inbound object.
                // We should fetch it using EF.
                
                OpenInboundDetailsById(item.Id);
            }
            else if (item.Type == Services.RecentItemType.Outbound)
            {
                 // Outbound details are just a messagebox currently, implemented inline
                 // But we don't have the full object here.
                 // Ideally we should implement OpenOutboundDetailsById
            }
        });
    }

    private async void OpenInboundDetailsById(string idString)
    {
        try
        {
            if (!int.TryParse(idString, out int id)) return;

            using var scope = _serviceProvider.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DCMS.Infrastructure.Data.DCMSDbContext>>();
            using var context = await contextFactory.CreateDbContextAsync();
            
            var inbound = await context.Inbounds
                .Include(i => i.Transfers).ThenInclude(t => t.Engineer)
                .Include(i => i.ResponsibleEngineers).ThenInclude(r => r.Engineer)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inbound != null)
            {
                var detailsVm = new ViewModels.InboundDetailsViewModel(_serviceProvider.GetRequiredService<IDbContextFactory<DCMS.Infrastructure.Data.DCMSDbContext>>(), 
                    _serviceProvider, _notificationService, _serviceProvider.GetRequiredService<Services.CurrentUserService>(), _viewModel.CurrentUser, inbound);
                
                var detailsView = new Views.InboundDetailsView();
                detailsView.DataContext = detailsVm;
                detailsView.Owner = this;
                
                detailsVm.RequestClose += () => detailsView.Close();
                detailsView.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في فتح التفاصيل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnIdleDetected(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() => ShowLockScreen());
    }

    private void ShowLockScreen()
    {
        // Suspend database polling to save CU-hrs
        _databasePollingService.Suspend();
        _notificationService.Stop();

        // Create Lock Screen if not exists
        if (_lockScreenVm == null)
        {
            _lockScreenVm = new ViewModels.LockScreenViewModel(
                _serviceProvider.GetRequiredService<CurrentUserService>(),
                _databasePollingService);
            
            _lockScreenVm.UnlockSuccessful += OnUnlockSuccessful;
            _lockScreenVm.SignOutRequested += OnSignOutFromLockScreen;
        }

        if (_lockScreenView == null)
        {
            _lockScreenView = new Views.LockScreenView { DataContext = _lockScreenVm };
        }
        else
        {
            _lockScreenView.DataContext = _lockScreenVm;
        }

        // Inject into overlay
        LockScreenOverlay.Children.Clear();
        LockScreenOverlay.Children.Add(_lockScreenView);
        LockScreenOverlay.Visibility = Visibility.Visible;

        // Animate slide-in from right
        var slideIn = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = ActualWidth,
            To = 0,
            Duration = TimeSpan.FromSeconds(0.4),
            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
        };
        LockScreenTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slideIn);
    }

    private void HideLockScreen()
    {
        // Animate slide-out to right
        var slideOut = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0,
            To = ActualWidth,
            Duration = TimeSpan.FromSeconds(0.3),
            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
        };
        slideOut.Completed += (s, e) => LockScreenOverlay.Visibility = Visibility.Collapsed;
        LockScreenTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slideOut);

        // Resume services
        _notificationService.Start();
        _idleDetector.Start();
    }

    private void OnUnlockSuccessful(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(HideLockScreen);
    }

    private void OnSignOutFromLockScreen(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var loginView = _serviceProvider.GetRequiredService<Views.LoginView>();
            loginView.Show();
            this.Close();
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        // Unsubscribe from events to prevent memory leaks and crashes when Singleton ViewModels fire events for closed windows
        if (_globalSearchVm != null)
            _globalSearchVm.ResultSelected -= OnSearchResultSelected;
            
        if (_recentItemsVm != null)
            _recentItemsVm.ItemSelected -= OnRecentItemSelected;

        _notificationService.Stop();
        _idleDetector.Stop();
        _idleDetector.IdleDetected -= OnIdleDetected;
        base.OnClosed(e);
    }
}
