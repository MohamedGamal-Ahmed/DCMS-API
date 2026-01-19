using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using DCMS.Application.DTOs;
using DCMS.Application.Interfaces;
using DCMS.Domain.Enums;
using Microsoft.SemanticKernel;

namespace DCMS.Infrastructure.Ai.Plugins;

public class CorrespondencePlugin
{
    private readonly ICorrespondenceService _correspondenceService;

    public CorrespondencePlugin(ICorrespondenceService correspondenceService)
    {
        _correspondenceService = correspondenceService;
    }

    [KernelFunction, Description("يبحث عن المراسلات (وارد أو صادر) بناءً على معايير مثل الموضوع، التاريخ، الحالة، أو الجهة. لا تخمن المعاملات إذا لم يذكرها المستخدم.")]
    public async Task<List<AiSearchResultDto>> SearchCorrespondences(
        [Description("نص للبحث في الموضوع أو رقم المراسلة")] string? query = null,
        [Description("نوع السجل: 'Inbound' للوارد، 'Outbound' للصادر")] string? recordType = null,
        [Description("حالة المراسلة: New, InProgress, Closed")] string? status = null,
        [Description("تاريخ البدء للبحث (yyyy-MM-dd)")] string? startDate = null,
        [Description("تاريخ الانتهاء للبحث (yyyy-MM-dd)")] string? endDate = null,
        [Description("اسم الجهة المرسلة أو المستقبلة")] string? entity = null)
    {
        CorrespondenceStatus? enumStatus = null;
        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<CorrespondenceStatus>(status, true, out var parsedStatus))
                enumStatus = parsedStatus;
        }

        DateTime? start = null;
        if (DateTime.TryParse(startDate, out var s)) start = s;

        DateTime? end = null;
        if (DateTime.TryParse(endDate, out var e)) end = e;

        return await _correspondenceService.SearchAsync(
            query: query,
            recordType: recordType,
            status: enumStatus,
            startDate: start,
            endDate: end,
            fromEntity: entity,
            toEntity: entity
        );
    }

    [KernelFunction, Description("يجلب تفاصيل مراسلة محددة باستخدام المعرف والنوع")]
    public async Task<AiSearchResultDto?> GetCorrespondenceDetails(
        [Description("المعرف الرقمي للمراسلة")] int id,
        [Description("نوع المراسلة (Inbound أو Outbound)")] string type)
    {
        return await _correspondenceService.GetByIdAsync(id, type);
    }
}
