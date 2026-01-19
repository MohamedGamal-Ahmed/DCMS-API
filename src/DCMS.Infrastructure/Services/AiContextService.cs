using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DCMS.Application.Interfaces;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DCMS.Infrastructure.Services;

public class AiContextService : IAiContextService
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public AiContextService(
        IDbContextFactory<DCMSDbContext> contextFactory,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<string> GetSystemPromptAsync()
    {
        var userRole = _currentUserService.CurrentUserRole ?? "Unknown";
        var userName = _currentUserService.CurrentUserName;
        var criticalAlerts = await GetCriticalAlertsAsync();
        
        return $@"Role & Context: You are the 'Technical Follow-up Manager', the core engine of the DCMS Command Center. 
You are no longer a side chatbot; you are the Decision Support System that users see immediately upon login. 
Your primary goal is to ensure document lifecycle integrity and eliminate delays.

TODAY'S DATE: {DateTime.Now:yyyy-MM-dd}
CURRENT USER: {userName} (Role: {userRole})

Data-Driven Intelligence: You MUST analyze the [DATABASE_CONTEXT] below before answering.
{criticalAlerts}

Role-Based Response Strategy:
1. If UserRole == 'FollowUpStaff' (موظف متابعة): Prioritize missing OneDrive links. 
   Say: 'تنبيه: يوجد [X] موضوعات بدون رابط OneDrive.'
   
2. If UserRole == 'TechnicalManager' (مدير فني): Prioritize 'Pending Manager Review' (>48h) and 'External Follow-up' (>72h).
   Say: 'تنبيه: يوجد [X] موضوعات لم تُعرض على المدير منذ أكثر من يومين.'
   
3. If UserRole == 'Admin' (مدير النظام): Provide high-level ROI and bottleneck report.
   Focus on: Total delays, missing data, overall efficiency metrics.

Mandatory Action - 'The Proactive Brief':
When initializing or upon user request, generate a summary that includes:
- Critical Alerts: Direct mentions of record IDs (e.g., 'الموضوع رقم IN-0123 متأخر منذ 4 أيام')
- Drafting Assistance: Offer to draft reminder emails immediately for any external delays
- Smart Buttons: Suggest actions using format 'BUTTONS: [Draft Reminder Email], [Verify Archive Completion]'

Strict Rules:
1. No General Chat: If asked about non-DCMS topics (cooking, general knowledge, etc.), respond EXACTLY:
   'عذراً، تخصصي هو مدير المتابعة الفنية لنظام DCMS فقط.'
   
2. No Direct Edits: You suggest fixes (The Draft), and the human clicks the button to apply it. NEVER directly modify data.

3. Data Integrity: A record without a OneDrive link is a 'System Failure' in your view. Be strict about this.

4. Response Format Rules:
   - When responding to a specific Transaction/Subject Number (if provided in prompt), ALWAYS start your response with: '# رقم المعاملة: [SubjectNumber]' as a header on the first line.
   - For missing attachments, provide context and explicitly suggest 'إضافة رابط' in your buttons.
   - Professional Tone: Use administrative, serious, practical tone. Use emojis (⚠️, ✅, 📧, ⏳) for quick readability.
   - Always respond in Arabic.
   - Suggest actions using format 'BUTTONS: [Button Text 1], [Button Text 2]'. 
   - If suggesting a link fix, use 'BUTTONS: [إضافة رابط]' as one of the options.

EXAMPLE INTERACTIONS:
User (first login): (automatic welcome)
Assistant: 'أهلاً بك في مركز قيادة المتابعة الفنية ⚡
يوجد 3 موضوعات لم يتم إرفاق رابط OneDrive لها ⚠️، ومراسلة محولة للمهندس الاستشاري منذ 4 أيام (رقم IN-0180). 
هل أجهز لك مسودة إيميل لمتابعة المهندس الاستشاري؟
BUTTONS: [📧 صياغة إيميل تذكير], [✅ فحص اكتمال الأرشفة]'

User: 'What is the capital of Egypt?'
Assistant: 'عذراً، تخصصي هو مدير المتابعة الفنية لنظام DCMS فقط.'
";
    }

    public async Task<string> GetCriticalAlertsAsync()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var userName = _currentUserService.CurrentUserName;
            var userFullName = _currentUserService.CurrentUserFullName;
            var userRole = _currentUserService.CurrentUserRole;
            bool isAdmin = userRole == "Admin";

            // Base queries
            var inboundQuery = context.Inbounds.AsQueryable();
            int? currentUserId = _currentUserService.CurrentUserId;
            
            // Filter by Ownership for non-Admins/non-Followup
            if (!isAdmin && userRole != "FollowUpStaff")
            {
                var normalizedFullName = NormalizeArabic(userFullName ?? "");
                var normalizedUserName = userName?.ToLower() ?? "";

                inboundQuery = inboundQuery.AsEnumerable()
                                    .Where(i => 
                                        (currentUserId.HasValue && i.CreatedByUserId == currentUserId) ||
                                        (i.ResponsibleEngineer != null && 
                                               (NormalizeArabic(i.ResponsibleEngineer).Contains(normalizedFullName) || 
                                                normalizedFullName.Contains(NormalizeArabic(i.ResponsibleEngineer)) ||
                                                i.ResponsibleEngineer.ToLower().Contains(normalizedUserName) ||
                                                normalizedUserName.Contains(i.ResponsibleEngineer.ToLower()))))
                                    .AsQueryable();
            }

            // Delayed or New Internal Review (Current/Presented)
            var delayedInbound = await inboundQuery
                .Where(i => i.Status == Domain.Enums.CorrespondenceStatus.New || i.Status == Domain.Enums.CorrespondenceStatus.InProgress)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => i.SubjectNumber)
                .Take(10).ToListAsync();
            
            // Missing OneDrive Links (Staff focus)
            var missingAttachments = await inboundQuery
                .Where(i => (i.OriginalAttachmentUrl == null || i.OriginalAttachmentUrl == "" || i.OriginalAttachmentUrl == "N/A") && 
                           (i.AttachmentUrl == null || i.AttachmentUrl == "" || i.AttachmentUrl == "N/A") &&
                           i.Status != Domain.Enums.CorrespondenceStatus.Closed)
                .Select(i => i.SubjectNumber)
                .Take(10).ToListAsync();
            
            // Delayed External Response (Presented but no reply)
            var delayedExternal = await inboundQuery
                .Where(i => i.TransferDate != null && 
                           (i.Reply == null || i.Reply == "" || i.Reply == "N/A") &&
                           i.Status != Domain.Enums.CorrespondenceStatus.Closed &&
                           i.Status != Domain.Enums.CorrespondenceStatus.Completed)
                .Select(i => new { i.SubjectNumber, i.TransferredTo })
                .Take(10).ToListAsync();

            return $@"
[DATABASE_CONTEXT]
- Delayed Internal Review: {delayedInbound.Count} items (IDs: {string.Join(", ", delayedInbound)})
- Missing OneDrive Links: {missingAttachments.Count} items (IDs: {string.Join(", ", missingAttachments)})
- Delayed External Response: {delayedExternal.Count} items (Details: {string.Join("; ", delayedExternal.Select(x => $"{x.SubjectNumber} -> {x.TransferredTo}"))})
[/DATABASE_CONTEXT]";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CRITICAL ALERTS ERROR] {ex.Message}");
            return "\n[DATABASE_CONTEXT]\n- No alerts available (error loading data)\n[/DATABASE_CONTEXT]";
        }
    }

    private string NormalizeArabic(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text
            .Replace("أ", "ا")
            .Replace("إ", "ا")
            .Replace("آ", "ا")
            .Replace("ة", "ه")
            .Replace("ى", "ي");
    }
}
