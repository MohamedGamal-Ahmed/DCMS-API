using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DCMS.Application.DTOs;
using DCMS.Domain.Enums;

namespace DCMS.Application.Interfaces;

public interface ICorrespondenceService
{
    Task<List<AiSearchResultDto>> SearchAsync(
        string? query = null,
        string? recordType = null,
        CorrespondenceStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? subject = null,
        string? code = null,
        string? fromEntity = null,
        string? toEntity = null,
        string? transferredTo = null,
        int take = 10);
    
    Task<AiSearchResultDto?> GetByIdAsync(int id, string type);
    Task<AiSearchResultDto?> GetByIdAsync(string idOrNumber, string type);
    Task<int> CreateInboundAsync(string subject, string code, string fromEntity, string assignedEngineer);
    Task<int> CreateOutboundAsync(string subject, string code, string toEntity);
    Task<List<AiSearchResultDto>> GetSimilarInboundsAsync(string subject, int take = 3);
}
