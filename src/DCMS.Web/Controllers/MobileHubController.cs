using System.Text.Json;
using DCMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DCMS.Infrastructure.Services;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DCMS.Web.Controllers;

public class MobileHubController : Controller
{
    private readonly ICorrespondenceService _correspondenceService;
    private readonly IMeetingService _meetingService;
    private readonly DashboardDataService _dashboardDataService;
    private readonly ICurrentUserService _currentUserService;
    private readonly DCMSDbContext _context;

    public MobileHubController(
        ICorrespondenceService correspondenceService,
        IMeetingService meetingService,
        DashboardDataService dashboardDataService,
        ICurrentUserService currentUserService,
        DCMSDbContext context)
    {
        _correspondenceService = correspondenceService;
        _meetingService = meetingService;
        _dashboardDataService = dashboardDataService;
        _currentUserService = currentUserService;
        _context = context;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var data = await GetAppDataAsync();
        ViewBag.AppData = JsonSerializer.Serialize(data, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        
        return View();
    }
    /// <summary>
    /// Debug: Check if user exists
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> CheckUser([FromQuery] string username)
    {
        try
        {
            // Use Select to only get the columns we need (avoids missing column errors)
            var user = await _context.Users
                .Where(u => u.Username == username)
                .Select(u => new { u.Id, u.Username, u.FullName, u.Role, u.PasswordHash })
                .FirstOrDefaultAsync();
            
            if (user == null)
                return NotFound(new { found = false, message = "User not found" });
            
            return Ok(new { found = true, id = user.Id, name = user.FullName, role = user.Role.ToString() });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
        }
    }

    /// <summary>
    /// Login endpoint for Mobile App
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] MobileLoginRequest request)
    {
        string step = "Initialization";
        try
        {
            if (request == null)
            {
                return BadRequest(new { success = false, message = "طلب غير صالح - body is null" });
            }

            step = "Checking parameters";
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { success = false, message = "اسم المستخدم وكلمة المرور مطلوبان" });
            }

            step = "Querying database for user: " + request.Username;
            // Use Select to only get required columns (avoids PublicKeyCredential column error)
            var user = await _context.Users
                .Where(u => u.Username == request.Username)
                .Select(u => new { u.Id, u.Username, u.FullName, u.Role, u.PasswordHash })
                .FirstOrDefaultAsync();

            step = "Checking if user exists";
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "اسم المستخدم غير موجود" });
            }

            step = "Hashing password";
            // Hash the password using SHA256 (same as WPF app)
            var hashedPassword = HashPassword(request.Password);

            step = "Verifying password match";
            // Check password
            if (user.PasswordHash != hashedPassword)
            {
                return Unauthorized(new { success = false, message = "كلمة المرور غير صحيحة" });
            }

            step = "Preparing successful response";
            return Ok(new
            {
                success = true,
                user = new
                {
                    id = user.Id,
                    name = user.FullName ?? user.Username,
                    username = user.Username,
                    role = user.Role.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"خطأ في الخادم (Step: {step}): " + ex.Message, detail = ex.ToString() });
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Get all mobile data - for logged in users or anonymous with limited data
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetData([FromQuery] int? userId = null)
    {
        var data = await GetAppDataAsync(userId);
        return Json(data);
    }

    private async Task<object> GetAppDataAsync(int? userId = null)
    {
        // 1. Fetch Latest Correspondences with responsible engineer info
        var inbounds = await _context.Inbounds
            .Include(i => i.ResponsibleEngineers)
            .ThenInclude(re => re.Engineer)
            .OrderByDescending(i => i.CreatedAt)
            .Take(50)
            .Select(i => new
            {
                id = i.Id,
                subject = i.Subject,
                referenceNumber = i.SubjectNumber,
                status = i.Status == CorrespondenceStatus.New ? "New" : "Processed",
                date = i.CreatedAt.ToString("yyyy-MM-dd"),
                // Get the responsible engineer name (first one from the junction table)
                responsibleEngineer = i.ResponsibleEngineers
                    .Select(re => re.Engineer.FullName)
                    .FirstOrDefault() ?? i.ResponsibleEngineer ?? "غير محدد",
                // Get attachment URL (use OriginalAttachmentUrl or AttachmentUrl)
                attachmentUrl = !string.IsNullOrEmpty(i.OriginalAttachmentUrl) 
                    ? i.OriginalAttachmentUrl 
                    : i.AttachmentUrl,
                category = "Inbound"
            })
            .ToListAsync();

        // 2. Fetch ALL Meetings for the year (for calendar view)
        var now = DateTime.UtcNow;
        var startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfYear = new DateTime(now.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        
        var meetings = await _context.Meetings
            .Where(m => m.StartDateTime >= startOfYear && m.StartDateTime <= endOfYear)
            .OrderBy(m => m.StartDateTime)
            .Select(m => new
            {
                id = m.Id,
                title = m.Title,
                time = m.StartDateTime.ToString("HH:mm"),
                date = m.StartDateTime.ToString("yyyy-MM-dd"),
                startTime = DateTime.SpecifyKind(m.StartDateTime, DateTimeKind.Utc),
                location = m.Location ?? "غير محدد",
                // Count attendees from comma-separated string
                participants = string.IsNullOrEmpty(m.Attendees) 
                    ? 0 
                    : m.Attendees.Split(',', StringSplitOptions.RemoveEmptyEntries).Length,
                platform = m.IsOnline ? "Online" : "حضوري",
                isOnline = m.IsOnline,
                meetingLink = m.OnlineMeetingLink,
                status = m.StartDateTime.Date == DateTime.UtcNow.Date ? "Today" : "Scheduled"
            })
            .ToListAsync();

        // 3. Get user info if userId provided
        object? userInfo = null;
        object? statsInfo = null;

        if (userId.HasValue)
        {
            var user = await _context.Users
                .Where(u => u.Id == userId.Value)
                .Select(u => new { u.Id, u.Username, u.FullName, u.Role })
                .FirstOrDefaultAsync();

            if (user != null)
            {
                userInfo = new
                {
                    name = user.FullName ?? user.Username,
                    role = user.Role.ToString(),
                    id = user.Id
                };

                // Get stats for this user
                var todayMeetings = meetings.Count(m => m.status == "Today");
                var pendingInbounds = await _context.Inbounds.CountAsync(i => i.Status == CorrespondenceStatus.New);
                var processedInbounds = await _context.Inbounds.CountAsync(i => i.Status != CorrespondenceStatus.New);

                statsInfo = new
                {
                    meetingsToday = todayMeetings,
                    pendingIssues = pendingInbounds,
                    completedReports = processedInbounds
                };
            }
        }

        return new
        {
            user = userInfo ?? new { name = "زائر", role = "Guest", id = 0 },
            correspondences = inbounds,
            meetings = meetings,
            stats = statsInfo ?? new { meetingsToday = 0, pendingIssues = 0, completedReports = 0 }
        };
    }
}

public class MobileLoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
