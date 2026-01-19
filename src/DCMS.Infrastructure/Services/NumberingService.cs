using Microsoft.EntityFrameworkCore;
using DCMS.Infrastructure.Data;
using Npgsql; // For NpgsqlException

namespace DCMS.Infrastructure.Services;

public class NumberingService
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;

    public NumberingService(IDbContextFactory<DCMSDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Generates the next subject number in YY-XXX format using Database Sequences.
    /// Handles concurrency and automatic yearly reset.
    /// </summary>
    public async Task<string> GenerateNextInboundNumberAsync()
    {
        return await GetNextSequenceNumberAsync("inbound", "inbound", "subject_number");
    }

    /// <summary>
    /// Generates the next subject number for Outbound correspondence using Database Sequences.
    /// </summary>
    public async Task<string> GenerateNextOutboundNumberAsync()
    {
        return await GetNextSequenceNumberAsync("outbound", "outbound", "subject_number");
    }

    private async Task<string> GetNextSequenceNumberAsync(string entityName, string tableName, string columnName)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var currentYear = DateTime.Now.Year;
        var yearSuffix = (currentYear % 100).ToString("00"); // e.g., "25"
        
        var sequenceName = $"seq_{entityName}_{yearSuffix}";
        
        long nextVal;

        try
        {
            var result = await context.Database.SqlQueryRaw<long>($"SELECT nextval('dcms.{sequenceName}')").ToListAsync();
            nextVal = result.FirstOrDefault();
        }
        catch (Exception ex) when (IsSequenceMissingError(ex))
        {
            var yearPrefix = $"{yearSuffix}-";
            var maxSeq = 0;
            
            // OPTIMIZED: Use parameters for the LIKE value to avoid injection (though names are internal literals)
            var pattern = $"{yearPrefix}%";
            string sqlQuery = $"SELECT {columnName} FROM dcms.{tableName} WHERE {columnName} LIKE @p0";
            
            var existingNumbers = await context.Database.SqlQueryRaw<string>(sqlQuery, pattern).ToListAsync();
            
            foreach (var num in existingNumbers)
            {
                if (string.IsNullOrEmpty(num)) continue;
                var parts = num.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out int seq))
                {
                    if (seq > maxSeq) maxSeq = seq;
                }
            }
            
            var startValue = maxSeq + 1;
            await context.Database.ExecuteSqlRawAsync($"CREATE SEQUENCE IF NOT EXISTS dcms.{sequenceName} START WITH {startValue}");
            
            var result = await context.Database.SqlQueryRaw<long>($"SELECT nextval('dcms.{sequenceName}')").ToListAsync();
            nextVal = result.FirstOrDefault();
        }

        return $"{yearSuffix}-{nextVal:000}";
    }

    private static bool IsSequenceMissingError(Exception ex)
    {
        if (ex is PostgresException pgEx && pgEx.SqlState == "42P01") return true;
        if (ex.InnerException is PostgresException innerPgEx && innerPgEx.SqlState == "42P01") return true;
        var msg = ex.ToString().ToLower();
        return msg.Contains("does not exist") || msg.Contains("undefined relation");
    }

    public async Task<List<int>> GetAvailableYearsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var inboundYears = await context.Inbounds
            .Select(i => i.InboundDate.Year)
            .Distinct()
            .ToListAsync();

        var outboundYears = await context.Outbounds
            .Select(o => o.OutboundDate.Year)
            .Distinct()
            .ToListAsync();

        return inboundYears
            .Union(outboundYears)
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();
    }
}
