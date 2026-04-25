namespace DataAccess.Common;

public static class SecurityHelper
{
    // 1. تحسين أداء البحث باستخدام HashSet بدلاً من Array.Contains
    public static void EnsureRole(this UserSession session, params string[] allowedRoles)
    {
        // ✨ صلاحية العبور المطلقة للـ Admin
        if (session.RoleName == "Admin") return;

        // البحث في الـ HashSet أسرع بكثير
        var allowedRolesSet = new HashSet<string>(allowedRoles);
        if (!allowedRolesSet.Contains(session.RoleName))
        {
            throw new UnauthorizedAccessException($"عذراً، الدور '{session.RoleName}' غير مسموح له بإجراء هذه العملية.");
        }
    }

    // 2. إصلاح الثغرة الأمنية: الـ Admin يجب أن يتجاوز فحص الصلاحيات
    public static void EnsurePermission(this UserSession session, string permissionName)
    {
        // ✨ إضافة شرط الـ Admin هنا أيضاً!
        if (session.RoleName == "Admin") return;

        if (!session.HasPermission(permissionName))
        {
            throw new UnauthorizedAccessException($"عذراً، لا تملك صلاحية ({permissionName}) لإتمام هذه العملية.");
        }
    }
}