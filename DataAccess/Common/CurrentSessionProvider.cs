namespace DataAccess.Common
{
    /// <summary>
    /// كلاس وحيد (Singleton) يحتفظ ببيانات المستخدم الحالي لكي تصل إليها قاعدة البيانات تلقائياً
    /// </summary>
    public class CurrentSessionProvider
    {
        public UserSession? CurrentSession { get; set; }
    }
}
