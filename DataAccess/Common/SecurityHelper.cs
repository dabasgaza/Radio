namespace BroadcastWorkflow.Services;

public static class SecurityHelper
{
    public static void EnsureRole(UserSession session, params string[] allowedRoles)
    {
        if (!allowedRoles.Contains(session.RoleName))
            throw new UnauthorizedAccessException($"Role '{session.RoleName}' is not permitted for this operation.");
    }

    public static void EnsurePermission(UserSession session, string permissionName)
    {
        if (!session.HasPermission(permissionName))
            throw new UnauthorizedAccessException($"عذراً، لا تملك صلاحية: {permissionName}");

    }
}