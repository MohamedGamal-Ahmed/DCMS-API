using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DCMS.Application.DTOs;
using DCMS.Domain.Entities;

namespace DCMS.Application.Interfaces;

public interface IMeetingService
{
    Task<List<AiSearchResultDto>> SearchMeetingsAsync(
        string? query = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? location = null,
        string? attendee = null,
        string? project = null,
        string? partner = null);

    Task<Meeting?> GetByIdAsync(int id);
    Task<int> CreateMeetingAsync(string title, DateTime startDateTime, string location, string description);
}
