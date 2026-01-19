using DCMS.Domain.Enums;

namespace DCMS.Domain.Entities;

/// <summary>
/// Extension methods for User entity to check permissions
/// </summary>
public static class UserExtensions
{
    /// <summary>
    /// Check if user can manage (add/edit/delete) other users
    /// Only Admin can manage users
    /// </summary>
    public static bool CanManageUsers(this User user)
    {
        return user.Role == UserRole.Admin;
    }

    /// <summary>
    /// Check if user can delete inbound/outbound correspondence
    /// Admin and Office Manager can delete
    /// </summary>
    public static bool CanDeleteCorrespondence(this User user)
    {
        return user.Role == UserRole.Admin || user.Role == UserRole.OfficeManager;
    }

    /// <summary>
    /// Check if user can manage (add/edit/delete) engineers
    /// Admin and Office Manager can manage engineers
    /// </summary>
    public static bool CanManageEngineers(this User user)
    {
        return user.Role == UserRole.Admin || user.Role == UserRole.OfficeManager;
    }

    /// <summary>
    /// Check if user can add new inbound correspondence
    /// Admin and Follow-up Staff can add
    /// Technical Manager (Engineer) cannot add
    /// </summary>
    public static bool CanAddInbound(this User user)
    {
        return user.Role == UserRole.Admin || 
               user.Role == UserRole.FollowUpStaff ||
               user.Role == UserRole.OfficeManager;
    }

    /// <summary>
    /// Check if user can add new outbound correspondence
    /// Admin and Follow-up Staff can add
    /// Technical Manager (Engineer) cannot add
    /// </summary>
    public static bool CanAddOutbound(this User user)
    {
        return user.Role == UserRole.Admin || 
               user.Role == UserRole.FollowUpStaff ||
               user.Role == UserRole.OfficeManager;
    }

    /// <summary>
    /// Check if user is an Admin
    /// </summary>
    public static bool IsAdministrator(this User user)
    {
        return user.Role == UserRole.Admin;
    }

    /// <summary>
    /// Check if user is a Follow-up Staff member
    /// </summary>
    public static bool IsFollowUpStaff(this User user)
    {
        return user.Role == UserRole.FollowUpStaff;
    }

    /// <summary>
    /// Check if user is a Technical Manager (Engineer)
    /// </summary>
    public static bool IsTechnicalManager(this User user)
    {
        return user.Role == UserRole.TechnicalManager;
    }

    /// <summary>
    /// Check if user is an Office Manager
    /// </summary>
    public static bool IsOfficeManager(this User user)
    {
        return user.Role == UserRole.OfficeManager;
    }
}
