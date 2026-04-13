namespace DataAccess.Common
{
    /// <summary>
    /// يحتوي على كافة ثوابت الصلاحيات لضمان عدم وجود أخطاء إملائية في الكود
    /// </summary>
    public static class AppPermissions
    {
        public const string UserManage = "USER_MANAGE";
        public const string ProgramManage = "PROGRAM_MANAGE";
        public const string EpisodeManage = "EPISODE_MANAGE";
        public const string EpisodeExecute = "EPISODE_EXECUTE";
        public const string EpisodePublish = "EPISODE_PUBLISH";
        public const string GuestManage = "GUEST_MANAGE";
        public const string CoordinationManage = "CORR_MANAGE";
        public const string ViewReports = "VIEW_REPORTS";
    }

}
