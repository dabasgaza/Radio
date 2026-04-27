namespace DataAccess.Common
{
    /// <summary>
    /// يحتوي على كافة ثوابت الصلاحيات لضمان عدم وجود أخطاء إملائية في الكود
    /// </summary>
    public static class AppPermissions
    {
        // المستخدمين
        public const string UserManage = "USER_MANAGE";

        // البرامج
        public const string ProgramManage = "PROGRAM_MANAGE";

        // الحلقات
        public const string EpisodeManage = "EPISODE_MANAGE";       // إضافة + عرض
        public const string EpisodeExecute = "EPISODE_EXECUTE";     // تسجيل تنفيذ
        public const string EpisodePublish = "EPISODE_PUBLISH";     // نشر رقمي
        public const string EpisodeWebPublish = "EPISODE_WEB_PUBLISH"; // نشر الموقع
        public const string EpisodeEdit = "EPISODE_EDIT";           // تعديل
        public const string EpisodeDelete = "EPISODE_DELETE";       // حذف

        // الضيوف
        public const string GuestManage = "GUEST_MANAGE";

        // التنسيق الميداني (المراسلين)
        public const string CoordinationManage = "CORR_MANAGE";

        // التقارير
        public const string ViewReports = "VIEW_REPORTS";

    }

}
