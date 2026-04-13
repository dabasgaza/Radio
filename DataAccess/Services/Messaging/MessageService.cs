namespace DataAccess.Services.Messaging
{
    public interface IMessageService
    {
        void ShowSuccess(string message, string title = "نجاح");
        void ShowError(string message, string title = "خطأ");
        void ShowWarning(string message, string title = "تحذير");
        void ShowInfo(string message, string title = "معلومة");

        // 👈 رسالة التأكيد ترجع Task<bool> لأنها توقف التنفيذ لانتظار رد المستخدم
        Task<bool> ShowConfirmationAsync(string message, string title = "تأكيد");
    }

    /// <summary>
    /// نظام الإشعارات المركزي (Ambient Context)
    /// </summary>
    public static class MessageService
    {
        // الـ Default هو Null Object لمنع انهيار النظام إذا نسينا التهيئة
        public static IMessageService Current { get; private set; } = new NullMessageService();

        public static void Initialize(IMessageService service)
        {
            Current = service ?? throw new ArgumentNullException(nameof(service));
        }
    }

    /// <summary>
    /// تطبيق الـ Null Object Pattern
    /// </summary>
    public class NullMessageService : IMessageService
    {
        public void ShowSuccess(string message, string title = "نجاح") { }
        public void ShowError(string message, string title = "خطأ") { }
        public void ShowWarning(string message, string title = "تحذير") { }
        public void ShowInfo(string message, string title = "معلومة") { }
        public Task<bool> ShowConfirmationAsync(string message, string title = "تأكيد") => Task.FromResult(false);
    }

}
