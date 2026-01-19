using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DCMS.Web.Controllers;

public class AccountController : Controller
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;

    public AccountController(IDbContextFactory<DCMSDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToAction("Index", "MobileHub");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // SIMPLE AUTH FOR MOBILE HUB (Improve as needed with real hashing)
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        
        // NOTE: In a real app, use PasswordHasher. For this HUB, we'll assume the user exists
        if (user != null && user.PasswordHash == password) // Placeholder validation
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("FullName", user.FullName ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = rememberMe };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
            
            return RedirectToAction("Index", "MobileHub");
        }

        ViewBag.Error = "بيانات الاعتماد غير صحيحة";
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
