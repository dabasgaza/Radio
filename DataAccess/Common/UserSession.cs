namespace DataAccess.Common;

/// <summary>
/// بيانات الجلسة النشطة للمستخدم — تُملأ عند تسجيل الدخول وتُحدَّث عند تغيير الصلاحيات
/// </summary>
public class UserSession
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// قائمة أسماء الصلاحيات الممنوحة للمستخدم عبر دوره (محمّلة عند تسجيل الدخول)
    /// </summary>
    public List<string> Permissions { get; set; } = [];

    /// <summary>
    /// يتحقق إن كان المستخدم يملك صلاحية معيّنة.
    /// المستخدم ذو دور "Admin" يملك جميع الصلاحيات تلقائياً.
    /// </summary>
    public bool HasPermission(string permissionName)
        => RoleName == "Admin" || Permissions.Contains(permissionName);
}
