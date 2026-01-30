using System.Text.Json;
using System.Text.Json.Serialization;
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
        try
        {
            var data = await GetAppDataAsync();
            ViewBag.AppData = JsonSerializer.Serialize(data, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            
            return View();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
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
        try
        {
            var data = await GetAppDataAsync(userId);
            return Json(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error fetching data: " + ex.Message, detail = ex.ToString() });
        }
    }

    private async Task<object> GetAppDataAsync(int? userId = null)
    {
        // 1. Fetch Latest Inbounds (Raw data)
        var inboundsRaw = await _context.Inbounds
            .Include(i => i.ResponsibleEngineers)
            .ThenInclude(re => re.Engineer)
            .OrderByDescending(i => i.InboundDate)
            .Take(200)
            .Select(i => new 
            {
                i.Id,
                i.Subject,
                i.SubjectNumber,
                i.Status,
                i.InboundDate,
                ResponsibleEngineer = i.ResponsibleEngineers
                    .Select(re => re.Engineer.FullName)
                    .FirstOrDefault() ?? i.ResponsibleEngineer ?? "غير محدد",
                i.Reply,
                i.FromEntity,
                i.CreatedAt,
                i.OriginalAttachmentUrl,
                i.AttachmentUrl,
                i.ReplyAttachmentUrl
            })
            .ToListAsync();

        var inbounds = inboundsRaw.Select(i => new MobileCorrespondenceDto
        {
            Id = i.Id,
            Subject = i.Subject,
            ReferenceNumber = i.SubjectNumber,
            Status = i.Status == CorrespondenceStatus.New ? "New" : "Processed",
            Date = i.InboundDate.ToString("yyyy-MM-dd"),
            ResponsibleEngineer = i.ResponsibleEngineer,
            Description = i.Reply ?? (i.FromEntity != null ? $"وارد من: {i.FromEntity}" : "لا توجد تفاصيل"),
            Category = "Inbound",
            CreatedAt = i.CreatedAt,
            SortDate = i.InboundDate,
            Attachments = new List<MobileAttachmentDto> { 
                new MobileAttachmentDto { Title = "المرفق الأصلي", Url = i.OriginalAttachmentUrl ?? i.AttachmentUrl, Type = "original" },
                new MobileAttachmentDto { Title = "مرفق الرد", Url = i.ReplyAttachmentUrl, Type = "reply" }
            }.Where(a => !string.IsNullOrEmpty(a.Url)).ToList()
        }).ToList();

        // 2. Fetch Latest Outbounds (Raw data)
        var outboundsRaw = await _context.Outbounds
            .OrderByDescending(o => o.OutboundDate)
            .Take(200)
            .Select(o => new 
            {
                o.Id,
                o.Subject,
                o.SubjectNumber,
                o.OutboundDate,
                o.ResponsibleEngineer,
                o.ToEntity,
                o.TransferredTo,
                o.CreatedAt,
                o.OriginalAttachmentUrl,
                o.ReplyAttachmentUrl,
                o.AttachmentUrls
            })
            .ToListAsync();

        var outbounds = outboundsRaw.Select(o => new MobileCorrespondenceDto
        {
            Id = o.Id,
            Subject = o.Subject,
            ReferenceNumber = o.SubjectNumber,
            Status = "Processed",
            Date = o.OutboundDate.ToString("yyyy-MM-dd"),
            ResponsibleEngineer = o.ResponsibleEngineer ?? "غير محدد",
            Description = $"صادر إلى: {o.ToEntity}" + (string.IsNullOrEmpty(o.TransferredTo) ? "" : $" \nمحول إلى: {o.TransferredTo}"),
            Category = "Outbound",
            CreatedAt = o.CreatedAt,
            SortDate = o.OutboundDate,
            Attachments = new List<MobileAttachmentDto> { 
                new MobileAttachmentDto { Title = "المرفق الأصلي", Url = o.OriginalAttachmentUrl, Type = "original" },
                new MobileAttachmentDto { Title = "مرفق الرد", Url = o.ReplyAttachmentUrl, Type = "reply" }
            }.Concat(o.AttachmentUrls.Select(url => new MobileAttachmentDto { Title = "مرفق إضافي", Url = url, Type = "other" }))
             .Where(a => !string.IsNullOrEmpty(a.Url)).ToList()
        }).ToList();

        // Combine, Sort by actual Date, and Take Top 400 total (200 of each or combined sorted)
        var correspondences = inbounds
            .Concat(outbounds)
            .OrderByDescending(c => c.SortDate)
            .Take(200)
            .ToList();

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
                startTime = m.StartDateTime,
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
            correspondences = correspondences,
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

public class MobileCorrespondenceDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = "";
    
    [JsonPropertyName("referenceNumber")]
    public string ReferenceNumber { get; set; } = "";
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
    
    [JsonPropertyName("date")]
    public string Date { get; set; } = "";
    
    [JsonPropertyName("responsibleEngineer")]
    public string ResponsibleEngineer { get; set; } = "";
    
    [JsonPropertyName("attachmentUrl")]
    public string? AttachmentUrl { get; set; }
    
    [JsonPropertyName("originalAttachmentUrl")]
    public string? OriginalAttachmentUrl { get; set; }
    
    [JsonPropertyName("replyAttachmentUrl")]
    public string? ReplyAttachmentUrl { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("category")]
    public string Category { get; set; } = "";
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [JsonIgnore]
    public DateTime SortDate { get; set; }
    
    [JsonPropertyName("attachments")]
    public List<MobileAttachmentDto> Attachments { get; set; } = new();
}

public class MobileAttachmentDto
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";
    
    [JsonPropertyName("url")]
    public string? Url { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
}
