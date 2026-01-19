using System.Security.Claims;
using DCMS.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DCMS.Web.Services;

public class WebCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WebCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CurrentUserName => _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

    public string? CurrentUserFullName => _httpContextAccessor.HttpContext?.User?.FindFirstValue("FullName");

    public int? CurrentUserId => int.TryParse(_httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public bool IsLoggedIn => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string? CurrentUserRole => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
}
