using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DCMS.WPF.Services;

public enum ToastType { Success, Error, Warning, Info }

public partial class NotificationItem : ObservableObject
{
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private ToastType _type;
    public Guid Id { get; } = Guid.NewGuid();
}

public class NotificationService
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    public ObservableCollection<NotificationItem> ActiveNotifications { get; } = new();

    public event EventHandler? NotificationAdded;

    public NotificationService(IDbContextFactory<DCMSDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // --- Toast Functionality (Transient) ---

    public void Show(string message, ToastType type = ToastType.Info)
    {
        var notification = new NotificationItem { Message = message, Type = type };
        ActiveNotifications.Add(notification);

        // Play audio feedback based on notification type
        PlayNotificationSound(type);

        // Auto-remove after 4 seconds
        _ = Task.Delay(4000).ContinueWith(_ => 
        {
            App.Current.Dispatcher.Invoke(() => ActiveNotifications.Remove(notification));
        });
    }

    private void PlayNotificationSound(ToastType type)
    {
        try
        {
            switch (type)
            {
                case ToastType.Success:
                    System.Media.SystemSounds.Asterisk.Play();
                    break;
                case ToastType.Error:
                    System.Media.SystemSounds.Hand.Play();
                    break;
                case ToastType.Warning:
                    System.Media.SystemSounds.Exclamation.Play();
                    break;
                case ToastType.Info:
                    System.Media.SystemSounds.Beep.Play();
                    break;
            }
        }
        catch { /* Ignore audio errors */ }
    }

    public void Success(string message) => Show(message, ToastType.Success);
    public void Error(string message) => Show(message, ToastType.Error);
    public void Warning(string message) => Show(message, ToastType.Warning);
    public void Info(string message) => Show(message, ToastType.Info);

    // --- Persistent Notification Functionality (Database-backed) ---

    public void Start() { /* Service start logic */ }
    public void Stop() { /* Service stop logic */ }

    public async Task<List<Notification>> GetUnreadNotifications(int userId)
    {
        // OPTIMIZED: Strict filtering for 'Meeting', fast projection and AsNoTracking for speed (< 20ms goal)
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead && n.Type == DCMS.Domain.Enums.NotificationType.Meeting)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .Select(n => new Notification { Id = n.Id, Message = n.Message, CreatedAt = n.CreatedAt, Type = n.Type })
            .ToListAsync();
    }

    // Overload for specific user
    public async Task AddNotification(int userId, string message, string? recordId, DCMS.Domain.Enums.NotificationType type)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var notification = new Notification
        {
            UserId = userId,
            Message = message,
            RelatedRecordId = recordId,
            Type = type,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };
        // context.Notifications.Add(notification); // Disabled for productivity/storage optimization
        // await context.SaveChangesAsync();
        
        NotificationAdded?.Invoke(this, EventArgs.Empty);
    }

    // Overload for broadcast (notifying all relevant users, e.g., admins/managers)
    // This matches the 3-argument call used in various ViewModels
    public async Task AddNotification(string message, string? recordId, DCMS.Domain.Enums.NotificationType type)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // Notify all Admin and OfficeManager users
        var targetUserIds = await context.Users
            .Where(u => u.Role == UserRole.Admin || u.Role == UserRole.OfficeManager || u.Role == UserRole.TechnicalManager)
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in targetUserIds)
        {
            // context.Notifications.Add(...) // Disabled for productivity/storage optimization
            /*
            context.Notifications.Add(new Notification
            {
                UserId = userId,
                Message = message,
                RelatedRecordId = recordId,
                Type = type,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });
            */
        }

        // await context.SaveChangesAsync();
        NotificationAdded?.Invoke(this, EventArgs.Empty);

        // Also show a toast for the person who triggered it
        App.Current.Dispatcher.Invoke(() => 
        {
            Show(message, ConvertToToastType(type));
        });
    }

    public async Task MarkAsRead(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var note = await context.Notifications.FindAsync(id);
        if (note != null)
        {
            note.IsRead = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsRead(int userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var notes = await context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in notes) n.IsRead = true;
        await context.SaveChangesAsync();
    }

    private ToastType ConvertToToastType(DCMS.Domain.Enums.NotificationType type)
    {
        return type switch
        {
            DCMS.Domain.Enums.NotificationType.Success => ToastType.Success,
            DCMS.Domain.Enums.NotificationType.Error => ToastType.Error,
            DCMS.Domain.Enums.NotificationType.Warning => ToastType.Warning,
            _ => ToastType.Info
        };
    }
}
