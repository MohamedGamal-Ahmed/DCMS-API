using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using DCMS.Infrastructure.Data;
using System.Windows;
using System.IO;
using DCMS.Infrastructure.Services;
using DCMS.WPF.Services;

namespace DCMS.WPF;

public partial class App : System.Windows.Application
{
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public IConfiguration Configuration { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Global Exception Handling
        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Uncaught UI Exception: {args.Exception.Message}\n\n{args.Exception.StackTrace}", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            MessageBox.Show($"Uncaught Task Exception: {args.Exception.Message}\n\n{args.Exception.StackTrace}", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.SetObserved();
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show($"Fatal Error: {ex?.Message}\n\n{ex?.StackTrace}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        base.OnStartup(e);

        try
        {
            // Build configuration using multi-layered approach
            var builder = new ConfigurationBuilder();
            
            // 1. Load defaults from Embedded Resource
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("DCMS.WPF.appsettings.json"))
            {
                if (stream != null)
                {
                    builder.AddJsonStream(stream);
                }
                
                // 2. Load from appsettings.json on disk (if exists)
                builder.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                
                // 3. Load from appsettings.Local.json on disk (if exists)
                builder.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
                
                Configuration = builder.Build();
            }

            // Validate Connection String
            var connectionStringCheck = Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionStringCheck) || connectionStringCheck.Contains("YOUR_") || connectionStringCheck.Contains("Default_"))
            {
                MessageBox.Show("خطأ: لم يتم العثور على سلسلة اتصال قاعدة البيانات (Connection String).\n\nيرجى التأكد من إعداد ملف appsettings.Local.json بشكل صحيح.", 
                    "خطأ في الإعدادات", MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
                return;
            }

            // Configure services
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            // Apply pending database migrations and self-healing (Optimized: only run once per day or on version change)
            using (var scope = ServiceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DCMSDbContext>();
                try
                {
                    var lockFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".db_lock");
                    var currentVersion = assembly.GetName().Version?.ToString() ?? "1.1.9";
                    bool shouldRun = true;

                    if (File.Exists(lockFilePath))
                    {
                        var content = File.ReadAllText(lockFilePath).Split('|');
                        if (content.Length == 2)
                        {
                            var lastRunDate = content[0];
                            var lastVersion = content[1];
                            if (lastRunDate == DateTime.UtcNow.ToString("yyyyMMdd") && lastVersion == currentVersion)
                            {
                                shouldRun = false;
                            }
                        }
                    }

                    if (shouldRun && dbContext.Database.CanConnect())
                    {
                        dbContext.Database.Migrate();
                        
                        // Self-healing: Ensure required columns exist
                        var sql = @"
                            DO $$ 
                            BEGIN 
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
                        
                        // Save lock state
                        File.WriteAllText(lockFilePath, $"{DateTime.UtcNow:yyyyMMdd}|{currentVersion}");
                        System.Diagnostics.Debug.WriteLine("[MIGRATION] Database migrations and self-healing applied.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[MIGRATION] Scaled back: Migrations already run today or version hasn't changed.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MIGRATION ERROR] {ex.Message}");
                }
            }

            // Start Notification Service (Moved to MainWindow)
            // var notificationService = ServiceProvider.GetRequiredService<Services.NotificationService>();
            // notificationService.Start();

            // Check for updates (async)
            await CheckForUpdatesAsync();

            // Show login window
            var loginView = ServiceProvider.GetRequiredService<Views.LoginView>();
            loginView.Show();

            // Start Bot and SignalR (Async)
            _ = Task.Run(async () => 
            {
                var signalR = ServiceProvider.GetRequiredService<Services.SignalRService>();
                await signalR.ConnectAsync();
                
                var bot = ServiceProvider.GetRequiredService<Services.BotService>();
                await bot.StartAsync();
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في بدء التطبيق:\n{ex.Message}\n\n{ex.StackTrace}", 
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        var connectionString = Configuration.GetConnectionString("DefaultConnection");
        
        // Register Interceptors as singleton
        services.AddSingleton<Infrastructure.Interceptors.AuditInterceptor>();
        services.AddSingleton<Infrastructure.Interceptors.NotificationInterceptor>();
        
        // Add DbContext Factory for thread-safe usage in new ViewModels
        services.AddDbContextFactory<DCMSDbContext>((serviceProvider, options) =>
        {
            var auditInterceptor = serviceProvider.GetRequiredService<Infrastructure.Interceptors.AuditInterceptor>();
            var notificationInterceptor = serviceProvider.GetRequiredService<Infrastructure.Interceptors.NotificationInterceptor>();
            options.UseNpgsql(connectionString)
                   .AddInterceptors(auditInterceptor, notificationInterceptor);
        });
        
        // Also add scoped DbContext for backward compatibility with existing ViewModels
        services.AddDbContext<DCMSDbContext>((serviceProvider, options) =>
        {
            var auditInterceptor = serviceProvider.GetRequiredService<Infrastructure.Interceptors.AuditInterceptor>();
            var notificationInterceptor = serviceProvider.GetRequiredService<Infrastructure.Interceptors.NotificationInterceptor>();
            options.UseNpgsql(connectionString)
                   .AddInterceptors(auditInterceptor, notificationInterceptor);
        });

        // Add configuration
        services.AddSingleton(Configuration);

        // Add windows and view models
        services.AddTransient<MainWindow>();
        services.AddTransient<Views.LoginView>();
        services.AddTransient<ViewModels.LoginViewModel>();
        services.AddTransient<Views.DashboardView>();
        services.AddTransient<ViewModels.DashboardViewModel>();
        services.AddTransient<Views.InboundTypeSelectorView>();
        services.AddTransient<ViewModels.InboundTypeSelectorViewModel>();
        services.AddTransient<Views.PostaInboundView>();
        services.AddTransient<ViewModels.PostaInboundViewModel>();
        services.AddTransient<Views.EmailInboundView>();
        services.AddTransient<ViewModels.EmailInboundViewModel>();
        services.AddTransient<Views.RequestInboundView>();
        services.AddTransient<ViewModels.RequestInboundViewModel>();
        services.AddTransient<Views.MissionInboundView>();
        services.AddTransient<ViewModels.MissionInboundViewModel>();
        services.AddTransient<Views.ContractInboundView>();
        services.AddTransient<ViewModels.ContractInboundViewModel>();
        services.AddTransient<Views.PostaOutboundView>();
        services.AddTransient<ViewModels.PostaOutboundViewModel>();
        services.AddTransient<Views.MeetingAgendaView>();
        services.AddTransient<ViewModels.MeetingAgendaViewModel>();
        services.AddTransient<Views.AddMeetingDialog>();
        services.AddTransient<ViewModels.AddMeetingDialogViewModel>();
        services.AddTransient<Views.SearchAndFollowUpView>();
        services.AddTransient<ViewModels.SearchAndFollowUpViewModel>();
        services.AddTransient<Views.UserManagementView>();
        services.AddTransient<ViewModels.UserManagementViewModel>();
        services.AddTransient<Views.AuditLogView>();
        services.AddTransient<Views.AuditLogView>();
        services.AddTransient<ViewModels.AuditLogViewModel>();
        services.AddTransient<Views.BackupView>();
        services.AddTransient<ViewModels.BackupViewModel>();
        services.AddTransient<Views.ImportView>();
        services.AddTransient<ViewModels.ImportViewModel>();
        services.AddTransient<Views.ReportingView>();
        services.AddTransient<ViewModels.ReportingViewModel>();
        services.AddTransient<ViewModels.CodesManagerViewModel>();
        services.AddTransient<ViewModels.ExecutiveAnalysisViewModel>();
        services.AddTransient<Views.EngineerManagementView>();
        services.AddTransient<ViewModels.EngineerManagementViewModel>();

        // Services
        services.AddSingleton<Services.CurrentUserService>();
        services.AddSingleton<DCMS.Application.Interfaces.ICurrentUserService>(sp => sp.GetRequiredService<Services.CurrentUserService>());
        services.AddSingleton<Services.NotificationService>();
        services.AddSingleton<Services.ExcelExportService>();
        services.AddSingleton<Services.BackupService>();
        services.AddSingleton<DCMS.WPF.Services.UpdateService>();
        services.AddSingleton<DCMS.Application.Interfaces.IAiDashboardService, DCMS.Infrastructure.Services.AiDashboardService>();
        services.AddSingleton<DCMS.Application.Interfaces.IRecordNavigationService, Services.RecordNavigationService>();
        services.AddSingleton<DCMS.Infrastructure.Services.NumberingService>();
        services.AddSingleton<DCMS.WPF.Services.DatabaseExportService>();
        services.AddSingleton<DCMS.Application.Interfaces.ICorrespondenceImportService, Services.CorrespondenceImportService>();
        services.AddSingleton<DCMS.Application.Interfaces.IMeetingImportService, Services.MeetingImportService>();
        services.AddSingleton<Services.ExcelImportService>();
        services.AddSingleton<Services.ReportingService>();
        services.AddSingleton<DCMS.Application.Interfaces.IReportingService>(sp => sp.GetRequiredService<Services.ReportingService>());
        services.AddSingleton<Services.CodeLookupService>();
        services.AddSingleton<Services.IdleDetectorService>();
        services.AddSingleton<Services.DatabasePollingService>();
        services.AddSingleton<Services.RecentItemsService>();
        services.AddSingleton<Services.AiRoiReportService>();
        services.AddSingleton<DCMS.Infrastructure.Services.SearchQueryService>();
        services.AddSingleton<DCMS.Application.Interfaces.ISearchService, DCMS.Infrastructure.Services.SearchService>();
        services.AddSingleton<DCMS.Application.Interfaces.IEngineerService, DCMS.Infrastructure.Services.EngineerService>();
        services.AddSingleton<DCMS.Infrastructure.Services.SearchQueryService>();
        services.AddSingleton<DCMS.Infrastructure.Services.DashboardDataService>();
        services.AddMemoryCache(); // EMERGENCY: For dashboard caching
        services.AddSingleton<Services.DashboardCacheService>(); // EMERGENCY: Cache layer to stop DB hits
        services.AddSingleton<DCMS.Infrastructure.Services.DashboardAiService>();
        services.AddSingleton<ViewModels.RecentItemsViewModel>();
        services.AddTransient<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.GlobalSearchViewModel>();

        // AI Services
        services.AddSingleton<DCMS.Application.Interfaces.ICorrespondenceService, DCMS.Infrastructure.Services.CorrespondenceService>();
        services.AddSingleton<DCMS.Application.Interfaces.IMeetingService, DCMS.Infrastructure.Services.MeetingService>();
        services.AddSingleton<DCMS.Application.Interfaces.IAiHistoryService, DCMS.Infrastructure.Services.AiHistoryService>();
        services.AddSingleton<DCMS.Application.Interfaces.IAiContextService, DCMS.Infrastructure.Services.AiContextService>();
        services.AddSingleton<DCMS.Application.Interfaces.IAiService, DCMS.Infrastructure.Services.AiChatService>();
        services.AddSingleton<SignalRService>(sp =>
        {
            var hubUrl = Configuration["SignalR:HubUrl"] ?? "http://dcmschat.runasp.net/chatHub";
            var currentUserService = sp.GetRequiredService<DCMS.Application.Interfaces.ICurrentUserService>();
            return new SignalRService(hubUrl, currentUserService);
        });
        services.AddSingleton<BotService>();
        services.AddTransient<Views.AiChatView>();
        services.AddTransient<ViewModels.AiChatViewModel>();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            // Use local instance or resolve from DI if possible, but here we can just new it up or resolve
            // Since we built ServiceProvider, let's try to get it.
            var updateService = ServiceProvider.GetService<Services.UpdateService>();
            if (updateService == null) return;
            
            var updateInfo = await updateService.CheckForUpdatesAsync();

            if (updateInfo.IsUpdateAvailable)
            {
                var updateDialog = new Views.Dialogs.UpdateDialog(updateInfo);
                updateDialog.ShowDialog();
                // Mandatory update: if dialog closed (and app didn't restart via update), shutdown.
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
        }
    }
}
