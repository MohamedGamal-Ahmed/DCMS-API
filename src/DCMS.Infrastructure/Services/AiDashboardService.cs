using DCMS.Application.Interfaces;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCMS.Infrastructure.Services;

public class AiDashboardService : IAiDashboardService
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly IMemoryCache _cache;
    private const string CACHE_KEY_PREFIX = "ai_dashboard_data_";

    public AiDashboardService(IDbContextFactory<DCMSDbContext> contextFactory, IMemoryCache cache)
    {
        _contextFactory = contextFactory;
        _cache = cache;
    }

    public async Task<AiDashboardDataDto> GetAiDashboardDataAsync(int userId, string? userRole, string? fullName, string? userName)
    {
        string cacheKey = $"{CACHE_KEY_PREFIX}{userId}";
        if (_cache.TryGetValue(cacheKey, out AiDashboardDataDto? cachedData) && cachedData != null)
        {
            return cachedData;
        }

        var result = new AiDashboardDataDto();
        var startOf2026 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var today = DateTime.UtcNow.Date;

            // Stats for AI Report (2026 onwards)
            result.TotalReceived = await context.Inbounds.CountAsync(i => i.InboundDate >= startOf2026);
            result.TotalPresented = await context.Inbounds.CountAsync(i => i.InboundDate >= startOf2026 && (i.Status == CorrespondenceStatus.InProgress || i.Status == CorrespondenceStatus.Completed || i.Status == CorrespondenceStatus.Closed));
            result.TotalPending = await context.Inbounds.CountAsync(i => i.InboundDate >= startOf2026 && i.Status == CorrespondenceStatus.New);
            result.TotalTransferred = await context.Inbounds.CountAsync(i => i.InboundDate >= startOf2026 && ( (i.TransferredTo != null && i.TransferredTo != "" && i.TransferredTo != "N/A") || i.Transfers.Any() ));

            // Total internal transactions (New status)
            result.TotalInternalTransactions = await context.Inbounds
                .CountAsync(i => i.Status == CorrespondenceStatus.New && i.InboundDate >= startOf2026);

            // Critical external delays (>3 days from transfer)
            result.CriticalExternalDelays = await context.Inbounds
                .CountAsync(i => (i.TransferDate != null && i.TransferDate.Value.Date <= today.AddDays(-3) && i.Status != CorrespondenceStatus.Closed && i.InboundDate >= startOf2026) ||
                                (i.Transfers.Any(t => t.TransferDate.Date <= today.AddDays(-3) && string.IsNullOrEmpty(t.Response)) && i.Status != CorrespondenceStatus.Closed && i.InboundDate >= startOf2026));

            // Overall completion rate
            var total = result.TotalReceived;
            var closed = await context.Inbounds.CountAsync(i => (i.Status == CorrespondenceStatus.Closed || i.Status == CorrespondenceStatus.Completed) && i.InboundDate >= startOf2026);
            result.OverallCompletionRate = total > 0 ? (int)((closed * 100.0) / total) : 0;

            // Fastest engineer
            result.FastestEngineer = await context.Inbounds
                .Where(i => i.Status == CorrespondenceStatus.Closed && i.ResponsibleEngineer != null && i.InboundDate >= startOf2026)
                .GroupBy(i => i.ResponsibleEngineer)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync() ?? "-";

            // Manager Review (Only show New/InProgress that are NOT yet transferred at all)
            var managerData = await context.Inbounds
                .AsNoTracking()
                .Include(i => i.ResponsibleEngineers)
                .ThenInclude(re => re.Engineer)
                .Include(i => i.Transfers)
                .Where(i => (i.Status == CorrespondenceStatus.New || i.Status == CorrespondenceStatus.InProgress) && 
                           (i.TransferredTo == null || i.TransferredTo == "" || i.TransferredTo == "N/A") &&
                           !i.Transfers.Any() &&
                           i.InboundDate >= startOf2026)
                .OrderByDescending(i => i.CreatedAt)
                .Take(100)
                .ToListAsync();

            foreach (var item in managerData)
            {
                var daysDelayed = (today - item.CreatedAt.Date).Days;
                var responsible = GetResponsibleString(item);

                result.PendingManagerReview.Add(new AiPendingItemDto
                {
                    Id = item.Id,
                    SubjectNumber = item.SubjectNumber,
                    Subject = item.Subject,
                    ResponsibleEngineer = responsible,
                    DaysDelayed = daysDelayed,
                    DelayType = daysDelayed > 0 ? $"متأخرة منذ {daysDelayed} يوم" : "وارد اليوم"
                });
            }

            // Consultant Response (Everything that HAS BEEN transferred either via prop or junction, and has no response)
            var consultantData = await context.Inbounds
                .AsNoTracking()
                .Include(i => i.ResponsibleEngineers)
                .ThenInclude(re => re.Engineer)
                .Include(i => i.Transfers)
                .ThenInclude(t => t.Engineer)
                .Where(i => ( (i.TransferredTo != null && i.TransferredTo != "" && i.TransferredTo != "N/A") || i.Transfers.Any() ) && 
                           i.Status != CorrespondenceStatus.Completed && i.Status != CorrespondenceStatus.Closed &&
                           i.InboundDate >= startOf2026)
                .OrderByDescending(i => i.TransferDate ?? (i.Transfers.Any() ? i.Transfers.Max(t => t.TransferDate) : DateTime.MinValue))
                .Take(100)
                .ToListAsync();

            foreach (var item in consultantData)
            {
                var transDate = item.TransferDate;
                var responsible = GetResponsibleString(item);
                var transferredTo = item.TransferredTo;

                if (string.IsNullOrEmpty(transferredTo) && item.Transfers.Any())
                {
                    var lastTrans = item.Transfers.OrderByDescending(t => t.TransferDate).First();
                    transferredTo = lastTrans.Engineer?.FullName;
                    transDate = lastTrans.TransferDate;
                    
                    // IF there is a response in the junction table, this item isn't "pending" anymore for this section
                    if (!string.IsNullOrEmpty(lastTrans.Response)) continue;
                }

                var daysDelayed = transDate.HasValue ? (today - transDate.Value.Date).Days : 0;
                string delayText;
                if (daysDelayed == 0) delayText = "اليوم";
                else if (daysDelayed == 1) delayText = "منذ يوم";
                else if (daysDelayed == 2) delayText = "منذ يومين";
                else if (daysDelayed <= 10) delayText = $"منذ {daysDelayed} أيام";
                else delayText = $"منذ {daysDelayed} يوماً";

                result.PendingConsultantResponse.Add(new AiPendingItemDto
                {
                    Id = item.Id,
                    SubjectNumber = item.SubjectNumber,
                    Subject = item.Subject,
                    ResponsibleEngineer = responsible,
                    TransferredTo = transferredTo,
                    DaysDelayed = daysDelayed,
                    DelayType = $"تم العرض {delayText}"
                });
            }

            // Missing Attachments
            if (userRole == "Admin" || userRole == "FollowUpStaff" || userRole == "OfficeManager")
            {
                var missingData = await context.Inbounds
                    .AsNoTracking()
                    .Include(i => i.ResponsibleEngineers)
                    .ThenInclude(re => re.Engineer)
                    .Where(i => (i.OriginalAttachmentUrl == null || i.OriginalAttachmentUrl == "" || i.OriginalAttachmentUrl == "N/A") && 
                               (i.AttachmentUrl == null || i.AttachmentUrl == "" || i.AttachmentUrl == "N/A") &&
                                i.Status != CorrespondenceStatus.Closed &&
                                i.InboundDate >= startOf2026)
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(100)
                    .ToListAsync();

                foreach (var item in missingData)
                {
                    result.MissingAttachments.Add(new AiPendingItemDto
                    {
                        Id = item.Id,
                        SubjectNumber = item.SubjectNumber,
                        Subject = item.Subject,
                        ResponsibleEngineer = GetResponsibleString(item),
                        DelayType = "⚠️ مفقود رابط OneDrive"
                    });
                }
            }

            // Generate Diagnostic Log
            result.DiagnosticLog = GenerateLog(userId, userRole, fullName, userName, result);
            
            // EMERGENCY CACHE: Cache for 5 minutes (reduced from 10)
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            result.DiagnosticLog = $"Error: {ex.Message}";
        }
        return result;
    }

    private string GetResponsibleString(DCMS.Domain.Entities.Inbound item)
    {
        var responsible = item.ResponsibleEngineer;
        if (string.IsNullOrEmpty(responsible) && item.ResponsibleEngineers.Any())
        {
            responsible = string.Join(", ", item.ResponsibleEngineers.Select(re => re.Engineer.FullName));
        }
        return !string.IsNullOrEmpty(responsible) ? responsible : "غير محدد";
    }

    private string GenerateLog(int userId, string? userRole, string? fullName, string? userName, AiDashboardDataDto data)
    {
        var log = new StringBuilder();
        log.AppendLine("🔍 [بيانات التشخيص والفحص]");
        log.AppendLine($"• المستخدم الحالي: {userName} (ID: {userId})");
        log.AppendLine($"• الدور: {userRole}");
        log.AppendLine($"• الاسم الكامل: {fullName ?? "غير مسجل"}");
        log.AppendLine("---");
        log.AppendLine($"🟢 بانتظار العرض: {data.PendingManagerReview.Count} سجل");
        log.AppendLine($"🔴 بانتظار الرد: {data.PendingConsultantResponse.Count} سجل");
        log.AppendLine($"🟠 روابط مفقودة: {data.MissingAttachments.Count} سجل");
        log.AppendLine("---");
        log.AppendLine($"• إجمالي السجلات المعروضة: {data.PendingManagerReview.Count + data.PendingConsultantResponse.Count + data.MissingAttachments.Count}");
        log.AppendLine($"• معايير البحث: {(userId.ToString() ?? "N/A")} | {fullName ?? "N/A"} | {userName ?? "N/A"}");
        log.AppendLine($"• حالة الفلترة: {( (userRole != "Admin" && userRole != "FollowUpStaff") ? "مهندس (فلترة حسب الملكية)" : "إداري (عرض كامل)" )}");
        return log.ToString();
    }
}
