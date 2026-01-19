using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DCMS.Application.DTOs;
using DCMS.Application.Interfaces;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DCMS.Infrastructure.Services;

public class MeetingService : IMeetingService
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public MeetingService(IDbContextFactory<DCMSDbContext> contextFactory, ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<List<AiSearchResultDto>> SearchMeetingsAsync(
        string? query = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? location = null,
        string? attendee = null,
        string? project = null,
        string? partner = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var meetingsQuery = context.Meetings.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(query))
        {
            meetingsQuery = meetingsQuery.Where(m => 
                m.Title.Contains(query) || 
                (m.Description != null && m.Description.Contains(query)));
        }

        if (startDate.HasValue)
        {
            meetingsQuery = meetingsQuery.Where(m => m.StartDateTime >= DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc));
        }

        if (endDate.HasValue)
        {
            meetingsQuery = meetingsQuery.Where(m => m.StartDateTime <= DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc));
        }

        if (!string.IsNullOrEmpty(location))
        {
            meetingsQuery = meetingsQuery.Where(m => m.Location != null && m.Location.Contains(location));
        }

        if (!string.IsNullOrEmpty(attendee))
        {
            meetingsQuery = meetingsQuery.Where(m => m.Attendees != null && m.Attendees.Contains(attendee));
        }

        if (!string.IsNullOrEmpty(project))
        {
            meetingsQuery = meetingsQuery.Where(m => m.RelatedProject != null && m.RelatedProject.Contains(project));
        }

        if (!string.IsNullOrEmpty(partner))
        {
            meetingsQuery = meetingsQuery.Where(m => m.RelatedPartner != null && m.RelatedPartner.Contains(partner));
        }

        var results = await meetingsQuery
            .OrderByDescending(m => m.StartDateTime)
            .Select(m => new { m.Id, m.Title, m.MeetingType, m.StartDateTime, m.Location, m.IsOnline, m.Description })
            .Take(5) // EMERGENCY: LIMIT 5
            .ToListAsync();

        return results.Select(m => new AiSearchResultDto
        {
            Id = m.Id.ToString(),
            Type = "Meeting",
            SubjectNumber = m.MeetingType.ToString(),
            Subject = m.Title,
            Date = m.StartDateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            FromOrTo = m.Location ?? "N/A",
            Status = m.IsOnline ? "Online" : "In-Person",
            Summary = m.Description
        }).ToList();
    }

    public async Task<Meeting?> GetByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Meetings.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<int> CreateMeetingAsync(string title, DateTime startDateTime, string location, string description)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var meeting = new Meeting
        {
            Title = title,
            StartDateTime = DateTime.SpecifyKind(startDateTime, DateTimeKind.Utc),
            Location = location,
            Description = description,
            IsRecurring = false,
            MeetingType = MeetingType.Meeting,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUserService.CurrentUserId
        };

        context.Meetings.Add(meeting);
        await context.SaveChangesAsync();
        return meeting.Id;
    }
}
