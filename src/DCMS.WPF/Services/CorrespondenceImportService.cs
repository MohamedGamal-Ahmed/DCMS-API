using ClosedXML.Excel;
using DCMS.Application.Interfaces;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DCMS.WPF.Services;

public class CorrespondenceImportService : ICorrespondenceImportService
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;

    public CorrespondenceImportService(IDbContextFactory<DCMSDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ImportResultDto> ImportFromExcelAsync(string filePath, IProgress<string>? progress = null)
    {
        var result = new ImportResultDto();
        try
        {
            using var workbook = new XLWorkbook(filePath);
            
            progress?.Report("جاري قراءة صفحة الوارد...");
            var inboundRecords = ReadInboundSheet(workbook);
            result.InboundCount = inboundRecords.Count;
            
            progress?.Report("جاري قراءة صفحة المتابعة...");
            MergeFollowUpData(workbook, inboundRecords);
            
            progress?.Report("جاري قراءة صفحة الصادر...");
            var outboundRecords = ReadOutboundSheet(workbook);
            result.OutboundCount = outboundRecords.Count;
            
            progress?.Report("جاري حفظ البيانات في قاعدة البيانات...");
            await SaveToDatabase(inboundRecords, outboundRecords, progress);
            
            result.Success = true;
            result.Message = $"تم استيراد {result.InboundCount} وارد و {result.OutboundCount} صادر بنجاح";
        }
        catch (Exception ex)
        {
            result.Success = false;
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            result.Message = $"خطأ: {innerMessage}";
            Debug.WriteLine($"Correspondence Import error: {ex}");
        }
        return result;
    }

    private List<Inbound> ReadInboundSheet(XLWorkbook workbook)
    {
        var records = new List<Inbound>();
        var sheet = workbook.Worksheet("وارد");
        if (sheet == null) return records;

        var rows = sheet.RangeUsed()?.RowsUsed().Skip(1);
        if (rows == null) return records;

        foreach (var row in rows)
        {
            try
            {
                var subjectNumRaw = row.Cell(1).GetString().Trim();
                if (string.IsNullOrEmpty(subjectNumRaw)) continue;
                if (!int.TryParse(subjectNumRaw, out int subjectNum)) continue;
                if (subjectNum <= 0 || subjectNum > 5000) continue;

                var subjectNumber = $"25-{subjectNumRaw}";
                var code = Truncate(row.Cell(2).GetString().Trim(), 50);
                var fromEntityCode = Truncate(row.Cell(3).GetString().Trim(), 100);
                var fromEntity = Truncate(row.Cell(4).GetString().Trim(), 100);
                var fromEngineer = Truncate(row.Cell(5).GetString().Trim(), 100);
                var subject = Truncate(row.Cell(6).GetString().Trim(), 2000);
                var responsibleEngineer = Truncate(row.Cell(7).GetString().Trim(), 100);
                
                DateTime inboundDate = DateTime.Now;
                var dateCell = row.Cell(8);
                if (dateCell.TryGetValue<DateTime>(out var parsedDate)) inboundDate = parsedDate;

                var notes = Truncate(row.Cell(9).GetString().Trim(), 500);

                var inbound = new Inbound
                {
                    SubjectNumber = subjectNumber,
                    Code = code.ToUpperInvariant(),
                    FromEntity = fromEntity,
                    FromEngineer = fromEngineer,
                    Subject = subject,
                    ResponsibleEngineer = responsibleEngineer,
                    InboundDate = inboundDate.ToUniversalTime(),
                    Category = DetermineCategory(code),
                    Status = CorrespondenceStatus.InProgress,
                    Reply = notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                records.Add(inbound);
            }
            catch (Exception ex) { Debug.WriteLine($"Error reading inbound row: {ex.Message}"); }
        }
        return records;
    }

    private void MergeFollowUpData(XLWorkbook workbook, List<Inbound> inboundRecords)
    {
        var sheet = workbook.Worksheet("متابعة ") ?? workbook.Worksheet("متابعة");
        if (sheet == null) return;

        var rows = sheet.RangeUsed()?.RowsUsed().Skip(1);
        if (rows == null) return;

        var lookup = inboundRecords.ToDictionary(r => r.SubjectNumber, r => r);
        foreach (var row in rows)
        {
            try
            {
                var subjectNumRaw = row.Cell(1).GetString().Trim();
                if (string.IsNullOrEmpty(subjectNumRaw)) continue;

                var subjectNumber = $"25-{subjectNumRaw}";
                if (!lookup.TryGetValue(subjectNumber, out var inbound)) continue;

                var transferredTo = Truncate(row.Cell(6).GetString().Trim(), 100);
                DateTime? transferDate = null;
                var transferDateCell = row.Cell(7);
                if (transferDateCell.TryGetValue<DateTime>(out var parsedTransferDate)) transferDate = parsedTransferDate;

                var reply = Truncate(row.Cell(9).GetString().Trim(), 500);
                if (!string.IsNullOrEmpty(reply)) inbound.Reply = reply;
                if (!string.IsNullOrEmpty(transferredTo))
                {
                    inbound.TransferredTo = transferredTo;
                    inbound.TransferDate = transferDate?.ToUniversalTime();
                }

                var statusText = row.Cell(10).GetString().Trim().ToLower();
                if (statusText.Contains("مغلق") || statusText.Contains("closed") || statusText.Contains("complet"))
                    inbound.Status = CorrespondenceStatus.Closed;
                else if (statusText.Contains("جاري") || statusText.Contains("inprogress"))
                    inbound.Status = CorrespondenceStatus.InProgress;
            }
            catch (Exception ex) { Debug.WriteLine($"Error reading follow-up row: {ex.Message}"); }
        }
    }

    private List<Outbound> ReadOutboundSheet(XLWorkbook workbook)
    {
        var records = new List<Outbound>();
        var sheet = workbook.Worksheet("صادر");
        if (sheet == null) return records;

        var rows = sheet.RangeUsed()?.RowsUsed().Skip(1);
        if (rows == null) return records;

        foreach (var row in rows)
        {
            try
            {
                var subjectNumRaw = row.Cell(1).GetString().Trim();
                if (string.IsNullOrEmpty(subjectNumRaw)) continue;
                if (!int.TryParse(subjectNumRaw, out int subjectNum)) continue;
                if (subjectNum <= 0 || subjectNum > 1000) continue;

                var subjectNumber = $"25-{subjectNumRaw}";
                var code = Truncate(row.Cell(2).GetString().Trim(), 50);
                var toEntity = Truncate(row.Cell(3).GetString().Trim(), 100);
                var responsibleEngineer = Truncate(row.Cell(4).GetString().Trim(), 100);
                var subject = Truncate(row.Cell(5).GetString().Trim(), 2000);
                
                DateTime outboundDate = DateTime.Now;
                var dateCell = row.Cell(6);
                if (dateCell.TryGetValue<DateTime>(out var parsedDate)) outboundDate = parsedDate;

                var notes = Truncate(row.Cell(7).GetString().Trim(), 500);
                var relatedInbound = Truncate(row.Cell(8).GetString().Trim(), 50);

                var outbound = new Outbound
                {
                    SubjectNumber = subjectNumber,
                    Code = code.ToUpperInvariant(),
                    ToEntity = toEntity,
                    Subject = subject,
                    ResponsibleEngineer = responsibleEngineer,
                    OutboundDate = outboundDate.ToUniversalTime(),
                    RelatedInboundNo = relatedInbound,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                records.Add(outbound);
            }
            catch (Exception ex) { Debug.WriteLine($"Error reading outbound row: {ex.Message}"); }
        }
        return records;
    }

    private async Task SaveToDatabase(List<Inbound> inbounds, List<Outbound> outbounds, IProgress<string>? progress)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var existingInboundNumbers = (await context.Inbounds.Select(i => i.SubjectNumber).ToListAsync()).ToHashSet();
        var existingOutboundNumbers = (await context.Outbounds.Select(o => o.SubjectNumber).ToListAsync()).ToHashSet();

        var newInbounds = inbounds.Where(i => !existingInboundNumbers.Contains(i.SubjectNumber)).ToList();
        var newOutbounds = outbounds.Where(o => !existingOutboundNumbers.Contains(o.SubjectNumber)).ToList();

        progress?.Report($"جاري حفظ {newInbounds.Count} وارد جديد...");
        if (newInbounds.Any()) context.Inbounds.AddRange(newInbounds);

        progress?.Report($"جاري حفظ {newOutbounds.Count} صادر جديد...");
        if (newOutbounds.Any()) context.Outbounds.AddRange(newOutbounds);

        await context.SaveChangesAsync();
        progress?.Report($"تم الحفظ: {newInbounds.Count} وارد، {newOutbounds.Count} صادر");
    }

    private InboundCategory DetermineCategory(string code)
    {
        if (string.IsNullOrEmpty(code)) return InboundCategory.Posta;
        code = code.ToUpperInvariant();
        if (code.StartsWith("IN-CHR") || code.Contains("CHR")) return InboundCategory.Posta;
        if (code.StartsWith("IN-TND") || code.Contains("TND")) return InboundCategory.Posta;
        if (code.StartsWith("IN-CLM") || code.Contains("CLM")) return InboundCategory.Posta;
        if (code.StartsWith("IN-GNL") || code.Contains("GNL")) return InboundCategory.Posta;
        return InboundCategory.Posta;
    }

    private static string Truncate(string? value, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}
