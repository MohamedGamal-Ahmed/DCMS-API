using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Application.Interfaces;

namespace DCMS.Infrastructure.Interceptors;

/// <summary>
/// Interceptor that creates notifications for important database operations
/// </summary>
public class NotificationInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public NotificationInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CreateNotifications(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CreateNotifications(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CreateNotifications(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added ||
                       e.State == EntityState.Modified)
            .Where(e => !(e.Entity is Notification || e.Entity is AuditLog))
            .Where(e => ShouldNotify(e))
            .ToList();

        foreach (var entry in entries)
        {
            try
            {
                var message = GenerateNotificationMessage(entry);
                var entityType = entry.Entity.GetType().Name;
                var entityId = GetEntityId(entry);

                // Get all active users to notify
                var dbContext = context as Infrastructure.Data.DCMSDbContext;
                if (dbContext == null) continue;

                var users = dbContext.Users.Where(u => u.IsActive).ToList();

                foreach (var user in users)
                {
                    var notification = new Notification
                    {
                        UserId = user.Id,
                        Message = message,
                        RelatedRecordId = $"{entityType}:{entityId}",
                        Type = entry.State == EntityState.Added ? NotificationType.Success : NotificationType.Info,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Add(notification);
                }
            }
            catch
            {
                // Don't fail if notification creation fails
            }
        }
    }

    private static bool ShouldNotify(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var entityType = entry.Entity.GetType().Name;
        // Only notify for important entities
        return entityType is "Inbound" or "Outbound" or "Meeting";
    }

    private static string GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
        return idProperty?.CurrentValue?.ToString() ?? "Unknown";
    }

    private string GenerateNotificationMessage(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var entityType = entry.Entity.GetType().Name;
        var action = entry.State == EntityState.Added ? "أضاف" : "عدل";
        var userName = _currentUserService.CurrentUserName ?? "مستخدم";

        // Try to get a meaningful name from the entity
        var nameProperty = entry.Properties.FirstOrDefault(p =>
            p.Metadata.Name == "Subject" ||
            p.Metadata.Name == "Name" ||
            p.Metadata.Name == "Title");

        var name = nameProperty?.CurrentValue?.ToString() ?? $"#{GetEntityId(entry)}";

        var arabicEntityName = entityType switch
        {
            "Inbound" => "موضوع وارد",
            "Outbound" => "موضوع صادر",
            "Meeting" => "اجتماع",
            _ => entityType
        };

        return $"{userName} {action} {arabicEntityName}: {name}";
    }
}
