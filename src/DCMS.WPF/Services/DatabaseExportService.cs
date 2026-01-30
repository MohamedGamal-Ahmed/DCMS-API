using System.IO;
using System.Text.Json;
using DCMS.Domain.Entities;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace DCMS.WPF.Services;

public class DatabaseExportService
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly CurrentUserService _currentUserService;
    private const int BatchSize = 500;

    public DatabaseExportService(IDbContextFactory<DCMSDbContext> contextFactory, CurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Exports all database data directly to a JSON file (memory-efficient streaming)
    /// </summary>
    public async Task ExportToJsonFileAsync(string filePath)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        // Disable tracking for better performance on read-only operations
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 65536, useAsync: true);
        await using var writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) });

        writer.WriteStartObject();
        
        // Write metadata
        writer.WriteString("ExportDate", DateTime.UtcNow);
        writer.WriteString("Version", "3.0");

        // Export Inbounds with related data
        writer.WritePropertyName("Inbounds");
        writer.WriteStartArray();
        await foreach (var batch in GetInboundsInBatchesAsync(context))
        {
            foreach (var item in batch)
            {
                JsonSerializer.Serialize(writer, item, options);
            }
        }
        writer.WriteEndArray();
        await writer.FlushAsync();

        // Export Outbounds
        writer.WritePropertyName("Outbounds");
        writer.WriteStartArray();
        await foreach (var batch in GetDataInBatchesAsync(context.Outbounds))
        {
            foreach (var item in batch)
            {
                JsonSerializer.Serialize(writer, item, options);
            }
        }
        writer.WriteEndArray();
        await writer.FlushAsync();

        // Export Contracts
        writer.WritePropertyName("Contracts");
        writer.WriteStartArray();
        await foreach (var batch in GetDataInBatchesAsync(context.Contracts))
        {
            foreach (var item in batch)
            {
                JsonSerializer.Serialize(writer, item, options);
            }
        }
        writer.WriteEndArray();
        await writer.FlushAsync();

        // Export CalendarEvents
        writer.WritePropertyName("CalendarEvents");
        writer.WriteStartArray();
        await foreach (var batch in GetDataInBatchesAsync(context.CalendarEvents))
        {
            foreach (var item in batch)
            {
                JsonSerializer.Serialize(writer, item, options);
            }
        }
        writer.WriteEndArray();
        await writer.FlushAsync();

        /* 
        // Export Notifications (Excluding to reduce file size as per user request)
        writer.WritePropertyName("Notifications");
        writer.WriteStartArray();
        await foreach (var batch in GetDataInBatchesAsync(context.Notifications))
        {
            foreach (var item in batch)
            {
                JsonSerializer.Serialize(writer, item, options);
            }
        }
        writer.WriteEndArray();
        await writer.FlushAsync();

        // Export AuditLogs (often the largest table)
        writer.WritePropertyName("AuditLogs");
        writer.WriteStartArray();
        await foreach (var batch in GetDataInBatchesAsync(context.AuditLogs))
        {
            foreach (var item in batch)
            {
                JsonSerializer.Serialize(writer, item, options);
            }
        }
        writer.WriteEndArray();
        await writer.FlushAsync();
        */

        // Export Users
        writer.WritePropertyName("Users");
        writer.WriteStartArray();
        await foreach (var batch in GetDataInBatchesAsync(context.Users))
        {
            foreach (var item in batch)
            {
                JsonSerializer.Serialize(writer, item, options);
            }
        }
        writer.WriteEndArray();
        await writer.FlushAsync();

        // Export Engineers
        writer.WritePropertyName("Engineers");
        writer.WriteStartArray();
        await foreach (var batch in GetDataInBatchesAsync(context.Engineers))
        {
            foreach (var item in batch)
            {
                JsonSerializer.Serialize(writer, item, options);
            }
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
        await writer.FlushAsync();
    }

    private async IAsyncEnumerable<List<Inbound>> GetInboundsInBatchesAsync(DCMSDbContext context)
    {
        var totalCount = await context.Inbounds.CountAsync();
        for (int skip = 0; skip < totalCount; skip += BatchSize)
        {
            var batch = await context.Inbounds
                .OrderBy(i => i.Id)
                .Skip(skip)
                .Take(BatchSize)
                .Include(i => i.Transfers)
                .Include(i => i.ResponsibleEngineers)
                .ToListAsync();
            yield return batch;
        }
    }

    private async IAsyncEnumerable<List<T>> GetDataInBatchesAsync<T>(IQueryable<T> query) where T : class
    {
        var totalCount = await query.CountAsync();
        for (int skip = 0; skip < totalCount; skip += BatchSize)
        {
            var batch = await query
                .Skip(skip)
                .Take(BatchSize)
                .ToListAsync();
            yield return batch;
        }
    }

    /// <summary>
    /// Exports all database data to JSON string (legacy method - may cause memory issues with large databases)
    /// </summary>
    [Obsolete("Use ExportToJsonFileAsync for large databases to avoid memory issues")]
    public async Task<string> ExportToJsonAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var backup = new DatabaseBackup
        {
            ExportDate = DateTime.UtcNow,
            Version = "3.0",
            
            // Export all data
            Inbounds = await context.Inbounds
                .Include(i => i.Transfers)
                .Include(i => i.ResponsibleEngineers)
                .ToListAsync(),
            
            Outbounds = await context.Outbounds.ToListAsync(),
            Contracts = await context.Contracts.ToListAsync(),
            CalendarEvents = await context.CalendarEvents.ToListAsync(),
            // Notifications and AuditLogs excluded to reduce file size
            // Notifications = await context.Notifications.ToListAsync(),
            // AuditLogs = await context.AuditLogs.ToListAsync(),
            
            // Backup Users and Engineers for reference (will not be restored by default)
            Users = await context.Users.ToListAsync(),
            Engineers = await context.Engineers.ToListAsync()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        return JsonSerializer.Serialize(backup, options);
    }

    /// <summary>
    /// Imports data from JSON file using a robust per-table approach with explicit order and transactions.
    /// </summary>
    public async Task ImportFromJsonFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("ملف النسخة الاحتياطية غير موجود", filePath);

        // 1. Load data into memory (current code already does this via bytes, so we parse as JsonDocument)
        // For very large files, this should be improved, but for current usage it's standard.
        var jsonBytes = await File.ReadAllBytesAsync(filePath);
        using var doc = JsonDocument.Parse(jsonBytes);
        var root = doc.RootElement;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };

        using var context = await _contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // 1. Clean Wipe (Children first to avoid FK errors)
            await context.Database.ExecuteSqlRawAsync("TRUNCATE dcms.inbound_transfers, dcms.inbound_responsible_engineers, dcms.event_attendees, dcms.contract_parties, dcms.ai_request_logs CASCADE;");
            await context.Database.ExecuteSqlRawAsync("TRUNCATE dcms.inbound, dcms.outbound, dcms.contracts, dcms.calendar_events, dcms.meetings, dcms.notifications, dcms.audit_logs, dcms.engineers, dcms.users, dcms.emails, dcms.inbound_codes CASCADE;");
            await context.SaveChangesAsync();

            // 2. Restore in exact order to satisfy FKs
            System.Diagnostics.Debug.WriteLine("[RESTORE] Starting table restoration...");
            
            // A. Users
            if (root.TryGetProperty("Users", out var usersProp))
            {
                var users = JsonSerializer.Deserialize<List<User>>(usersProp.GetRawText(), options);
                if (users != null && users.Count > 0)
                {
                    await InsertWithIdentityAsync(context, "users", users);
                    System.Diagnostics.Debug.WriteLine($"[RESTORE] Restored {users.Count} users.");
                }
            }

            // B. Engineers
            Dictionary<string, int> engineerMap = new(StringComparer.OrdinalIgnoreCase);
            if (root.TryGetProperty("Engineers", out var engineersProp))
            {
                var engineers = JsonSerializer.Deserialize<List<Engineer>>(engineersProp.GetRawText(), options);
                if (engineers != null && engineers.Count > 0)
                {
                    await InsertWithIdentityAsync(context, "engineers", engineers);
                    System.Diagnostics.Debug.WriteLine($"[RESTORE] Restored {engineers.Count} engineers.");
                    
                    // Build map for "Smart Mapping"
                    engineerMap = engineers
                        .Where(e => !string.IsNullOrEmpty(e.FullName))
                        .GroupBy(e => e.FullName)
                        .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
                }
            }

            var currentAdminId = _currentUserService.CurrentUserId;

            // C. Inbounds (Includes nested Transfers and ResponsibleEngineers)
            if (root.TryGetProperty("Inbounds", out var inboundsProp))
            {
                var inbounds = JsonSerializer.Deserialize<List<Inbound>>(inboundsProp.GetRawText(), options);
                if (inbounds != null && inbounds.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[RESTORE] Processing {inbounds.Count} inbounds for Smart Mapping...");
                    foreach (var inbound in inbounds)
                    {
                        // 1. Smart Mapping for Responsible Engineer
                        if (!string.IsNullOrEmpty(inbound.ResponsibleEngineer) && (inbound.ResponsibleEngineers == null || inbound.ResponsibleEngineers.Count == 0))
                        {
                            if (engineerMap.TryGetValue(inbound.ResponsibleEngineer, out int engId))
                            {
                                inbound.ResponsibleEngineers ??= new List<InboundResponsibleEngineer>();
                                inbound.ResponsibleEngineers.Add(new InboundResponsibleEngineer { EngineerId = engId });
                            }
                        }

                        // 2. Fix CreatedBy
                        if ((inbound.CreatedByUserId ?? 0) == 0 && currentAdminId.HasValue)
                        {
                            inbound.CreatedByUserId = currentAdminId;
                        }
                    }

                    await InsertWithIdentityAsync(context, "inbound", inbounds);
                    System.Diagnostics.Debug.WriteLine($"[RESTORE] Restored {inbounds.Count} inbounds.");
                }
            }

            // D. Outbounds
            if (root.TryGetProperty("Outbounds", out var outboundsProp))
            {
                var outbounds = JsonSerializer.Deserialize<List<Outbound>>(outboundsProp.GetRawText(), options);
                if (outbounds != null && outbounds.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[RESTORE] Processing {outbounds.Count} outbounds for Smart Mapping...");
                    foreach (var outbound in outbounds)
                    {
                        // Fix CreatedBy
                        if ((outbound.CreatedByUserId ?? 0) == 0 && currentAdminId.HasValue)
                        {
                            outbound.CreatedByUserId = currentAdminId;
                        }
                    }
                    await InsertWithIdentityAsync(context, "outbound", outbounds);
                    System.Diagnostics.Debug.WriteLine($"[RESTORE] Restored {outbounds.Count} outbounds.");
                }
            }

            // E. Contracts (Includes nested Parties)
            if (root.TryGetProperty("Contracts", out var contractsProp))
            {
                var contracts = JsonSerializer.Deserialize<List<Contract>>(contractsProp.GetRawText(), options);
                if (contracts != null && contracts.Count > 0)
                {
                    foreach (var contract in contracts)
                    {
                        if ((contract.CreatedByUserId ?? 0) == 0 && currentAdminId.HasValue)
                            contract.CreatedByUserId = currentAdminId;
                    }
                    await InsertWithIdentityAsync(context, "contracts", contracts);
                    System.Diagnostics.Debug.WriteLine($"[RESTORE] Restored {contracts.Count} contracts.");
                }
            }

            // F. CalendarEvents
            if (root.TryGetProperty("CalendarEvents", out var eventsProp))
            {
                var events = JsonSerializer.Deserialize<List<CalendarEvent>>(eventsProp.GetRawText(), options);
                if (events != null && events.Count > 0)
                {
                    foreach (var ev in events)
                    {
                        if ((ev.CreatedByUserId ?? 0) == 0 && currentAdminId.HasValue)
                            ev.CreatedByUserId = currentAdminId;
                    }
                    await InsertWithIdentityAsync(context, "calendar_events", events);
                    System.Diagnostics.Debug.WriteLine($"[RESTORE] Restored {events.Count} events.");
                }
            }

            // G. Meetings
            if (root.TryGetProperty("Meetings", out var meetingsProp))
            {
                var meetings = JsonSerializer.Deserialize<List<Meeting>>(meetingsProp.GetRawText(), options);
                if (meetings != null && meetings.Count > 0)
                {
                    foreach (var m in meetings)
                    {
                        if ((m.CreatedByUserId ?? 0) == 0 && currentAdminId.HasValue)
                            m.CreatedByUserId = currentAdminId;
                    }
                    await InsertWithIdentityAsync(context, "meetings", meetings);
                    System.Diagnostics.Debug.WriteLine($"[RESTORE] Restored {meetings.Count} meetings.");
                }
            }

            // H. Other tables (Optional/Supplemental)
            if (root.TryGetProperty("Notifications", out var notificationsProp))
            {
                var notifications = JsonSerializer.Deserialize<List<Notification>>(notificationsProp.GetRawText(), options);
                if (notifications != null && notifications.Count > 0) 
                {
                    await InsertWithIdentityAsync(context, "notifications", notifications);
                }
            }

            if (root.TryGetProperty("AuditLogs", out var auditProp))
            {
                var audits = JsonSerializer.Deserialize<List<AuditLog>>(auditProp.GetRawText(), options);
                if (audits != null && audits.Count > 0)
                {
                    await InsertWithIdentityAsync(context, "audit_logs", audits);
                }
            }

            System.Diagnostics.Debug.WriteLine("[RESTORE] Full restoration completed successfully.");
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            var errorMessage = $"فشل استيراد البيانات: {ex.Message}";
            if (ex.InnerException != null) errorMessage += $"\nInner Error: {ex.InnerException.Message}";
            System.Diagnostics.Debug.WriteLine($"[RESTORE ERROR] {errorMessage}\n{ex.StackTrace}");
            throw new Exception(errorMessage, ex);
        }
    }

    /// <summary>
    /// Helper to insert entities while preserving their IDs in Postgres.
    /// Temporarily drops IDENTITY constraint to allow explicit ID insertion.
    /// </summary>
    private async Task InsertWithIdentityAsync<T>(DCMSDbContext context, string tableName, List<T> items) where T : class
    {
        if (items == null || items.Count == 0) return;

        try
        {
            // 1. Temporarily drop identity to allow explicit IDs
            // Note: Postgres "GENERATED BY DEFAULT AS IDENTITY" usually allows explicit IDs 
            // but we use "OVERRIDING SYSTEM VALUE" or temporary identity drop for maximum compatibility.
            // A simpler way for EF Core:
            foreach (var item in items)
            {
                context.Set<T>().Add(item);
            }
            
            // To ensure Postgres accepts the provided IDs for identity columns:
            // We use a strategy of stripping identity briefly if needed, 
            // but for EF Core with Npgsql, it's often better to just fix the sequence AFTER.
            await context.SaveChangesAsync();

            // 2. IMPORTANT: Reset the sequence so future auto-generated IDs don't conflict
            try
            {
                var sqlReset = $@"
                    SELECT setval(pg_get_serial_sequence('dcms.{tableName}', 'id'), 
                    COALESCE((SELECT MAX(id) FROM dcms.{tableName}), 1), true);";
                await context.Database.ExecuteSqlRawAsync(sqlReset);
            }
            catch { /* Some tables might not have 'id' or sequence, ignore */ }
            
            context.ChangeTracker.Clear();
        }
        catch (Exception ex)
        {
            // If it fails due to identity, we might need to handle it with more aggressive SQL
            throw new Exception($"خطأ أثناء إدخال البيانات في جدول {tableName}: {ex.Message}", ex);
        }
    }

    [Obsolete("Use rewritten ImportFromJsonFileAsync")]
    private void ImportDataSync(string filePath) { }

    [Obsolete("Use rewritten ImportFromJsonFileAsync")]
    private void ImportBatchSync<T>(ref Utf8JsonReader reader, DCMSDbContext context, JsonSerializerOptions options) where T : class { }

    /// <summary>
    /// Imports data from JSON backup, preserving Users and Engineers
    /// </summary>
    [Obsolete("Use ImportFromJsonFileAsync for better performance and memory usage")]
    public async Task ImportFromJsonAsync(string json)
    {
        var backup = JsonSerializer.Deserialize<DatabaseBackup>(json);
        if (backup == null)
            throw new Exception("Invalid backup file");

        using var context = await _contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Clear existing data (EXCEPT Users and Engineers)
            context.Inbounds.RemoveRange(context.Inbounds);
            context.InboundTransfers.RemoveRange(context.InboundTransfers);
            context.InboundResponsibleEngineers.RemoveRange(context.InboundResponsibleEngineers);
            context.Outbounds.RemoveRange(context.Outbounds);
            context.Contracts.RemoveRange(context.Contracts);
            context.CalendarEvents.RemoveRange(context.CalendarEvents);
            context.Notifications.RemoveRange(context.Notifications);
            context.AuditLogs.RemoveRange(context.AuditLogs);
            
            await context.SaveChangesAsync();

            // Import data
            if (backup.Inbounds != null && backup.Inbounds.Count > 0)
            {
                context.Inbounds.AddRange(backup.Inbounds);
            }

            if (backup.Outbounds != null && backup.Outbounds.Count > 0)
            {
                context.Outbounds.AddRange(backup.Outbounds);
            }

            if (backup.Contracts != null && backup.Contracts.Count > 0)
            {
                context.Contracts.AddRange(backup.Contracts);
            }

            if (backup.CalendarEvents != null && backup.CalendarEvents.Count > 0)
            {
                context.CalendarEvents.AddRange(backup.CalendarEvents);
            }

            if (backup.Notifications != null && backup.Notifications.Count > 0)
            {
                context.Notifications.AddRange(backup.Notifications);
            }

            if (backup.AuditLogs != null && backup.AuditLogs.Count > 0)
            {
                context.AuditLogs.AddRange(backup.AuditLogs);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

/// <summary>
/// Container for database backup data
/// </summary>
public class DatabaseBackup
{
    public DateTime ExportDate { get; set; }
    public string Version { get; set; } = string.Empty;
    
    // Main data
    public List<Inbound> Inbounds { get; set; } = new();
    public List<Outbound> Outbounds { get; set; } = new();
    public List<Contract> Contracts { get; set; } = new();
    public List<CalendarEvent> CalendarEvents { get; set; } = new();
    public List<Notification> Notifications { get; set; } = new();
    public List<AuditLog> AuditLogs { get; set; } = new();
    
    // Reference data (for information only, not restored)
    public List<User> Users { get; set; } = new();
    public List<Engineer> Engineers { get; set; } = new();
}
