using System.IO;
using System.Text.Json;
using DCMS.Domain.Entities;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DCMS.WPF.Services;

public class DatabaseExportService
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private const int BatchSize = 500; // Process data in batches to reduce memory usage

    public DatabaseExportService(IDbContextFactory<DCMSDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
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
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 65536, useAsync: true);
        await using var writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions { Indented = true });

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

        // Export Notifications
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
            Notifications = await context.Notifications.ToListAsync(),
            AuditLogs = await context.AuditLogs.ToListAsync(),
            
            // Backup Users and Engineers for reference (will not be restored by default)
            Users = await context.Users.ToListAsync(),
            Engineers = await context.Engineers.ToListAsync()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };

        return JsonSerializer.Serialize(backup, options);
    }

    /// <summary>
    /// Imports data from JSON file using streaming to minimize memory usage
    /// </summary>
    public async Task ImportFromJsonFileAsync(string filePath)
    {
        // 1. Efficient Cleanup using ExecuteDelete
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            // Delete children first to avoid FK constraints
            await context.InboundTransfers.ExecuteDeleteAsync();
            await context.InboundResponsibleEngineers.ExecuteDeleteAsync();
            
            // Delete main tables
            await context.Inbounds.ExecuteDeleteAsync();
            await context.Outbounds.ExecuteDeleteAsync();
            await context.Contracts.ExecuteDeleteAsync();
            await context.CalendarEvents.ExecuteDeleteAsync();
            await context.Notifications.ExecuteDeleteAsync();
            await context.AuditLogs.ExecuteDeleteAsync();
        }

        // 2. Stream Data Import (CPU-bound work moved to background thread)
        // We use synchronous IO/Deserialization inside Task.Run because Utf8JsonReader
        // is a ref struct and cannot be used in async methods.
        await Task.Run(() => ImportDataSync(filePath));
    }

    private void ImportDataSync(string filePath)
    {
        var fileData = File.ReadAllBytes(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var reader = new Utf8JsonReader(fileData);

        // Advance to StartObject
        while (reader.Read() && reader.TokenType != JsonTokenType.StartObject) { }

        using var context = _contextFactory.CreateDbContext();
        // Use a transaction for data integrity
        using var transaction = context.Database.BeginTransaction();

        try
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string? propertyName = reader.GetString();
                    
                    if (string.Equals(propertyName, "Inbounds", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ImportBatchSync<Inbound>(ref reader, context, options);
                    }
                    else if (string.Equals(propertyName, "Outbounds", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ImportBatchSync<Outbound>(ref reader, context, options);
                    }
                    else if (string.Equals(propertyName, "Contracts", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ImportBatchSync<Contract>(ref reader, context, options);
                    }
                    else if (string.Equals(propertyName, "CalendarEvents", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ImportBatchSync<CalendarEvent>(ref reader, context, options);
                    }
                    else if (string.Equals(propertyName, "Notifications", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ImportBatchSync<Notification>(ref reader, context, options);
                    }
                    else if (string.Equals(propertyName, "AuditLogs", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ImportBatchSync<AuditLog>(ref reader, context, options);
                    }
                    else
                    {
                        // Skip other properties/arrays
                        reader.Skip();
                    }
                }
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private void ImportBatchSync<T>(ref Utf8JsonReader reader, DCMSDbContext context, JsonSerializerOptions options) where T : class
    {
        // Advance to StartArray
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            // If checking StartArray failed, it might be that we read property name and the next token token is the array
            // But usually after PropertyName, next Read() gives the value.
            return;
        }

        int count = 0;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            // Deserialize single item
            var item = JsonSerializer.Deserialize<T>(ref reader, options);
            if (item != null)
            {
                context.Set<T>().Add(item);
                count++;

                // Save in batches
                if (count % BatchSize == 0)
                {
                    context.SaveChanges();
                    context.ChangeTracker.Clear();
                }
            }
        }

        // Save remaining
        if (count % BatchSize != 0)
        {
            context.SaveChanges();
            context.ChangeTracker.Clear();
        }
    }

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
