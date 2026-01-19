using System.Text.Json;
using DCMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DCMS.Infrastructure.Services;
using DCMS.Domain.Enums;

namespace DCMS.Web.Controllers;

[Authorize]
public class MobileHubController : Controller
{
    private readonly ICorrespondenceService _correspondenceService;
    private readonly IMeetingService _meetingService;
    private readonly DashboardDataService _dashboardDataService;
    private readonly ICurrentUserService _currentUserService;

    public MobileHubController(
        ICorrespondenceService correspondenceService,
        IMeetingService meetingService,
        DashboardDataService dashboardDataService,
        ICurrentUserService currentUserService)
    {
        _correspondenceService = correspondenceService;
        _meetingService = meetingService;
        _dashboardDataService = dashboardDataService;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index()
    {
        var data = await GetAppDataAsync();
        ViewBag.AppData = JsonSerializer.Serialize(data, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetData()
    {
        var data = await GetAppDataAsync();
        return Json(data);
    }

    private async Task<object> GetAppDataAsync()
    {
        // 1. Fetch Latest Correspondences (Inbound/Outbound)
        var results = await _correspondenceService.SearchAsync(take: 50);
        
        // 2. Fetch Meetings (Agenda)
        // Note: I'll fetch meetings for a wider range to support the calendar
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(2).AddTicks(-1);
        
        var meetings = await _meetingService.SearchMeetingsAsync(startDate: startOfMonth, endDate: endOfMonth);
        
        // 3. Fetch Stats for Profile/Dashboard
        var kpis = await _dashboardDataService.GetGeneralKpisAsync(
            _currentUserService.CurrentUserFullName, 
            _currentUserService.CurrentUserId ?? 0, 
            Enum.Parse<UserRole>(_currentUserService.CurrentUserRole ?? "FollowUpStaff"));

        return new
        {
            user = new
            {
                name = _currentUserService.CurrentUserFullName ?? _currentUserService.CurrentUserName,
                role = _currentUserService.CurrentUserRole,
                id = _currentUserService.CurrentUserId
            },
            correspondences = results,
            meetings = meetings,
            stats = new
            {
                meetingsToday = kpis.TotalInboundToday + kpis.TotalOutboundToday, // Placeholder, usually it would be real meetings
                pendingIssues = kpis.OngoingTasks,
                completedReports = kpis.ClosedTasks
            }
        };
    }
}
