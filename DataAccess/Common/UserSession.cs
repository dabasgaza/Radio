public class UserSession
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();

    // 👈 التعديلات الجديدة
    public bool IsAdmin => RoleName == "آدمن";
    public bool IsCoordination => RoleName == "التنسيق";
    public bool IsProduction => RoleName == "الإنتاج";
    public bool IsPublishing => RoleName == "النشر الرقمي";

    // التحقق من الصلاحية: الأدمن يمر دائماً، وغيره يمر إذا ملك الصلاحية
    // ✅ الحل الجذري: الأدمن يملك كل الصلاحيات دائماً
    public bool HasPermission(string permissionName)
    {
        return RoleName == "Admin" || Permissions.Contains(permissionName);
    }
}
