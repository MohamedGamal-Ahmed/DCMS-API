namespace DCMS.Application.Interfaces;

/// <summary>
/// Interface for getting the current logged-in user information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Username of the currently logged-in user, or "System" if no user is logged in
    /// </summary>
    string CurrentUserName { get; }

    /// <summary>
    /// Full Name (Arabic) of the currently logged-in user
    /// </summary>
    string? CurrentUserFullName { get; }
    
    /// <summary>
    /// ID of the currently logged-in user, or null if no user is logged in
    /// </summary>
    int? CurrentUserId { get; }
    
    /// <summary>
    /// Indicates whether a user is currently logged in
    /// </summary>
    bool IsLoggedIn { get; }
    
    /// <summary>
    /// Role of the currently logged-in user as a string (Admin, TechnicalManager, FollowUpStaff)
    /// </summary>
    string? CurrentUserRole { get; }
}
