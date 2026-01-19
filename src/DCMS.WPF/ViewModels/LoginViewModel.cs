using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DCMS.WPF.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly DCMSDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly Services.CurrentUserService _currentUserService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;

    public LoginViewModel(DCMSDbContext context, IServiceProvider serviceProvider, Services.CurrentUserService currentUserService)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _currentUserService = currentUserService;
        LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
    }

    public string Username
    {
        get => _username;
        set
        {
            SetProperty(ref _username, value);
            ErrorMessage = string.Empty;
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            SetProperty(ref _password, value);
            ErrorMessage = string.Empty;
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand LoginCommand { get; }

    private bool CanExecuteLogin(object? parameter)
    {
        return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password) && !IsLoading;
    }

    private async void ExecuteLogin(object? parameter)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            // Retry logic for Neon auto-wake (up to 3 attempts)
            const int maxRetries = 3;
            int retryCount = 0;
            Domain.Entities.User? user = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == Username);
                    break; // Success, exit retry loop
                }
                catch (Npgsql.NpgsqlException ex) when (retryCount < maxRetries - 1)
                {
                    retryCount++;
                    ErrorMessage = $"جاري إيقاظ قاعدة البيانات... محاولة {retryCount + 1}/{maxRetries}";
                    await Task.Delay(2000 * retryCount); // Exponential backoff: 2s, 4s
                }
            }

            if (user == null)
            {
                ErrorMessage = "اسم المستخدم غير صحيح";
                return;
            }

            // Hash the entered password and compare
            var hashedPassword = HashPassword(Password);
            if (user.PasswordHash != hashedPassword) 
            {
                ErrorMessage = "كلمة المرور غير صحيحة";
                return;
            }

            if (!user.IsActive)
            {
                ErrorMessage = "هذا الحساب غير نشط";
                return;
            }

            // Login successful - Set current user
            _currentUserService.SetCurrentUser(user);
            
            // Trigger auto-backup for Admin users only (fire and forget)
            if (user.Role == Domain.Enums.UserRole.Admin)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var backupService = _serviceProvider.GetRequiredService<Services.BackupService>();
                        await backupService.CreateAutoBackupAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Auto-backup error: {ex.Message}");
                    }
                });
            }
            
            // Open Main Window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            
            // Close login window
            System.Windows.Application.Current.Windows.OfType<Views.LoginView>().FirstOrDefault()?.Close();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ أثناء تسجيل الدخول: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
