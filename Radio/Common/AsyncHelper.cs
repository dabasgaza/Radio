using DataAccess.Services.Messaging;
using Serilog;

namespace Radio.Common
{
    /// <summary>
    /// غلاف أمان مركزي لعمليات async void في معالجات الأحداث.
    /// يمنع انهيار التطبيق بسبب استثناءات غير معالجة في async void،
    /// حيث أن أي استثناء غير معالج في async void يُرفع مباشرة إلى
    /// SynchronizationContext ويُنهي التطبيق.
    /// 
    /// الاستخدام:
    ///   private async void BtnSave_Click(object sender, RoutedEventArgs e)
    ///   {
    ///       await AsyncHelper.RunSafe(async () =>
    ///       {
    ///           // كود العملية غير المتزامنة هنا
    ///       }, "حفظ البيانات");
    ///   }
    /// </summary>
    public static class AsyncHelper
    {
        /// <summary>
        /// تنفيذ عملية غير متزامنة بأمان — يلتقط أي استثناء ويُظهر رسالة مناسبة للمستخدم.
        /// </summary>
        /// <param name="action">العملية غير المتزامنة المراد تنفيذها</param>
        /// <param name="operationName">اسم العملية بالعربية (مثل "حفظ البيانات") — يظهر في رسالة الخطأ</param>
        /// <param name="showErrorToUser">هل يُظهر رسالة خطأ للمستخدم؟ (الافتراضي: نعم)</param>
        public static async Task RunSafe(Func<Task> action, string operationName = "العملية", bool showErrorToUser = true)
        {
            try
            {
                await action();
            }
            catch (OperationCanceledException)
            {
                // إلغاء العملية ليس خطأ — تجاهله بصمت
                Log.Information("تم إلغاء {Operation}", operationName);
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ مع السياق الكامل
                Log.Error(ex, "فشل {Operation}: {Message}", operationName, ex.Message);

                // إظهار رسالة خطأ للمستخدم على مؤشر UI
                if (showErrorToUser)
                {
                    ShowErrorMessage(operationName, ex);
                }
            }
        }

        /// <summary>
        /// إظهار رسالة خطأ عبر نظام الإشعارات المركزي.
        /// NotificationManager يتولى التحقق من Dispatcher داخلياً،
        /// لذلك لا حاجة للتحقق من CheckAccess أو استدعاء Dispatcher.Invoke يدوياً.
        /// </summary>
        private static void ShowErrorMessage(string operationName, Exception ex)
        {
            try
            {
                var message = GetInnermostMessage(ex);

                // ✨ استخدام نظام الإشعارات المركزي بدلاً من MessageBox التقليدية
                // NotificationManager يتحقق من Dispatcher داخلياً
                MessageService.Current.ShowError(
                    $"فشل {operationName}.\n\n{message}");
            }
            catch (Exception showEx)
            {
                // إذا فشل إظهار الرسالة نفسه، سجّله فقط
                Log.Error(showEx, "فشل إظهار رسالة الخطأ للمستخدم");
            }
        }

        /// <summary>
        /// الحصول على الرسالة الداخلية الأكثر تحديداً من سلسلة الاستثناءات.
        /// DbUpdateException و AggregateException غالباً ما يكون لديهم رسائل أكثر تحديداً في الاستثناءات الداخلية.
        /// </summary>
        private static string GetInnermostMessage(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;

            // البحث عن الرسالة الأكثر تحديداً (حد أقصى 5 مستويات لمنع الحلقات اللانهائية)
            int depth = 0;
            while (inner != null && depth < 5)
            {
                if (!string.IsNullOrWhiteSpace(inner.Message) && inner.Message != message)
                {
                    message = inner.Message;
                }
                inner = inner.InnerException;
                depth++;
            }

            return message;
        }
    }
}
