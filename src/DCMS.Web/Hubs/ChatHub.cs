using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace DCMS.Web.Hubs;

public class ChatHub : Hub
{
    private static readonly Regex UrlRegex = new Regex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Dictionary<string, string> _connectedUsers = new Dictionary<string, string>();
    private static readonly object _lock = new object();
    private static readonly string ServerInstanceId = Guid.NewGuid().ToString().Substring(0, 8);

    // User Registration (called immediately after connection)
    public async Task RegisterUser(string userName)
    {
        List<string> onlineUsers;
        lock (_lock)
        {
            _connectedUsers[Context.ConnectionId] = userName;
            onlineUsers = _connectedUsers.Values.Distinct().ToList();
        }
        
        var log = $"[Hub:{ServerInstanceId}] User Registered: {userName} (ID: {Context.ConnectionId.Substring(0,5)}...). Total Online: {onlineUsers.Count}. List: [{string.Join(",", onlineUsers)}]";
        Console.WriteLine(log);
        
        // Push the ENTIRE online users list to EVERY connected client
        await Clients.All.SendAsync("ReceiveOnlineUsers", onlineUsers);
        
        // Also send UserConnected event for individual notification
        await Clients.All.SendAsync("UserConnected", userName);
    }

    // Manual request for online users
    public async Task RequestOnlineUsers()
    {
        List<string> onlineUsers;
        lock (_lock)
        {
            onlineUsers = _connectedUsers.Values.Distinct().ToList();
        }
        await Clients.Caller.SendAsync("ReceiveOnlineUsers", onlineUsers);
    }

    // KeepAlive ping from client to prevent Render timeout
    public Task KeepAlive()
    {
        return Task.CompletedTask;
    }

    // Send Private Message to specific user
    public async Task SendPrivateMessage(string recipientUserName, string message, string messageId)
    {
        // Security Check: Block URLs
        if (UrlRegex.IsMatch(message))
        {
            await Clients.Caller.SendAsync("ReceiveError", "⚠️ سياسة الأمان: غير مسموح بإرسال روابط خارجية أو ملفات. الدردشة مخصصة للنصوص فقط.");
            return;
        }

        bool isRegistered;
        string? senderName;
        lock (_lock)
        {
            isRegistered = _connectedUsers.TryGetValue(Context.ConnectionId, out senderName);
        }

        if (!isRegistered)
        {
            await Clients.Caller.SendAsync("ReceiveError", "خطأ: لم يتم تسجيل المستخدم. يرجى إعادة الاتصال.");
            return;
        }

        // Find the recipient's connection ID
        string? recipientConnectionId = null;
        lock (_lock)
        {
            recipientConnectionId = _connectedUsers.FirstOrDefault(x => x.Value == recipientUserName).Key;
        }

        if (string.IsNullOrEmpty(recipientConnectionId))
        {
            await Clients.Caller.SendAsync("ReceiveError", $"المستخدم '{recipientUserName}' غير متصل حالياً.");
            return;
        }

        var timestamp = DateTime.UtcNow;

        // Send to recipient
        await Clients.Client(recipientConnectionId).SendAsync("ReceivePrivateMessage", senderName, recipientUserName, message, timestamp, messageId);
        
        // Echo back to sender for their own UI
        await Clients.Caller.SendAsync("ReceivePrivateMessage", senderName, recipientUserName, message, timestamp, messageId);
    }

    public async Task UpdateMessageStatus(string recipientUserName, string messageId, int status)
    {
        string? recipientConnectionId;
        lock (_lock)
        {
            recipientConnectionId = _connectedUsers.FirstOrDefault(x => x.Value == recipientUserName).Key;
        }

        if (!string.IsNullOrEmpty(recipientConnectionId))
        {
            await Clients.Client(recipientConnectionId).SendAsync("ReceiveMessageStatus", messageId, status);
        }
    }

    public async Task AddReaction(string recipientUserName, string messageId, string emoji)
    {
        string? recipientConnectionId;
        lock (_lock)
        {
            recipientConnectionId = _connectedUsers.FirstOrDefault(x => x.Value == recipientUserName).Key;
        }

        if (!string.IsNullOrEmpty(recipientConnectionId))
        {
            await Clients.Client(recipientConnectionId).SendAsync("ReceiveReaction", messageId, emoji);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? userName = null;
        List<string> onlineUsers;
        lock (_lock)
        {
            if (_connectedUsers.TryGetValue(Context.ConnectionId, out userName))
            {
                _connectedUsers.Remove(Context.ConnectionId);
            }
            onlineUsers = _connectedUsers.Values.Distinct().ToList();
        }

        if (!string.IsNullOrEmpty(userName))
        {
            Console.WriteLine($"[ChatHub] User Disconnected: {userName}");
            // Notify all clients that user disconnected
            await Clients.All.SendAsync("UserDisconnected", userName);
            // Broadcast updated user list to ALL
            await Clients.All.SendAsync("ReceiveOnlineUsers", onlineUsers);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
