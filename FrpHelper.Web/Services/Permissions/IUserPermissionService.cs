using FrpHelper.Web.Models;

namespace FrpHelper.Web.Services.Permissions;

public interface IUserPermissionService
{
    UserPermissionRow? CurrentPermissions { get; }

    bool IsAdmin { get; }

    bool CanViewPool { get; }

    bool CanUploadPool { get; }

    bool CanEditReports { get; }

    event Action<UserPermissionRow?>? PermissionChanged;

    Task<SupabaseOperationResult> EnsureCurrentUserRecordAsync(CancellationToken cancellationToken = default);

    Task<UserPermissionRow?> RefreshMyPermissionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserPermissionRow>> GetAllUsersAsync(int limit = 500, CancellationToken cancellationToken = default);

    Task<SupabaseOperationResult> UpdateUserPermissionsAsync(UserPermissionRow row, CancellationToken cancellationToken = default);
}