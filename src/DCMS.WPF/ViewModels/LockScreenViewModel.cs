using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DCMS.WPF.Services;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DCMS.WPF.ViewModels;

public partial class LockScreenViewModel : ViewModelBase
{
    private readonly CurrentUserService _currentUserService;
    private readonly DatabasePollingService _databasePollingService;
    
    [ObservableProperty] private string _currentUsername = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
    
    public event EventHandler? UnlockSuccessful;
    public event EventHandler? SignOutRequested;

    public LockScreenViewModel(CurrentUserService currentUserService, DatabasePollingService databasePollingService)
    {
        _currentUserService = currentUserService;
        _databasePollingService = databasePollingService;
        
        CurrentUsername = _currentUserService.CurrentUser?.FullName 
                       ?? _currentUserService.CurrentUser?.Username 
                       ?? "مستخدم";
    }

    [RelayCommand]
    private void Unlock(object? parameter)
    {
        var passwordBox = parameter as PasswordBox;
        if (passwordBox == null) return;
        
        var enteredPassword = passwordBox.Password;
        
        if (string.IsNullOrWhiteSpace(enteredPassword))
        {
            ErrorMessage = "يرجى إدخال كلمة المرور";
            HasError = true;
            return;
        }
        
        // Verify password against current user
        var currentUser = _currentUserService.CurrentUser;
        if (currentUser == null)
        {
            SignOutRequested?.Invoke(this, EventArgs.Empty);
            return;
        }
        
        // Password verification using SHA256 hash (matches LoginViewModel)
        var hashedPassword = HashPassword(enteredPassword);
        bool isValid = hashedPassword == currentUser.PasswordHash;
        
        if (isValid)
        {
            HasError = false;
            ErrorMessage = string.Empty;
            passwordBox.Clear();
            
            // Resume database polling
            _databasePollingService.Resume();
            
            UnlockSuccessful?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            ErrorMessage = "كلمة المرور غير صحيحة";
            HasError = true;
            passwordBox.Clear();
        }
    }

    [RelayCommand]
    private void SignOut()
    {
        SignOutRequested?.Invoke(this, EventArgs.Empty);
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

