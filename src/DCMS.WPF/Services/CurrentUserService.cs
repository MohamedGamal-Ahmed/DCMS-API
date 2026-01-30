using DCMS.Domain.Entities;
using DCMS.Application.Interfaces;

namespace DCMS.WPF.Services;

/// <summary>
/// Service to hold and provide access to the currently logged-in user
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private User? _currentUser;

    public User? CurrentUser
    {
        get => _currentUser;
        set => _currentUser = value;
    }
    
    public User GetCurrentUserOrThrow() => _currentUser ?? throw new InvalidOperationException("No user is currently logged in");

    public bool IsLoggedIn => _currentUser != null;

    public string CurrentUserName => _currentUser?.Username ?? "System";
    public string? CurrentUserFullName => _currentUser?.FullName; // Arabic full name for filtering
    public int? CurrentUserId => _currentUser?.Id;
    public string? CurrentUserRole => _currentUser?.Role.ToString();

    public void SetCurrentUser(User user)
    {
        _currentUser = user;
    }

    public void ClearCurrentUser()
    {
        _currentUser = null;
    }
}
