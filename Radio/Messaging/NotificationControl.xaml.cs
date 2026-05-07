using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Radio.Messaging
{
    public partial class NotificationControl : UserControl
    {
        private readonly DispatcherTimer _closeTimer;
        private readonly TimeSpan _duration = TimeSpan.FromSeconds(4);
        private bool _isExiting; // ── علم حماية لمنع استدعاء AnimateExit مرتين ──

        public NotificationControl(NotificationType type, string title, string message)
        {
            InitializeComponent();
            TxtTitle.Text = title;
            TxtMessage.Text = message;
            ApplyTheme(type);
            AnimateEntrance();

            _closeTimer = new DispatcherTimer { Interval = _duration };
            _closeTimer.Tick += (_, _) => AnimateExit();
            _closeTimer.Start();

            Loaded += (_, _) => AnimateProgress();
        }

        private void ApplyTheme(NotificationType type)
        {
            //  النوع ← (أيقونة, خلفية البطاقة, لون الحدود, لون العنوان, لون الرسالة, لون الأيقونة)
            var (icon, bgColor, borderColor, titleColor, msgColor, iconColor) = type switch
            {
                NotificationType.Success => ("CheckCircle",  "#E8F5E9", "#A5D6A7", "#1B5E20", "#2E7D32", "#2E7D32"),
                NotificationType.Error   => ("CloseCircle",  "#FFEBEE", "#EF9A9A", "#B71C1C", "#C62828", "#C62828"),
                NotificationType.Warning => ("AlertCircle",  "#FFF3E0", "#FFCC80", "#E65100", "#BF360C", "#E65100"),
                NotificationType.Info    => ("Information",   "#E3F2FD", "#90CAF9", "#0D47A1", "#1565C0", "#1565C0"),
                _ => ("Information", "#E3F2FD", "#90CAF9", "#0D47A1", "#1565C0", "#1565C0")
            };

            Icon.Kind = Enum.Parse<MaterialDesignThemes.Wpf.PackIconKind>(icon);

            var bgBrush     = (SolidColorBrush)new BrushConverter().ConvertFrom(bgColor)!;
            var borderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(borderColor)!;
            var titleBrush  = (SolidColorBrush)new BrushConverter().ConvertFrom(titleColor)!;
            var msgBrush    = (SolidColorBrush)new BrushConverter().ConvertFrom(msgColor)!;
            var iconBrush   = (SolidColorBrush)new BrushConverter().ConvertFrom(iconColor)!;

            // خلفية البطاقة بلون الحالة
            RootBorder.Background = bgBrush;
            RootBorder.BorderBrush = borderBrush;

            // النصوص بألوان متوافقة مع الخلفية
            TxtTitle.Foreground   = titleBrush;
            TxtMessage.Foreground = msgBrush;

            // الأيقونة داخل دائرة بيضاء
            IconCircle.Background = new SolidColorBrush(Colors.White);
            Icon.Foreground       = iconBrush;

            // زر الإغلاق بلون الحالة
            CloseIcon.Foreground = iconBrush;

            // شريط التقدم بلون داكن للحالة
            ProgressBar.Background = iconBrush;
        }

        private void AnimateEntrance()
        {
            RenderTransform = new TranslateTransform(-60, 0);

            var slideIn = new DoubleAnimation
            {
                From = -60, To = 0,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            if (RenderTransform is TranslateTransform t)
                t.BeginAnimation(TranslateTransform.XProperty, slideIn);

            BeginAnimation(OpacityProperty, fadeIn);
        }

        private void AnimateExit()
        {
            // ── منع الاستدعاء المزدوج: المؤقت وشريط التقدم يتنافسان على نفس الدالة ──
            if (_isExiting) return;
            _isExiting = true;

            _closeTimer.Stop();

            var slideOut = new DoubleAnimation
            {
                From = 0, To = 80,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var fadeOut = new DoubleAnimation
            {
                From = 1, To = 0,
                Duration = TimeSpan.FromMilliseconds(250)
            };

            fadeOut.Completed += (_, _) =>
            {
                (Parent as Panel)?.Children.Remove(this);
            };

            if (RenderTransform is TranslateTransform t)
                t.BeginAnimation(TranslateTransform.XProperty, slideOut);

            BeginAnimation(OpacityProperty, fadeOut);
        }

        private void AnimateProgress()
        {
            var actualWidth = ActualWidth > 0 ? ActualWidth : 380;

            var animation = new DoubleAnimation
            {
                From = actualWidth, To = 0,
                Duration = _duration
            };
            animation.Completed += (_, _) => AnimateExit();

            ProgressBar.Width = actualWidth;
            ProgressBar.BeginAnimation(WidthProperty, animation);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            AnimateExit();
        }
    }
}
