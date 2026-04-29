namespace DataAccess.Common;

public static class SecurityHelper
{
    public static Result EnsureRole(this UserSession session, params string[] allowedRoles)
    {
        if (session.RoleName == "Admin") return Result.Success();

        var allowedRolesSet = new HashSet<string>(allowedRoles);
        if (!allowedRolesSet.Contains(session.RoleName))
            return Result.Fail($"عذراً، الدور '{session.RoleName}' غير مسموح له بإجراء هذه العملية.");

        return Result.Success();
    }

    public static Result EnsurePermission(this UserSession session, string permissionName)
    {
        if (session.RoleName == "Admin") return Result.Success();

        if (!session.HasPermission(permissionName))
            return Result.Fail($"عذراً، لا تملك صلاحية ({permissionName}) لإتمام هذه العملية.");

        return Result.Success();
    }
}
