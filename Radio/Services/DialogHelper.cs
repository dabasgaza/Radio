using DataAccess.Services.Messaging;
using System.Windows;

namespace Radio.Services
{
    /// <summary>
    /// خدمة مساعدة لعرض الحوارات — تفك الاعتماد بين Views و MainWindow.
    /// بدلاً من Window.GetWindow(this) as ModernMainWindow، يستخدم أي View هذه الخدمة.
    /// تتضمن حماية شاملة من أخطاء Owner و ShowDialog لمنع انهيار التطبيق.
    /// </summary>
    public class DialogHelper
    {
        /// <summary>
        /// حدث يُطلب من MainWindow إظهار الغطاء الشفاف (Overlay).
        /// </summary>
        public event Action? OverlayShowRequested;

        /// <summary>
        /// حدث يُطلب من MainWindow إخفاء الغطاء الشفاف (Overlay).
        /// </summary>
        public event Action? OverlayHideRequested;

        /// <summary>
        /// إظهار الغطاء الشفاف — يُستخدم قبل فتح حوار Modal.
        /// </summary>
        public Task ShowOverlayAsync()
        {
            OverlayShowRequested?.Invoke();
            return Task.CompletedTask;
        }

        /// <summary>
        /// إخفاء الغطاء الشفاف — يُستخدم بعد إغلاق الحوار Modal.
        /// </summary>
        public Task HideOverlayAsync()
        {
            OverlayHideRequested?.Invoke();
            return Task.CompletedTask;
        }

        /// <summary>
        /// الحصول على النافذة الرئيسية كـ Window عام — بدون اقتران بنوع ModernMainWindow.
        /// يُستخدم لتعيين Owner للحوار.
        /// يبحث أولاً في Application.Current.MainWindow، ثم يبحث في النوافذ المفتوحة
        /// عن نافذة مرئية وصالحة كـ Owner.
        /// </summary>
        public Window? GetMainWindow()
        {
            var app = Application.Current;
            if (app == null) return null;

            // 1) المحاولة الأولى: Application.Current.MainWindow
            var mainWin = app.MainWindow;
            if (IsValidOwner(mainWin))
                return mainWin;

            // 2) البحث في النوافذ المفتوحة عن نافذة صالحة
            foreach (Window w in app.Windows)
            {
                if (IsValidOwner(w))
                    return w;
            }

            return null;
        }

        /// <summary>
        /// عرض حوار مشروط مع تعيين Owner تلقائياً + إظهار/إخفاء Overlay.
        /// يُغني عن تكرار نمط ShowOverlayAsync / Owner / ShowDialog / HideOverlayAsync.
        /// يتضمن حماية شاملة: أي خطأ أثناء فتح الحوار يُعرض للمستخدم عبر MessageService
        /// بدلاً من انهيار التطبيق.
        /// </summary>
        public async Task<bool?> ShowDialogAsync(Window dialog)
        {
            try
            {
                SafeSetOwner(dialog);

                await ShowOverlayAsync();
                try
                {
                    return dialog.ShowDialog();
                }
                finally
                {
                    await HideOverlayAsync();
                }
            }
            catch (InvalidOperationException ex)
            {
                // أخطاء مثل: Cannot set Owner to a Window that has not been shown
                Serilog.Log.Error(ex, "خطأ في فتح الحوار: {Message}", ex.Message);
                MessageService.Current.ShowError($"تعذر فتح النافذة. يرجى المحاولة مرة أخرى.\n\nالتفاصيل: {ex.Message}");
                return null;
            }
            catch (ArgumentException ex)
            {
                // أخطاء مثل: Owner already closed أو Owner is in different thread
                Serilog.Log.Error(ex, "خطأ في تعيين Owner للحوار: {Message}", ex.Message);
                MessageService.Current.ShowError($"تعذر فتح النافذة. يرجى المحاولة مرة أخرى.\n\nالتفاصيل: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // أي خطأ آخر غير متوقع
                Serilog.Log.Error(ex, "خطأ غير متوقع أثناء فتح الحوار: {Message}", ex.Message);
                MessageService.Current.ShowError($"حدث خطأ غير متوقع أثناء فتح النافذة.\n\nالتفاصيل: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// عرض حوار مشروط بدون Overlay (مثل ReasonInputDialog).
        /// يُعين Owner فقط دون Overlay.
        /// يتضمن حماية شاملة من أخطاء Owner.
        /// </summary>
        public bool? ShowDialog(Window dialog)
        {
            try
            {
                SafeSetOwner(dialog);
                return dialog.ShowDialog();
            }
            catch (InvalidOperationException ex)
            {
                Serilog.Log.Error(ex, "خطأ في فتح الحوار: {Message}", ex.Message);
                MessageService.Current.ShowError($"تعذر فتح النافذة. يرجى المحاولة مرة أخرى.\n\nالتفاصيل: {ex.Message}");
                return null;
            }
            catch (ArgumentException ex)
            {
                Serilog.Log.Error(ex, "خطأ في تعيين Owner للحوار: {Message}", ex.Message);
                MessageService.Current.ShowError($"تعذر فتح النافذة. يرجى المحاولة مرة أخرى.\n\nالتفاصيل: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "خطأ غير متوقع أثناء فتح الحوار: {Message}", ex.Message);
                MessageService.Current.ShowError($"حدث خطأ غير متوقع أثناء فتح النافذة.\n\nالتفاصيل: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// تعيين Owner للحوار بطريقة آمنة — تتحقق من صلاحية النافذة قبل التعيين.
        /// إذا كانت النافذة غير صالحة، يترك Owner فارغاً بدلاً من رمي استثناء.
        /// </summary>
        public void SafeSetOwner(Window dialog)
        {
            var owner = GetMainWindow();
            if (owner != null && IsValidOwner(owner) && IsWindowShown(owner))
            {
                try
                {
                    dialog.Owner = owner;
                }
                catch (InvalidOperationException)
                {
                    // فشل تعيين Owner — لا مشكلة، الحوار سيعمل بدون Owner
                    Serilog.Log.Warning("فشل تعيين Owner للحوار — سيتم عرضه بدون نافذة أب");
                }
            }
        }

        /// <summary>
        /// فحص هل النافذة صالحة كـ Owner (مرئية وغير مغلقة ولها HWND).
        /// WPF يرفض تعيين Owner لنافذة لم تُعرض بعد أو أُغلقت.
        /// </summary>
        private static bool IsValidOwner(Window? window)
        {
            if (window == null) return false;
            try
            {
                // نافذة صالحة كـ Owner إذا كانت مرئية ومحمّلة
                return window.IsLoaded && window.Visibility == Visibility.Visible;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// فحص إضافي: هل النافذة عُرضت فعلاً؟
        /// WPF يرفض تعيين Owner لنافذة لم تُعرض بعد (لم تستدع Show() بعد).
        /// </summary>
        private static bool IsWindowShown(Window window)
        {
            try
            {
                // التحقق من وجود HWND — إذا وُجد فالنافذة عُرضت فعلاً
                var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
                return handle != IntPtr.Zero;
            }
            catch
            {
                return false;
            }
        }
    }
}
