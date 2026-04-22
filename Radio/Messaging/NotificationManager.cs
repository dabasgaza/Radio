using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Radio.Messaging
{
    /// <summary>
    /// مدير الإشعارات — ينشئ ويعرض إشعارات Toast في أعلى النافذة النشطة.
    /// </summary>
    public static class NotificationManager
    {
        private static Panel? _hostPanel;

        /// <summary>
        /// تسجيل لوحة الإشعارات من MainWindow أو أي نافذة.
        /// يجب استدعاؤها مرة واحدة عند تحميل النافذة الرئيسية.
        /// </summary>
        public static void RegisterHost(Panel panel)
        {
            _hostPanel = panel;
        }

        /// <summary>
        /// عرض إشعار Toast جديد.
        /// </summary>
        public static void Show(NotificationType type, string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var host = GetHost();
                if (host is null) return;

                var notification = new NotificationControl(type, title, message);

                // ✅ تداخل الإشعارات — كل جديد يظهر فوق السابق
                host.Children.Add(notification);

                // الحد الأقصى: 5 إشعارات
                while (host.Children.Count > 5)
                    host.Children.RemoveAt(0);
            });
        }

        /// <summary>
        /// البحث عن NotificationHost في النافذة النشطة.
        /// </summary>
        private static Panel? GetHost()
        {
            if (_hostPanel is not null)
                return _hostPanel;

            var window = Application.Current.Windows
                .OfType<Window>()
                .LastOrDefault(w => w.IsActive)
                ?? Application.Current.Windows.OfType<Window>().LastOrDefault();

            return window is null ? null : FindVisualChild<Panel>(window, "NotificationHost");
        }

        private static T? FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T element && element.Name == name)
                    return element;

                var found = FindVisualChild<T>(child, name);
                if (found is not null)
                    return found;
            }

            return null;
        }
    }

}
