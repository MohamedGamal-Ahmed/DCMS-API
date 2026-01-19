using DCMS.Application.Interfaces;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace DCMS.Infrastructure.Services;

public class DashboardAiService
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly IAiService _aiService;

    public DashboardAiService(IDbContextFactory<DCMSDbContext> contextFactory, IAiService aiService)
    {
        _contextFactory = contextFactory;
        _aiService = aiService;
    }

    public async Task<string> GenerateExecutiveSummaryAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var dashboardContext = await GetDashboardContextForAi(context);
        
        var prompt = $@"أنت محلل إداري ذكي لنظام إدارة المراسلات (DCMS).
حلل البيانات التالية بدقة وقدم تقريراً إدارياً شاملاً باللغة العربية.

{dashboardContext}

المطلوب في التقرير:
1. إحصائيات سريعة للحالة العامة.
2. تحديد المشكلة الرئيسية (Problem Identification): حلل البيانات لرصد أبطأ العمليات أو أكبر تكدس للمهام أو خلل في توزيع العمل.
3. الحل المقترح (Proposed Solution): قدم حلولاً عملية ومباشرة بناءً على الأرقام (مثلاً: نقل مهام، توجيه إنذار، تحفيز مادي، إلخ).
4. تحليل ضغط العمل (Bottlenecks) لكل مهندس واقتراح إعادة توزيع للمهام.
5. تقييم إنتاجية موظفي المتابعة.

ملاحظات هامة:
- لا تعلق على جودة النظام التقنية، ركز فقط على العمليات والبيانات.
- استخدم تنسيق Markdown (عناوين بارزة، نقاط).
- اجعل قسم ""المشكلة والحل"" واضحاً جداً في بداية التقرير.
- كن صريحاً ومباشراً في التوصيات.";

        var response = await _aiService.GetResponseAsync(prompt, new List<ChatMessage>());
        return response.Content;
    }

    private async Task<string> GetDashboardContextForAi(DCMSDbContext context)
    {
        var sb = new StringBuilder();
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        sb.AppendLine("# إحصائيات النظام الحالية");
        sb.AppendLine($"التاريخ: {now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();

        sb.AppendLine("## مؤشرات الأداء العامة (KPIs)");
        sb.AppendLine($"- إجمالي الوارد هذا الشهر: {await context.Inbounds.CountAsync(i => i.InboundDate >= startOfMonth)}");
        sb.AppendLine($"- الموضوعات الجارية حالياً: {await context.Inbounds.CountAsync(i => i.Status == CorrespondenceStatus.New || i.Status == CorrespondenceStatus.InProgress)}");
        sb.AppendLine($"- الموضوعات المغلقة: {await context.Inbounds.CountAsync(i => i.Status == CorrespondenceStatus.Completed || i.Status == CorrespondenceStatus.Closed)}");
        var sevenDaysAgo = now.AddDays(-7);
        sb.AppendLine($"- المتأخرات (+7 أيام): {await context.Inbounds.CountAsync(i => i.InboundDate < sevenDaysAgo && (i.Status == CorrespondenceStatus.New || i.Status == CorrespondenceStatus.InProgress))}");
        sb.AppendLine();

        sb.AppendLine("## أداء المهندسين المسئولين (Workload)");
        var engineers = await context.Engineers.Where(e => e.IsResponsibleEngineer).ToListAsync();
        foreach (var eng in engineers)
        {
            var open = await context.InboundResponsibleEngineers.CountAsync(ire => ire.EngineerId == eng.Id && (ire.Inbound.Status == CorrespondenceStatus.New || ire.Inbound.Status == CorrespondenceStatus.InProgress));
            var closed = await context.InboundResponsibleEngineers.CountAsync(ire => ire.EngineerId == eng.Id && (ire.Inbound.Status == CorrespondenceStatus.Completed || ire.Inbound.Status == CorrespondenceStatus.Closed));
            sb.AppendLine($"- {eng.FullName}: جاري ({open})، منجز ({closed})");
        }
        sb.AppendLine();

        sb.AppendLine("## أداء موظفي المتابعة (Staff Performance)");
        var staffUsers = await context.Users.Where(u => u.Role == UserRole.FollowUpStaff).ToListAsync();
        foreach (var user in staffUsers)
        {
            var regs = await context.Inbounds.CountAsync(i => i.CreatedByUserId == user.Id && i.CreatedAt >= startOfMonth);
            var actions = await context.InboundTransfers.CountAsync(t => t.CreatedByUserId == user.Id && t.TransferDate >= startOfMonth);
            var closures = await context.Inbounds.CountAsync(i => i.UpdatedByUserId == user.Id && (i.Status == CorrespondenceStatus.Completed || i.Status == CorrespondenceStatus.Closed) && i.UpdatedAt >= startOfMonth);
            sb.AppendLine($"- {user.FullName ?? user.Username}: تسجيل ({regs})، تحويلات ({actions})، إغلاق ({closures})");
        }
        sb.AppendLine();

        sb.AppendLine("## التوزيع الخارجي (External Distribution)");
        var external = await context.InboundTransfers
            .Include(t => t.Engineer)
            .Where(t => t.Engineer.IsResponsibleEngineer == false && t.TransferDate >= startOfMonth)
            .GroupBy(t => t.Engineer.FullName)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(10)
            .ToListAsync();
        foreach (var ext in external)
        {
            sb.AppendLine($"- {ext.Name}: {ext.Count} موضوع");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates actionable management insights and workload recommendations on-demand.
    /// Called when user clicks 'Analyze' button to save API tokens.
    /// </summary>
    public async Task<string> GenerateManagementInsightsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var dashboardContext = await GetDashboardContextForAi(context);
        
        var systemPrompt = @"You are a Professional Operations Manager for a Correspondence Management System.
Your role is to analyze operational data and provide clear, actionable business insights.

INSTRUCTIONS:
- Analyze the numbers and metrics provided
- Provide exactly 3-4 bullet points of 'Actionable Insights'
- Provide 2-3 'Workload Recommendations' to balance team capacity
- Use Arabic language for the response
- Be direct and specific with recommendations
- Focus on practical, immediate actions
- Format using Markdown with clear headers";

        var userPrompt = $@"قم بتحليل البيانات التالية وقدم توصيات إدارية عملية:

{dashboardContext}

المطلوب:
✨ **رؤى عملية (Actionable Insights)**: 3-4 نقاط سريعة حول الوضع الحالي
📊 **توصيات توزيع العمل (Workload Recommendations)**: اقتراحات لموازنة الأحمال بين الفريق
⚠️ **تنبيهات هامة**: أي مشاكل تحتاج اهتمام فوري";

        var response = await _aiService.GetResponseAsync(userPrompt, new List<ChatMessage>(), systemPrompt);
        return response.Content;
    }
}
