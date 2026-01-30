using DCMS.Infrastructure.Services;
using DCMS.Infrastructure.Data;
using DCMS.Web.Hubs;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Microsoft.AspNetCore.Authentication.Cookies;
using DCMS.Application.Interfaces;
using Telegram.Bot.Types.Enums;
using Npgsql;

// CRASH DIAGNOSTIC WRAPPER
try
{
    var builder = WebApplication.CreateBuilder(args);

    // Load appsettings.Local.json (for production secrets like connection strings)
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);


    // --- SAFE DB CONFIGURATION ---
    try 
    {
        var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
                                ?? builder.Configuration["DATABASE_URL"]
                                ?? builder.Configuration.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrEmpty(rawConnectionString))
        {
            string finalConnectionString;
            if (rawConnectionString.Contains("://"))
            {
                var uri = new Uri(rawConnectionString);
                var userInfo = uri.UserInfo.Split(':');
                var csBuilder = new NpgsqlConnectionStringBuilder
                {
                    Host = uri.Host,
                    Port = uri.Port > 0 ? uri.Port : 5432,
                    Username = userInfo[0],
                    Password = userInfo.Length > 1 ? userInfo[1] : null,
                    Database = uri.AbsolutePath.TrimStart('/'),
                    SslMode = SslMode.Require,
                    TrustServerCertificate = true,
                    Pooling = true
                };
                finalConnectionString = csBuilder.ToString();
            }
            else
            {
                var csBuilder = new NpgsqlConnectionStringBuilder(rawConnectionString);
                csBuilder.SslMode = SslMode.Require;
                csBuilder.TrustServerCertificate = true;
                csBuilder.Pooling = true;
                finalConnectionString = csBuilder.ToString();
            }
            builder.Services.AddDbContext<DCMSDbContext>(options => options.UseNpgsql(finalConnectionString));
            // Also add factory for Singleton services like DashboardDataService
            builder.Services.AddDbContextFactory<DCMSDbContext>(options => options.UseNpgsql(finalConnectionString), ServiceLifetime.Scoped);
        }
    }
    catch (Exception ex)
    {
        System.IO.File.WriteAllText("crash_log.txt", $"DB SETUP FAILED: {ex}");
        Console.WriteLine($"DB SETUP FAILED (Non-Fatal): {ex.Message}");
    }

    // --- SERVICES ---
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options => {
            options.LoginPath = "/Account/Login";
            options.Cookie.Name = "DCMS_Hub_Auth";
        });
    builder.Services.AddAuthorization();
    builder.Services.AddSingleton<DashboardDataService>();
    builder.Services.AddScoped<ICorrespondenceService, CorrespondenceService>();
    builder.Services.AddScoped<IMeetingService, MeetingService>();
    builder.Services.AddScoped<NumberingService>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, DCMS.Web.Services.WebCurrentUserService>(); 
    builder.Services.AddSignalR(); 
    builder.Services.AddControllersWithViews();
    builder.Services.AddCors(options => {
        options.AddPolicy("AllowAll", p => p
            .WithOrigins(
                "https://mohamedgamal-ahmed.github.io",
                "http://localhost:5173",
                "http://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });

    var app = builder.Build();

    // --- AUTO-MIGRATION & SCHEMA PATCHING ---
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<DCMSDbContext>();
        try
        {
            // Ensure DB is migrated
            if (dbContext.Database.GetPendingMigrations().Any())
            {
                dbContext.Database.Migrate();
            }

            // Explicitly patch columns if missing (Neon/MonsterASP self-healing)
            var sql = @"
                DO $$ 
                BEGIN 
                    CREATE SCHEMA IF NOT EXISTS dcms;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='outbound' AND column_name='original_attachment_url') THEN
                        ALTER TABLE dcms.outbound ADD COLUMN original_attachment_url TEXT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='outbound' AND column_name='reply_attachment_url') THEN
                        ALTER TABLE dcms.outbound ADD COLUMN reply_attachment_url TEXT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='meetings' AND column_name='online_meeting_link') THEN
                        ALTER TABLE dcms.meetings ADD COLUMN online_meeting_link TEXT;
                    END IF;
                END $$;";
            dbContext.Database.ExecuteSqlRaw(sql);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AUTO-MIGRATION ERROR] {ex.Message}");
        }
    }

    // --- MIDDLEWARE ---
    app.UseDeveloperExceptionPage();
    
    // Path rewriting MUST come before UseStaticFiles
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value;
        
        // If requesting /Mobile (without trailing slash or file), serve index.html
        if (path != null && path.Equals("/Mobile", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Path = "/Mobile/index.html";
        }
        
        await next();
    });
    
    app.UseStaticFiles();
    app.UseRouting();
    app.UseCors("AllowAll");
    app.UseAuthentication(); 
    app.UseAuthorization();

    app.MapHub<ChatHub>("/chatHub");
    app.MapControllerRoute(name: "mobile", pattern: "Mobile/{action=Index}/{id?}", defaults: new { controller = "MobileHub" });
    app.MapDefaultControllerRoute();

    app.MapGet("/", () => $"DCMS LIVE - {DateTime.UtcNow}");
    app.MapGet("/health", () => Results.Ok("healthy"));

    app.MapGet("/env-check", (IConfiguration config) => {
        var dbUrl = config["DATABASE_URL"] ?? Environment.GetEnvironmentVariable("DATABASE_URL");
        return Results.Ok(new {
            HasDbUrl = !string.IsNullOrEmpty(dbUrl),
            DbUrlLength = dbUrl?.Length ?? 0,
            Time = DateTime.UtcNow
        });
    });

    app.MapGet("/test-db", async ([Microsoft.AspNetCore.Mvc.FromServices] DCMSDbContext db) => {
        try {
            await db.Database.CanConnectAsync();
            var count = await db.Users.CountAsync();
            return Results.Ok($"✅ DB Success! Count: {count}");
        } catch (Exception ex) { return Results.Problem(ex.Message); }
    });

    app.Run();
}
catch (Exception ex)
{
    // Write crash log to file for diagnosis
    var crashInfo = $"CRASH TIME: {DateTime.UtcNow}\n\nEXCEPTION:\n{ex}\n\nSTACK TRACE:\n{ex.StackTrace}";
    System.IO.File.WriteAllText("crash_log.txt", crashInfo);
    throw;
}