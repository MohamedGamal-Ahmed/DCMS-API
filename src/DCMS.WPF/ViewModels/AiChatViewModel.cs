using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DCMS.Application.Interfaces;
using DCMS.WPF.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using DCMS.Infrastructure.Data;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using DCMS.WPF.Services;
using System.Text.RegularExpressions;
using DCMS.WPF.Models;

namespace DCMS.WPF.ViewModels;

// PrivateChatMessageViewModel moved to SignalRService.cs

public class AiChatMessageViewModel : ViewModelBase
{
    private string _content = string.Empty;
    private int _logId;
    
    public int LogId 
    { 
        get => _logId;
        set => SetProperty(ref _logId, value);
    }
    
    public string Role { get; set; } = string.Empty;
    
    public string Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }
    
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsUser => Role == "user";
    public bool IsAssistant => Role == "assistant";

    private ObservableCollection<string> _interactiveButtons = new();
    public ObservableCollection<string> InteractiveButtons
    {
        get => _interactiveButtons;
        set => SetProperty(ref _interactiveButtons, value);
    }
}

public class PendingItemViewModel : ViewModelBase
{
    public int Id { get; set; }
    public string SubjectNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? TransferredTo { get; set; }
    public string? ResponsibleEngineer { get; set; }
    public int DaysDelayed { get; set; }
    public string DelayType { get; set; } = string.Empty;
    public bool IsCritical => DaysDelayed >= 4;
}

public class OnlineUserViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private string _userName = string.Empty;
    private bool _hasUnreadMessages;

    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    public bool HasUnreadMessages
    {
        get => _hasUnreadMessages;
        set => SetProperty(ref _hasUnreadMessages, value);
    }

    public override string ToString() => UserName;
}

public class AiChatViewModel : ViewModelBase
{
    private readonly IAiService _aiService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly DCMS.WPF.Services.CurrentUserService _currentUserService;
    private readonly IAiDashboardService _aiDashboardService;
    private readonly IRecordNavigationService _recordNavigationService;
    private readonly SignalRService _signalRService;

    private string _userInput = string.Empty;
    private string _chatMessage = string.Empty;
    private bool _isBusy;
    private string _statusText = string.Empty;
    private bool _isChatConnected;
    private ObservableCollection<AiChatMessageViewModel> _messages = new();
    private ObservableCollection<PrivateChatMessageViewModel> _filteredMessages = new();
    private ObservableCollection<OnlineUserViewModel> _filteredOnlineUsers = new();
    private string? _selectedUser;
    private ObservableCollection<string> _quickCommands = new();
    
    private int _totalInternalTransactions;
    private int _criticalExternalDelays;
    private string _fastestEngineer = "-";
    private int _overallCompletionRate;
    
    private ObservableCollection<PendingItemViewModel> _pendingManagerReview;
    private ObservableCollection<PendingItemViewModel> _pendingConsultantResponse;
    private ObservableCollection<PendingItemViewModel> _missingAttachments;
    private string _diagnosticLog = string.Empty;
    private bool _showDiagnostics;
    private bool _isChatOpen; // For floating chat popup

    // EMERGENCY CACHE: Cache the welcome message to avoid repeated AI hits on view switch
    private static string? _cachedWelcomeContent;
    private static int _cachedWelcomeLogId;
    private static List<string> _cachedWelcomeButtons = new();
    private static DateTime _lastWelcomeCacheTime = DateTime.MinValue;

    public AiChatViewModel(
        IAiService aiService, 
        IServiceProvider serviceProvider, 
        IDbContextFactory<DCMSDbContext> contextFactory,
        IAiDashboardService aiDashboardService,
        IRecordNavigationService recordNavigationService,
        SignalRService signalRService)
    {
        _aiService = aiService;
        _serviceProvider = serviceProvider;
        _contextFactory = contextFactory;
        _aiDashboardService = aiDashboardService;
        _recordNavigationService = recordNavigationService;
        _signalRService = signalRService; // Use injected service
        _currentUserService = serviceProvider.GetRequiredService<DCMS.WPF.Services.CurrentUserService>();
        
        // Initialize collections that aren't initialized inline
        _pendingManagerReview = new ObservableCollection<PendingItemViewModel>();
        _pendingConsultantResponse = new ObservableCollection<PendingItemViewModel>();
        _missingAttachments = new ObservableCollection<PendingItemViewModel>();
        
        // Populate quick commands
        _quickCommands.Add("ما هي اجتماعات اليوم؟");
        _quickCommands.Add("عرض آخر المراسلات الواردة");
        _quickCommands.Add("هل يوجد مراسلات متأخرة؟");

        // Subscribe to global unread and pulse events
        _signalRService.UnreadMessagesChanged += () => OnPropertyChanged(nameof(UnreadMessageCount));
        _signalRService.PulseFab += TriggerFabPulse;

        // Register handlers
        _signalRService.OnlineUsersUpdated += OnOnlineUsersUpdated;
        _signalRService.UserConnected += OnUserConnected;
        _signalRService.UserDisconnected += OnUserDisconnected;
        _signalRService.PrivateMessageReceived += OnPrivateMessageReceived;
        _signalRService.ErrorReceived += OnSignalRErrorReceived;
        _signalRService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _signalRService.WakingUpStatusChanged += OnWakingUpStatusChanged;
        _signalRService.UnreadMessagesChanged += OnUnreadMessagesChanged;
        
        IsChatConnected = _signalRService.IsConnected;

        // Populate initial global state for this transient instance
        UpdateFilteredUsers();
        SelectedUser = _signalRService.LastSelectedUser;
        
        // Initialize commands
        SendCommand = new RelayCommand(async _ => await ExecuteSend(), _ => !string.IsNullOrWhiteSpace(UserInput) && !IsBusy);
        SendChatMessageCommand = new RelayCommand(async _ => await ExecuteSendPrivateMessage(), _ => !string.IsNullOrWhiteSpace(ChatMessage) && SelectedUser != null && IsChatConnected);
        ClearCommand = new RelayCommand(_ => Messages.Clear());
        QuickCommand = new RelayCommand(async p => await ExecuteQuickCommand(p?.ToString() ?? ""));
        OpenRecordCommand = new RelayCommand(async p => await _recordNavigationService.OpenRecordAsync(p?.ToString() ?? ""));
        ThumbsUpCommand = new RelayCommand<AiChatMessageViewModel>(msg => ExecuteSetFeedback(msg, true));
        ThumbsDownCommand = new RelayCommand<AiChatMessageViewModel>(msg => ExecuteSetFeedback(msg, false));
        CardClickCommand = new RelayCommand<PendingItemViewModel>(item => ExecuteCardClick(item));
        ToggleDiagnosticsCommand = new RelayCommand(_ => ShowDiagnostics = !ShowDiagnostics);
        DebugMatchCommand = new RelayCommand(async _ => await ExecuteDebugMatch());
        RefreshUsersCommand = new RelayCommand(async _ => await _signalRService.RequestOnlineUsersAsync());
        ToggleChatCommand = new RelayCommand(_ => IsChatOpen = !IsChatOpen);
        AddReactionCommand = new RelayCommand(p => _ = ExecuteAddReaction(p));

        // Always trigger connection/registration logic
        _ = ConnectAndRegisterAsync();
        _ = LoadDashboardDataAsync();
    }

    public void Cleanup()
    {
        _signalRService.UnreadMessagesChanged -= OnUnreadMessagesChanged;
        _signalRService.UnreadMessagesChanged -= () => OnPropertyChanged(nameof(UnreadMessageCount));
        _signalRService.PulseFab -= TriggerFabPulse;
        _signalRService.OnlineUsersUpdated -= OnOnlineUsersUpdated;
        _signalRService.UserConnected -= OnUserConnected;
        _signalRService.UserDisconnected -= OnUserDisconnected;
        _signalRService.PrivateMessageReceived -= OnPrivateMessageReceived;
        _signalRService.ErrorReceived -= OnSignalRErrorReceived;
        _signalRService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _signalRService.WakingUpStatusChanged -= OnWakingUpStatusChanged;
    }

    private void LogDiagnostic(string message)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            DiagnosticLog += $"[{timestamp}] {message}{Environment.NewLine}";
            System.Diagnostics.Debug.WriteLine($"[SignalR_UI] {message}");
        });
    }

    private async Task ConnectAndRegisterAsync()
    {
        try
        {
            LogDiagnostic($"🔌 ConnectAndRegisterAsync Started");
            if (_signalRService.IsConnected)
            {
                LogDiagnostic("ℹ️ SignalR already connected at startup. Proceeding to registration...");
            }
            
            LogDiagnostic($"📍 Hub URL: {_signalRService.HubUrl}");
            LogDiagnostic($"👤 CurrentUserFullName: '{_currentUserService.CurrentUserFullName ?? "(null)"}'");
            LogDiagnostic($"👤 CurrentUserName: '{_currentUserService.CurrentUserName ?? "(null)"}'");
            
            // Try connection (service handles if already connected)
            var error = await _signalRService.ConnectAsync();
           
            LogDiagnostic($"🔗 Connection attempt result. IsConnected: {_signalRService.IsConnected}");
            
            if (_signalRService.IsConnected)
            {
                var myName = _currentUserService.CurrentUserFullName;
                if (string.IsNullOrEmpty(myName)) myName = _currentUserService.CurrentUserName;
                
                LogDiagnostic($"✅ Connection verified. Registering as: '{myName}'");
                await _signalRService.RegisterUserAsync(myName);
                LogDiagnostic($"📡 RegisterUser call completed");
            }
            else
            {
                LogDiagnostic($"❌ Connection Failed: {error ?? "Unknown Error"}");
            }
        }
        catch (Exception ex)
        {
            LogDiagnostic($"💥 EXCEPTION in ConnectAndRegisterAsync: {ex.Message}");
            LogDiagnostic($"💥 StackTrace: {ex.StackTrace}");
        }
    }


    private void OnConnectionStatusChanged(bool connected)
    {
        IsChatConnected = connected;
        LogDiagnostic(connected ? "✅ Chat Hub Connected." : "❌ Chat Hub Disconnected.");
        
        if (connected)
        {
            var myName = GetMyDisplayName();
            LogDiagnostic($"📡 Auto-registering as '{myName}' after connection change.");
            _ = _signalRService.RegisterUserAsync(myName);
        }
    }

    private string GetMyDisplayName()
    {
        var name = _currentUserService.CurrentUserFullName;
        if (string.IsNullOrWhiteSpace(name)) name = _currentUserService.CurrentUserName;
        return name ?? "Unknown";
    }
    
    // Properties
    public int TotalInternalTransactions { get => _totalInternalTransactions; set => SetProperty(ref _totalInternalTransactions, value); }
    public int CriticalExternalDelays { get => _criticalExternalDelays; set => SetProperty(ref _criticalExternalDelays, value); }
    public string FastestEngineer { get => _fastestEngineer; set => SetProperty(ref _fastestEngineer, value); }
    public int OverallCompletionRate { get => _overallCompletionRate; set => SetProperty(ref _overallCompletionRate, value); }
    public string DiagnosticLog { get => _diagnosticLog; set => SetProperty(ref _diagnosticLog, value); }
    public bool ShowDiagnostics { get => _showDiagnostics; set => SetProperty(ref _showDiagnostics, value); }
    public bool IsChatOpen 
    { 
        get => _isChatOpen; 
        set 
        { 
            if (SetProperty(ref _isChatOpen, value) && value)
            {
                // Clear unread count when chat is opened
                _signalRService.ClearTotalUnreadCount();
                if (!string.IsNullOrEmpty(SelectedUser))
                {
                    _ = _signalRService.MarkMessagesAsReadAsync(SelectedUser);
                    // Ensure the view is synced when opening
                    FilterConversations();
                }
            }
        } 
    }
    public int UnreadMessageCount => _signalRService.TotalUnreadCount;
    public bool FabShouldPulse 
    { 
        get => _fabShouldPulse; 
        private set => SetProperty(ref _fabShouldPulse, value); 
    }
    private bool _fabShouldPulse;
    public ObservableCollection<PendingItemViewModel> PendingManagerReview { get => _pendingManagerReview; set => SetProperty(ref _pendingManagerReview, value); }
    public ObservableCollection<PendingItemViewModel> PendingConsultantResponse { get => _pendingConsultantResponse; set => SetProperty(ref _pendingConsultantResponse, value); }
    public ObservableCollection<PendingItemViewModel> MissingAttachments { get => _missingAttachments; set => SetProperty(ref _missingAttachments, value); }
    public string UserRole => _currentUserService.CurrentUserRole ?? "Unknown";
    public bool ShowTechnicalColumns => UserRole == "Admin" || UserRole == "TechnicalManager" || UserRole == "OfficeManager";
    public bool ShowFollowUpColumns => UserRole == "Admin" || UserRole == "FollowUpStaff" || UserRole == "OfficeManager";
    public ObservableCollection<AiChatMessageViewModel> Messages { get => _messages; set => SetProperty(ref _messages, value); }
    public ObservableCollection<PrivateChatMessageViewModel> FilteredMessages { get => _filteredMessages; set => SetProperty(ref _filteredMessages, value); }
    public string UserInput { get => _userInput; set => SetProperty(ref _userInput, value); }
    public string ChatMessage { get => _chatMessage; set => SetProperty(ref _chatMessage, value); }
    public bool IsBusy { get => _isBusy; set { if (SetProperty(ref _isBusy, value)) OnPropertyChanged(nameof(StatusVisibility)); } }
    public bool IsChatConnected { get => _isChatConnected; set => SetProperty(ref _isChatConnected, value); }
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public Visibility StatusVisibility => IsBusy ? Visibility.Visible : Visibility.Collapsed;
    public ObservableCollection<OnlineUserViewModel> OnlineUsers => _filteredOnlineUsers;
    public string? SelectedUser 
    { 
        get => _selectedUser; 
        set 
        { 
            if (SetProperty(ref _selectedUser, value))
            {
                LogDiagnostic($"👤 SelectedUser changed to: '{value}'");
                _signalRService.LastSelectedUser = value;
                FilterConversations();
                if (!string.IsNullOrEmpty(value))
                {
                    _signalRService.ClearUnreadForUser(value);
                    if (IsChatOpen)
                    {
                        _ = _signalRService.MarkMessagesAsReadAsync(value);
                    }
                }
            }
        } 
    }

    private OnlineUserViewModel? _selectedOnlineUser;
    public OnlineUserViewModel? SelectedOnlineUser
    {
        get => _selectedOnlineUser;
        set
        {
            if (SetProperty(ref _selectedOnlineUser, value))
            {
                SelectedUser = value?.UserName;
            }
        }
    }

    // Method for UI binding to check if a user has unread messages
    public bool HasUnreadMessages(string userName) => _signalRService.HasUnreadMessages(userName);

    public ICommand SendCommand { get; }
    public ICommand SendChatMessageCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand QuickCommand { get; }
    public ICommand OpenRecordCommand { get; }
    public ICommand ThumbsUpCommand { get; }
    public ICommand ThumbsDownCommand { get; }
    public ICommand CardClickCommand { get; }
    public ICommand ToggleDiagnosticsCommand { get; }
    public ICommand DebugMatchCommand { get; }
    public ICommand RefreshUsersCommand { get; }
    public ICommand ToggleChatCommand { get; }
    public ICommand AddReactionCommand { get; }

    private void ExecuteCardClick(PendingItemViewModel? item)
    {
        if (item == null || item.Id == 0) return;
        _ = _recordNavigationService.OpenRecordAsync($"record://inbound/{item.Id}");
    }

    private async Task LoadDashboardDataAsync()
    {
        IsBusy = true;
        try
        {
            await Task.Delay(1000); // 1. Simulation for Skeleton Effect

            var data = await _aiDashboardService.GetAiDashboardDataAsync(
                _currentUserService.CurrentUserId ?? 0,
                _currentUserService.CurrentUserRole,
                _currentUserService.CurrentUserFullName,
                _currentUserService.CurrentUserName);

            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                TotalInternalTransactions = data.TotalInternalTransactions;
                CriticalExternalDelays = data.CriticalExternalDelays;
                FastestEngineer = data.FastestEngineer;
                OverallCompletionRate = data.OverallCompletionRate;
                LogDiagnostic("--- DASHBOARD DIAGNOSTICS ---");
                LogDiagnostic(data.DiagnosticLog);
                LogDiagnostic("-----------------------------");

                UpdateCollection(PendingManagerReview, data.PendingManagerReview);
                UpdateCollection(PendingConsultantResponse, data.PendingConsultantResponse);
                UpdateCollection(MissingAttachments, data.MissingAttachments);
            });
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DASHBOARD_ERROR] {ex.Message}"); }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateCollection(ObservableCollection<PendingItemViewModel> collection, IEnumerable<AiPendingItemDto> items)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            collection.Clear();
            foreach (var item in items)
            {
                collection.Add(new PendingItemViewModel
                {
                    Id = item.Id,
                    SubjectNumber = item.SubjectNumber,
                    Subject = item.Subject,
                    TransferredTo = item.TransferredTo,
                    ResponsibleEngineer = item.ResponsibleEngineer,
                    DaysDelayed = item.DaysDelayed,
                    DelayType = item.DelayType
                });
            }
        });
    }

    private async Task ExecuteQuickCommand(string parameter)
    {
        UserInput = parameter;
        await ExecuteSend();
    }

    private async Task ExecuteSend()
    {
        if (string.IsNullOrWhiteSpace(UserInput) || IsBusy) return;

        var userPrompt = UserInput.Trim();
        UserInput = string.Empty;
        IsBusy = true;
        StatusText = "جاري التفكير...";

        var userMessage = new AiChatMessageViewModel { Role = "user", Content = userPrompt };
        Messages.Add(userMessage);

        var history = Messages.Select(m => new ChatMessage { Role = m.Role, Content = m.Content }).TakeLast(11).SkipLast(1).ToList();

        try
        {
            var assistantMessage = new AiChatMessageViewModel { Role = "assistant", Content = string.Empty };
            Messages.Add(assistantMessage);

            var response = await _aiService.GetResponseAsync(userPrompt, history, (status) => StatusText = status);

            var content = response.Content;
            if (content.Contains("BUTTONS:"))
            {
                var lines = content.Split('\n');
                var buttonsLine = lines.FirstOrDefault(l => l.Trim().StartsWith("BUTTONS:"));
                if (buttonsLine != null)
                {
                    var buttonsPart = buttonsLine.Substring(buttonsLine.IndexOf('[')).Trim();
                    var buttonLabels = buttonsPart.Split(new[] { '[', ']', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(b => b.Trim()).Where(b => !string.IsNullOrEmpty(b));
                    foreach (var label in buttonLabels) assistantMessage.InteractiveButtons.Add(label);
                    content = string.Join("\n", lines.Where(l => l != buttonsLine)).Trim();
                }
            }

            assistantMessage.Content = content;
            assistantMessage.LogId = response.LogId;
        }
        catch (Exception ex) { Messages.Add(new AiChatMessageViewModel { Role = "assistant", Content = $"عذراً، حدث خطأ: {ex.Message}" }); }
        finally { IsBusy = false; StatusText = string.Empty; }
    }

    private async Task ExecuteSendPrivateMessage()
    {
        if (string.IsNullOrWhiteSpace(ChatMessage) || string.IsNullOrEmpty(SelectedUser)) return;

        var messageId = Guid.NewGuid().ToString();
        await _signalRService.SendPrivateMessageAsync(SelectedUser, ChatMessage, messageId);
        ChatMessage = string.Empty;
    }

    private async Task ExecuteAddReaction(object? parameter)
    {
        if (parameter is object[] values && values.Length == 2 && values[0] is string messageId && values[1] is string emoji && !string.IsNullOrEmpty(SelectedUser))
        {
            await _signalRService.AddReactionAsync(SelectedUser, messageId, emoji);
        }
    }

    private void OnPrivateMessageReceived(string sender, string recipient, string message, DateTime timestamp, string messageId)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            LogDiagnostic($"📩 Message Event: Sender='{sender}', Recipient='{recipient}', ID='{messageId}'");
            
            bool isMe = _signalRService.IsMe(sender);
            string partner = isMe ? recipient : sender;

            LogDiagnostic($"🔍 Checking match: Partner='{partner}', SelectedUser='{SelectedUser}'");

            if (string.Equals(partner, SelectedUser, StringComparison.OrdinalIgnoreCase))
            {
                LogDiagnostic("✅ Match found! Collection should auto-update via binding.");
                
                // If we're looking at the chat and it's from the other person, mark it as read immediately
                if (!isMe && IsChatOpen)
                {
                    _ = _signalRService.MarkMessagesAsReadAsync(partner);
                }
            }
            else if (!isMe)
            {
                // Message is from someone else and not for currently selected user
                LogDiagnostic($"🔔 Marking '{sender}' as having unread messages.");
                _signalRService.MarkUserAsUnread(sender);
            }
        });
    }

    private void UpdateFilteredUsers()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            LogDiagnostic("🔍 Refreshing Filtered Online Users...");
            _filteredOnlineUsers.Clear();
            foreach (var user in _signalRService.OnlineUsers)
            {
                if (!_signalRService.IsMe(user))
                {
                    _filteredOnlineUsers.Add(new OnlineUserViewModel 
                    { 
                        UserName = user,
                        HasUnreadMessages = _signalRService.HasUnreadMessages(user)
                    });
                }
            }
        });
    }

    private async void TriggerFabPulse()
    {
        if (IsChatOpen) return; // Don't pulse if already open
        FabShouldPulse = true;
        await Task.Delay(600); // Animation duration
        FabShouldPulse = false;
    }

    private void OnUnreadMessagesChanged()
    {
        // Update the unread status for all users in the list
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var userVm in _filteredOnlineUsers)
            {
                userVm.HasUnreadMessages = _signalRService.HasUnreadMessages(userVm.UserName);
            }
        });
    }

    private void OnOnlineUsersUpdated(List<string> users)
    {
        LogDiagnostic($"📨 ReceiveOnlineUsers Received! Count: {users.Count}");
        UpdateFilteredUsers();
    }

    private void OnUserConnected(string userName)
    {
        LogDiagnostic($"👋 UserConnected: '{userName}'");
        UpdateFilteredUsers();
    }

    private void OnUserDisconnected(string userName)
    {
        LogDiagnostic($"👋 UserDisconnected: '{userName}'");
        UpdateFilteredUsers();
    }

    private void FilterConversations()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (string.IsNullOrEmpty(SelectedUser)) 
            {
                LogDiagnostic("⚠️ FilterConversations: No user selected.");
                FilteredMessages = new ObservableCollection<PrivateChatMessageViewModel>();
                return;
            }

            LogDiagnostic($"🔍 Switching collection for: '{SelectedUser}'");

            if (_signalRService.Conversations.TryGetValue(SelectedUser, out var conversation))
            {
                LogDiagnostic($"✅ Found existing conversation with {conversation.Count} messages. Binding directly.");
                FilteredMessages = conversation;
            }
            else
            {
                LogDiagnostic("ℹ️ No conversation found for this user. Creating new empty collection.");
                var newColl = new ObservableCollection<PrivateChatMessageViewModel>();
                _signalRService.Conversations[SelectedUser] = newColl;
                FilteredMessages = newColl;
            }
        });
    }

    private void OnSignalRErrorReceived(string error)
    {
        LogDiagnostic($"SIGNALR ERROR: {error}");
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(error, "تنبيه أمني", MessageBoxButton.OK, MessageBoxImage.Warning);
        });
    }

    private void OnWakingUpStatusChanged(bool wakingUp)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            IsBusy = wakingUp;
            if (wakingUp)
            {
                StatusText = "جاري إيقاظ السيرفر والاتصال... يرجى الانتظار ⏳";
                LogDiagnostic("⏳ Server wake-up in progress...");
            }
            else
            {
                if (StatusText.Contains("إيقاظ")) StatusText = string.Empty;
            }
        });
    }
    private async Task LoadProactiveBriefAsync(AiChatMessageViewModel welcomeMessage)
    {
        try
        {
            // Use cache if fresh (last 30 minutes)
            if (_cachedWelcomeContent != null && (DateTime.Now - _lastWelcomeCacheTime).TotalMinutes < 30)
            {
                welcomeMessage.Content = _cachedWelcomeContent;
                welcomeMessage.LogId = _cachedWelcomeLogId;
                foreach (var btn in _cachedWelcomeButtons) welcomeMessage.InteractiveButtons.Add(btn);
                return;
            }

            var response = await _aiService.GetResponseAsync("مرحباً", new List<ChatMessage>(), null);
            var content = response.Content;
            var buttons = new List<string>();

            if (content.Contains("BUTTONS:"))
            {
                var lines = content.Split('\n');
                var buttonsLine = lines.FirstOrDefault(l => l.Trim().StartsWith("BUTTONS:"));
                if (buttonsLine != null)
                {
                    var buttonsPart = buttonsLine.Substring(buttonsLine.IndexOf('['));
                    var buttonMatches = System.Text.RegularExpressions.Regex.Matches(buttonsPart, @"\[([^\]]+)\]");
                    foreach (System.Text.RegularExpressions.Match match in buttonMatches)
                    {
                        var label = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(label)) 
                        {
                            welcomeMessage.InteractiveButtons.Add(label);
                            buttons.Add(label);
                        }
                    }
                    content = string.Join("\n", lines.Where(l => !l.Trim().StartsWith("BUTTONS:"))).Trim();
                }
            }
            welcomeMessage.Content = content;
            welcomeMessage.LogId = response.LogId;

            // Save to cache
            _cachedWelcomeContent = content;
            _cachedWelcomeLogId = response.LogId;
            _cachedWelcomeButtons = buttons;
            _lastWelcomeCacheTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            welcomeMessage.Content = "مرحباً! أنا مساعدك الذكي في نظام DCMS. كيف يمكنني مساعدتك اليوم؟";
        }
    }

    private async void ExecuteSetFeedback(AiChatMessageViewModel? message, bool isPositive)
    {
        if (message == null || message.LogId <= 0) return;
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var log = await context.AiRequestLogs.FindAsync(message.LogId);
            if (log != null)
            {
                log.UserFeedback = isPositive;
                log.IsSuccess = isPositive;
                await context.SaveChangesAsync();
                StatusText = isPositive ? "تم إرسال تقييم إيجابي، شكراً لك!" : "شكراً لملاحظاتك، سنعمل على تحسين الردود.";
                _ = Task.Run(async () => { await Task.Delay(3000); if (StatusText.Contains("شكراً")) StatusText = string.Empty; });
            }
        }
        catch (Exception ex) { StatusText = "عذراً، فشل إرسال التقييم."; }
    }

    private async Task ExecuteDebugMatch()
    {
        IsBusy = true;
        StatusText = "جاري فحص قاعدة البيانات...";
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var fullName = _currentUserService.CurrentUserFullName ?? "";
            var allNames = await context.Inbounds.Where(i => !string.IsNullOrEmpty(i.ResponsibleEngineer)).Select(i => i.ResponsibleEngineer).Distinct().ToListAsync();
            var message = new System.Text.StringBuilder();
            message.AppendLine("🔍 **متطابق أسماء المهندسين:**");
            message.AppendLine($"اسمك الحالي: `{fullName}`");
            message.AppendLine("---");
            message.AppendLine("الأسماء المسجلة في قاعدة البيانات حالياً:");
            foreach(var name in allNames.Take(30)) message.AppendLine($"- {name}");
            message.AppendLine("---");
            message.AppendLine("💡 إذا وجد اسمك بالعربي في هذه القائمة، قم بتغيير 'الاسم الكامل' في إعدادات حسابك بالتطابق التام مع الاسم في القائمة.");
            Messages.Add(new AiChatMessageViewModel { Role = "assistant", Content = message.ToString() });
        }
        catch (Exception ex) { Messages.Add(new AiChatMessageViewModel { Role = "assistant", Content = $"Error: {ex.Message}" }); }
        finally { IsBusy = false; StatusText = string.Empty; }
    }
}
