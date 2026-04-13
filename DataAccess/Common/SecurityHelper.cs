namespace BroadcastWorkflow.Services;

public static class SecurityHelper
{
    public static void EnsureRole(UserSession session, params string[] allowedRoles)
    {
        // 1. إذا كان المستخدم Admin، اسمح له بالمرور فوراً دون فحص الصلاحيات
        if (session.RoleName == "Admin") return;

        if (!allowedRoles.Contains(session.RoleName))
            throw new UnauthorizedAccessException($"عذراً، الدور '{session.RoleName}' غير مسموح له بإجراء هذه العملية.");
    }

    public static void EnsurePermission(UserSession session, string permissionName)
    {
        if (!session.HasPermission(permissionName))
        {
            throw new UnauthorizedAccessException($"عذراً، لا تملك صلاحية ({permissionName}) لإتمام هذه العملية.");
        }

    }
}