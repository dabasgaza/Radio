namespace DataAccess.Common
{
    [AttributeUsage(AttributeTargets.Field)]
    public class PermissionInfoAttribute(string displayName, string module) : Attribute
    {
        public string DisplayName { get; } = displayName;
        public string Module { get; } = module;
    }

    /// <summary>
    /// يحتوي على كافة ثوابت الصلاحيات لضمان عدم وجود أخطاء إملائية في الكود
    /// </summary>
    public static class AppPermissions
    {
        // المستخدمين
        [PermissionInfo("إدارة المستخدمين", "المستخدمين")]
        public const string UserManage = "USER_MANAGE";

        // البرامج
        [PermissionInfo("إدارة البرامج", "البرامج")]
        public const string ProgramManage = "PROGRAM_MANAGE";

        // الحلقات
        [PermissionInfo("إدارة الحلقات", "الحلقات")]
        public const string EpisodeManage = "EPISODE_MANAGE";       // إضافة + عرض

        [PermissionInfo("تنفيذ الحلقات", "الحلقات")]
        public const string EpisodeExecute = "EPISODE_EXECUTE";     // تسجيل تنفيذ

        [PermissionInfo("نشر رقمي", "الحلقات")]
        public const string EpisodePublish = "EPISODE_PUBLISH";     // نشر رقمي

        [PermissionInfo("نشر الموقع", "الحلقات")]
        public const string EpisodeWebPublish = "EPISODE_WEB_PUBLISH"; // نشر الموقع

        [PermissionInfo("تعديل الحلقات", "الحلقات")]
        public const string EpisodeEdit = "EPISODE_EDIT";           // تعديل

        [PermissionInfo("حذف الحلقات", "الحلقات")]
        public const string EpisodeDelete = "EPISODE_DELETE";       // حذف

        [PermissionInfo("تراجع عن تنفيذ أو نشر", "الحلقات")]
        public const string EpisodeRevert = "EPISODE_REVERT";       // تراجع عن تنفيذ أو نشر

        // الضيوف
        [PermissionInfo("إدارة الضيوف", "الضيوف")]
        public const string GuestManage = "GUEST_MANAGE";

        // التنسيق الميداني (المراسلين)
        [PermissionInfo("إدارة التنسيق الميداني", "التنسيق")]
        public const string CoordinationManage = "CORR_MANAGE";

        // طاقم العمل
        [PermissionInfo("إدارة طاقم العمل", "طاقم العمل")]
        public const string StaffManage = "STAFF_MANAGE";

        // التقارير
        [PermissionInfo("عرض التقارير", "التقارير")]
        public const string ViewReports = "VIEW_REPORTS";

        // النظام
        [PermissionInfo("إدارة قاعدة البيانات", "النظام")]
        public const string DatabaseManage = "DATABASE_MANAGE";

        [PermissionInfo("عرض سجلات التدقيق", "النظام")]
        public const string ViewAuditLogs = "VIEW_AUDIT_LOGS";
    }
}

