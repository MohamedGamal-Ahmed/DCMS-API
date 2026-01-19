using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace DCMS.WPF.Services
{
    public enum MessageStatus
    {
        Sent,
        Read
    }

    public class PrivateChatMessageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Sender { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsMe { get; set; }

        private MessageStatus _status = MessageStatus.Sent;
        public MessageStatus Status
        {
            get => _status;
            set { if (_status != value) { _status = value; OnPropertyChanged(); } }
        }

        private string _reactions = string.Empty;
        public string Reactions
        {
            get => _reactions;
            set { if (_reactions != value) { _reactions = value; OnPropertyChanged(); } }
        }
    }

    public class PendingMessage
    {
        public string Recipient { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class SignalRService
    {
        private HubConnection? _hubConnection;
        private readonly string _hubUrl;
        private readonly DCMS.Application.Interfaces.ICurrentUserService _currentUserService;
        private System.Timers.Timer? _keepAliveTimer;
        private string? _lastRegisteredName;
        private readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        private readonly ConcurrentQueue<PendingMessage> _pendingOutgoingMessages = new();
        private bool _isWakingUp;

        // Message Capping Setting
        private const int MaxMessagesPerConversation = 50;

        // Source of Truth - Bindable Collections
        public ObservableCollection<string> OnlineUsers { get; } = new();
        public ConcurrentDictionary<string, ObservableCollection<PrivateChatMessageViewModel>> Conversations { get; } = new(StringComparer.OrdinalIgnoreCase);
        
        // Track users with unread messages
        private readonly HashSet<string> _usersWithUnreadMessages = new(StringComparer.OrdinalIgnoreCase);
        
        // Global message lookup for "rocket" performance status updates
        private readonly ConcurrentDictionary<string, PrivateChatMessageViewModel> _allMessagesById = new();
        
        public IReadOnlyCollection<string> UsersWithUnreadMessages => _usersWithUnreadMessages.ToList();
        public event Action? UnreadMessagesChanged;
        public event Action? PulseFab;

        private int _totalUnreadCount;
        public int TotalUnreadCount 
        { 
            get => _totalUnreadCount; 
            private set
            {
                _totalUnreadCount = value;
                System.Windows.Application.Current.Dispatcher.Invoke(() => UnreadMessagesChanged?.Invoke());
            }
        }

        public string? LastSelectedUser { get; set; }

        public bool IsWakingUp 
        { 
            get => _isWakingUp; 
            private set 
            { 
                _isWakingUp = value; 
                WakingUpStatusChanged?.Invoke(value);
            } 
        }

        // Events
        public event Action<string, string, DateTime, string>? MessageReceived;
        public event Action<string>? ErrorReceived;
        public event Action<bool>? ConnectionStatusChanged;
        public event Action<bool>? WakingUpStatusChanged;
        public event Action<List<string>>? OnlineUsersUpdated;
        public event Action<string>? UserConnected;
        public event Action<string>? UserDisconnected;
        public event Action<string, string, string, DateTime, string>? PrivateMessageReceived;
        public event Action<string, string>? MessageStatusUpdated;
        public event Action<string, string>? ReactionAdded;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
        public string HubUrl => _hubUrl;

        public SignalRService(string hubUrl, DCMS.Application.Interfaces.ICurrentUserService currentUserService)
        {
            _hubUrl = hubUrl;
            _currentUserService = currentUserService;
        }

        public async Task<string?> ConnectAsync()
        {
            if (_hubConnection != null && IsConnected) 
            {
                await ProcessPendingMessagesAsync();
                return null;
            }

            try
            {
                // WAKE UP ON DEMAND: Ping health endpoint if server might be asleep
                IsWakingUp = true;
                System.Diagnostics.Debug.WriteLine("[SignalR] Waking up server via health ping...");
                
                var healthUrl = _hubUrl.Replace("/chatHub", "/health");
                if (healthUrl == _hubUrl) healthUrl = _hubUrl.Substring(0, _hubUrl.LastIndexOf('/')) + "/health";
                
                try 
                {
                    await _httpClient.GetAsync(healthUrl);
                    System.Diagnostics.Debug.WriteLine("[SignalR] Server responded to health ping.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SignalR] Health ping warning: {ex.Message}");
                    // Continue anyway, maybe it's reachable or health endpoint moved
                }

                if (_hubConnection == null)
                {
                    _hubConnection = new HubConnectionBuilder()
                        .WithUrl(_hubUrl)
                        .WithAutomaticReconnect()
                        .Build();
                        
                    _hubConnection.On<string, string, DateTime>("ReceiveMessage", (user, message, timestamp) =>
                    {
                        MessageReceived?.Invoke(user, message, timestamp, string.Empty);
                    });

                    _hubConnection.On<List<string>>("ReceiveOnlineUsers", (users) =>
                    {
                        UpdateOnlineUsers(users);
                        OnlineUsersUpdated?.Invoke(users);
                    });

                    _hubConnection.On<string>("UserConnected", (userName) =>
                    {
                        AddOnlineUser(userName);
                        UserConnected?.Invoke(userName);
                    });

                    _hubConnection.On<string>("UserDisconnected", (userName) =>
                    {
                        RemoveOnlineUser(userName);
                        UserDisconnected?.Invoke(userName);
                    });

                    // Support BOTH old (4-param) and new (5-param) Hub signatures for backward compatibility
                    _hubConnection.On<string, string, string, DateTime>("ReceivePrivateMessage", (sender, recipient, message, timestamp) =>
                    {
                        var messageId = Guid.NewGuid().ToString(); // Generate locally if not provided
                        HandleIncomingMessage(sender, recipient, message, timestamp, messageId);
                    });
                    _hubConnection.On<string, string, string, DateTime, string>("ReceivePrivateMessage", (sender, recipient, message, timestamp, messageId) =>
                    {
                        HandleIncomingMessage(sender, recipient, message, timestamp, messageId);
                    });

                    _hubConnection.On<string, MessageStatus>("ReceiveMessageStatus", (messageId, status) =>
                    {
                        UpdateLocalMessageStatus(messageId, status);
                        MessageStatusUpdated?.Invoke(messageId, status.ToString());
                    });

                    _hubConnection.On<string, string>("ReceiveReaction", (messageId, emoji) =>
                    {
                        AddLocalReaction(messageId, emoji);
                        ReactionAdded?.Invoke(messageId, emoji);
                    });

                    _hubConnection.On<string>("ReceiveError", (error) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"[SignalR] Error Received: {error}");
                        ErrorReceived?.Invoke(error);
                    });

                    _hubConnection.Closed += (error) =>
                    {
                        StopKeepAlive();
                        ConnectionStatusChanged?.Invoke(false);
                        return Task.CompletedTask;
                    };

                    _hubConnection.Reconnecting += (error) =>
                    {
                        StopKeepAlive();
                        ConnectionStatusChanged?.Invoke(false);
                        return Task.CompletedTask;
                    };

                    _hubConnection.Reconnected += async (connectionId) =>
                    {
                        ConnectionStatusChanged?.Invoke(true);
                        StartKeepAlive();
                        if (!string.IsNullOrEmpty(_lastRegisteredName))
                        {
                            await RegisterUserAsync(_lastRegisteredName);
                        }
                    };
                }

                await _hubConnection.StartAsync();
                System.Diagnostics.Debug.WriteLine("[SignalR] Connection Started Successfully.");
                ConnectionStatusChanged?.Invoke(true);
                StartKeepAlive();
                
                if (!string.IsNullOrEmpty(_lastRegisteredName))
                {
                    await RegisterUserAsync(_lastRegisteredName);
                }

                await ProcessPendingMessagesAsync();
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Connection Error: {ex.Message}");
                ConnectionStatusChanged?.Invoke(false);
                return ex.Message;
            }
            finally
            {
                IsWakingUp = false;
            }
        }

        private async Task ProcessPendingMessagesAsync()
        {
            if (!IsConnected) return;
            while (_pendingOutgoingMessages.TryDequeue(out var pending))
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Sending pending message to: {pending.Recipient}");
                await SendPrivateMessageAsync(pending.Recipient, pending.Message);
            }
        }

        // Register user in the Hub (CRITICAL: must be called after ConnectAsync)
        public async Task RegisterUserAsync(string userName)
        {
            if (_hubConnection == null || !IsConnected) 
            {
                System.Diagnostics.Debug.WriteLine("[SignalR] Cannot register user: Not connected.");
                return;
            }
            _lastRegisteredName = userName;
            System.Diagnostics.Debug.WriteLine($"[SignalR] Registering User: {userName}");
            await _hubConnection.SendAsync("RegisterUser", userName);
        }

        private void UpdateOnlineUsers(List<string> users)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                OnlineUsers.Clear();
                foreach (var user in users)
                {
                    var trimmed = user?.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !OnlineUsers.Contains(trimmed))
                        OnlineUsers.Add(trimmed);
                }
            });
        }

        private void AddOnlineUser(string userName)
        {
            var trimmed = userName?.Trim();
            if (string.IsNullOrEmpty(trimmed)) return;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (!OnlineUsers.Contains(trimmed))
                    OnlineUsers.Add(trimmed);
            });
        }

        private void RemoveOnlineUser(string userName)
        {
            var trimmed = userName?.Trim();
            if (string.IsNullOrEmpty(trimmed)) return;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                OnlineUsers.Remove(trimmed);
            });
        }

        public bool IsMe(string? name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            var trimmedName = name.Trim();
            
            var myFullName = (_currentUserService.CurrentUserFullName ?? "").Trim();
            var myUserName = (_currentUserService.CurrentUserName ?? "").Trim();
            
            bool result = string.Equals(trimmedName, myFullName, StringComparison.OrdinalIgnoreCase) || 
                          string.Equals(trimmedName, myUserName, StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(trimmedName, _lastRegisteredName, StringComparison.OrdinalIgnoreCase);

            // SPECIAL CASE: If name contains both, check subsets
            if (!result && !string.IsNullOrEmpty(myFullName))
            {
                result = myFullName.Contains(trimmedName, StringComparison.OrdinalIgnoreCase) || 
                         trimmedName.Contains(myFullName, StringComparison.OrdinalIgnoreCase);
            }

            System.Diagnostics.Debug.WriteLine($"[SignalR] IsMe('{trimmedName}'): FullName='{myFullName}', UserName='{myUserName}', RegisteredAs='{_lastRegisteredName}' => Result={result}");
            return result;
        }

        private void HandleIncomingMessage(string sender, string recipient, string message, DateTime timestamp, string messageId)
        {
            try 
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] HandleIncomingMessage: sender='{sender}', recipient='{recipient}', id='{messageId}'");
                
                bool isMe = IsMe(sender);
                
                // The partner is the other person in the conversation
                string partnerName = isMe ? recipient : sender;
                
                System.Diagnostics.Debug.WriteLine($"[SignalR] partnerName calculated as: '{partnerName}' (isMe={isMe})");
                
                if (string.IsNullOrEmpty(partnerName)) return;

                var msg = new PrivateChatMessageViewModel
                {
                    Id = messageId,
                    Sender = sender,
                    Message = message,
                    Timestamp = timestamp,
                    IsMe = isMe,
                    Status = MessageStatus.Sent // Default status
                };

                // Add to rocket lookup
                _allMessagesById[messageId] = msg;

                System.Diagnostics.Debug.WriteLine($"[SignalR] Storing message in Conversations['{partnerName}']");
                AddMessageToConversation(partnerName, msg);
                
                // NOTIFY UI: Pass BOTH sender and recipient so the UI can decide if it's relevant
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    if (!isMe)
                    {
                        TotalUnreadCount++;
                        PulseFab?.Invoke();
                    }
                    System.Diagnostics.Debug.WriteLine($"[SignalR] Invoking PrivateMessageReceived event for {messageId}");
                    PrivateMessageReceived?.Invoke(sender, recipient, message, timestamp, messageId);
                });
            }
            catch (Exception ex)
            {
                 System.Diagnostics.Debug.WriteLine($"[SignalR] Error handling message: {ex}");
            }
        }

        private void UpdateLocalMessageStatus(string messageId, MessageStatus status)
        {
            if (_allMessagesById.TryGetValue(messageId, out var msg))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    msg.Status = status;
                });
            }
        }

        private void AddLocalReaction(string messageId, string emoji)
        {
            if (_allMessagesById.TryGetValue(messageId, out var msg))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Simple space-separated reactions
                    if (string.IsNullOrEmpty(msg.Reactions))
                        msg.Reactions = emoji;
                    else if (!msg.Reactions.Contains(emoji))
                        msg.Reactions += " " + emoji;
                });
            }
        }

        public void AddMessageToConversation(string partnerName, PrivateChatMessageViewModel msg)
        {
            var conversation = Conversations.GetOrAdd(partnerName, _ => 
            {
                var coll = new ObservableCollection<PrivateChatMessageViewModel>();
                return coll;
            });

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                conversation.Add(msg);
                if (conversation.Count > MaxMessagesPerConversation)
                {
                    conversation.RemoveAt(0);
                }
            });
        }

        public void MarkUserAsUnread(string userName)
        {
            if (string.IsNullOrEmpty(userName)) return;
            lock (_usersWithUnreadMessages)
            {
                if (_usersWithUnreadMessages.Add(userName))
                {
                    System.Diagnostics.Debug.WriteLine($"[SignalR] Marked '{userName}' as having unread messages.");
                    System.Windows.Application.Current.Dispatcher.Invoke(() => UnreadMessagesChanged?.Invoke());
                }
            }
        }

        public void ClearUnreadForUser(string userName)
        {
            if (string.IsNullOrEmpty(userName)) return;
            lock (_usersWithUnreadMessages)
            {
                if (_usersWithUnreadMessages.Remove(userName))
                {
                    System.Diagnostics.Debug.WriteLine($"[SignalR] Cleared unread status for '{userName}'.");
                    System.Windows.Application.Current.Dispatcher.Invoke(() => UnreadMessagesChanged?.Invoke());
                }
            }
        }

        public void ClearTotalUnreadCount()
        {
            TotalUnreadCount = 0;
        }

        public async Task MarkMessagesAsReadAsync(string partnerName)
        {
            if (string.IsNullOrEmpty(partnerName)) return;
            
            if (Conversations.TryGetValue(partnerName, out var messages))
            {
                foreach (var msg in messages.Where(m => !m.IsMe && m.Status != MessageStatus.Read))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => msg.Status = MessageStatus.Read);
                    await UpdateMessageStatusAsync(partnerName, msg.Id, MessageStatus.Read);
                }
            }
        }

        public bool HasUnreadMessages(string userName)
        {
            if (string.IsNullOrEmpty(userName)) return false;
            lock (_usersWithUnreadMessages)
            {
                return _usersWithUnreadMessages.Contains(userName);
            }
        }

        // Send Private Message
        public async Task SendPrivateMessageAsync(string recipientUserName, string message, string? messageId = null)
        {
            if (string.IsNullOrEmpty(messageId)) messageId = Guid.NewGuid().ToString();

            if (!IsConnected)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Disconnected. Queuing message for: {recipientUserName}");
                _pendingOutgoingMessages.Enqueue(new PendingMessage { Recipient = recipientUserName, Message = message });
                _ = ConnectAsync(); // Background wake-up
                return;
            }
            
            try 
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Calling Hub.SendPrivateMessage to {recipientUserName}");
                // Send with OLD signature (2 params) for compatibility with hosted server
                await _hubConnection!.SendAsync("SendPrivateMessage", recipientUserName, message);
                System.Diagnostics.Debug.WriteLine($"[SignalR] Hub.SendPrivateMessage call completed.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Send failed: {ex.Message}");
                // If send fails, queue it
                _pendingOutgoingMessages.Enqueue(new PendingMessage { Recipient = recipientUserName, Message = message });
            }
        }

        public async Task UpdateMessageStatusAsync(string recipientUserName, string messageId, MessageStatus status)
        {
            if (!IsConnected) return;
            try
            {
                await _hubConnection!.SendAsync("UpdateMessageStatus", recipientUserName, messageId, status);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Error updating status: {ex.Message}");
            }
        }

        public async Task AddReactionAsync(string recipientUserName, string messageId, string emoji)
        {
            if (!IsConnected) return;
            try
            {
                await _hubConnection!.SendAsync("AddReaction", recipientUserName, messageId, emoji);
                // Also update locally for immediate feedback
                AddLocalReaction(messageId, emoji);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Error adding reaction: {ex.Message}");
            }
        }

        // Request updated online users list from server
        public async Task RequestOnlineUsersAsync()
        {
            if (_hubConnection == null || !IsConnected) return;
            System.Diagnostics.Debug.WriteLine("[SignalR] Manually requesting online users list...");
            await _hubConnection.SendAsync("RequestOnlineUsers");
        }

        // Group Chat (kept for backward compatibility)
        public async Task SendMessageAsync(string user, string message)
        {
            if (_hubConnection == null || !IsConnected) return;
            await _hubConnection.SendAsync("SendMessage", user, message);
        }

        // Keep Alive mechanism for Render
        private void StartKeepAlive()
        {
            _keepAliveTimer = new System.Timers.Timer(15000); // 15 seconds
            _keepAliveTimer.Elapsed += async (sender, e) =>
            {
                if (IsConnected)
                {
                    try
                    {
                        // Send a ping to keep the connection alive
                        await _hubConnection!.SendAsync("KeepAlive");
                    }
                    catch
                    {
                        // Connection might be lost, timer will stop on disconnect event
                    }
                }
            };
            _keepAliveTimer.Start();
        }

        private void StopKeepAlive()
        {
            _keepAliveTimer?.Stop();
            _keepAliveTimer?.Dispose();
            _keepAliveTimer = null;
        }

        // Helper to get current user (to be called from outside)
        private async Task<string?> GetCurrentUserNameAsync()
        {
            // This will be set by the ViewModel when calling RegisterUserAsync
            // For now, return null - the ViewModel will handle re-registration
            return await Task.FromResult<string?>(null);
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection != null)
            {
                StopKeepAlive();
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }
    }
}
