using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Application.Interfaces;
using System.Text.Json;

namespace DCMS.Infrastructure.Interceptors;

/// <summary>
/// Interceptor that automatically logs all Create, Update, and Delete operations to the AuditLog table
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public AuditInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditLogs(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditLogs(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added ||
                       e.State == EntityState.Modified ||
                       e.State == EntityState.Deleted)
            .Where(e => !(e.Entity is AuditLog)) // Don't audit the audit log itself
            .ToList();

        foreach (var entry in entries)
        {
            var auditLog = new AuditLog
            {
                UserName = _currentUserService.CurrentUserName ?? "System",
                EntityType = entry.Entity.GetType().Name,
                EntityId = GetEntityId(entry),
                Timestamp = DateTime.UtcNow,
                Action = entry.State switch
                {
                    EntityState.Added => AuditActionType.Create,
                    EntityState.Modified => AuditActionType.Update,
                    EntityState.Deleted => AuditActionType.Delete,
                    _ => AuditActionType.Create
                },
                IPAddress = null, // Can be populated from HTTP context in future
                Description = GenerateDescription(entry)
            };

            // For Create operations, only store new values
            if (entry.State == EntityState.Added)
            {
                auditLog.NewValues = SerializeEntity(entry.CurrentValues.ToObject());
            }
            // For Update operations, store both old and new values
            else if (entry.State == EntityState.Modified)
            {
                auditLog.OldValues = SerializeEntity(entry.OriginalValues.ToObject());
                auditLog.NewValues = SerializeEntity(entry.CurrentValues.ToObject());
            }
            // For Delete operations, only store old values
            else if (entry.State == EntityState.Deleted)
            {
                auditLog.OldValues = SerializeEntity(entry.OriginalValues.ToObject());
            }

            context.Add(auditLog);
        }
    }

    private static string GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
        return idProperty?.CurrentValue?.ToString() ?? "Unknown";
    }

    private static string GenerateDescription(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var entityType = entry.Entity.GetType().Name;
        var action = entry.State switch
        {
            EntityState.Added => "تم إضافة",
            EntityState.Modified => "تم تعديل",
            EntityState.Deleted => "تم حذف",
            _ => "عملية على"
        };

        // Try to get a meaningful name from the entity
        var nameProperty = entry.Properties.FirstOrDefault(p =>
            p.Metadata.Name == "Subject" ||
            p.Metadata.Name == "Name" ||
            p.Metadata.Name == "Title");

        var name = nameProperty?.CurrentValue?.ToString() ?? $"#{GetEntityId(entry)}";

        return $"{action} {GetArabicEntityName(entityType)}: {name}";
    }

    private static string GetArabicEntityName(string entityType)
    {
        return entityType switch
        {
            "Inbound" => "موضوع وارد",
            "Outbound" => "موضوع صادر",
            "Meeting" => "اجتماع",
            "Engineer" => "مهندس",
            "User" => "مستخدم",
            _ => entityType
        };
    }

    private static string? SerializeEntity(object? entity)
    {
        if (entity == null) return null;

        try
        {
            return JsonSerializer.Serialize(entity, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch
        {
            return entity.ToString();
        }
    }
}
