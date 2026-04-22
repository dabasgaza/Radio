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
        private Storyboard? _progressStoryboard;
        private readonly TimeSpan _duration = TimeSpan.FromSeconds(4);
        public NotificationControl(NotificationType type, string title, string message)
        {
            InitializeComponent();
            TxtTitle.Text = title;
            TxtMessage.Text = message;
            ApplyTheme(type);
            AnimateEntrance();

            _closeTimer = new DispatcherTimer
            {
                Interval = _duration
            };
            _closeTimer.Tick += (_, _) => AnimateExit();
            _closeTimer.Start();
            AnimateProgress();
        }

        private void ApplyTheme(NotificationType type)
        {
            var (icon, accentColor, progressColor) = type switch
            {
                NotificationType.Success => ("CheckCircle", "#2E7D32", "#4CAF50"),
                NotificationType.Error => ("CloseCircle", "#C62828", "#EF5350"),
                NotificationType.Warning => ("AlertCircle", "#E65100", "#FFA726"),
                NotificationType.Info => ("Information", "#1565C0", "#42A5F5"),
                _ => ("Information", "#1565C0", "#42A5F5")
            };

            Icon.Kind = MaterialDesignThemes.Wpf.PackIconKind.CheckCircle;
            Icon.Kind = Enum.Parse<MaterialDesignThemes.Wpf.PackIconKind>(icon);

            var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(accentColor)!;
            var progressBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(progressColor)!;

            Icon.Foreground = brush;
            ProgressBar.Background = progressBrush;
            RootBorder.BorderBrush = progressBrush;
            TxtTitle.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#212121");
            TxtMessage.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#616161");

            // تأثير خلفية خفيف
            var bgBrush = new SolidColorBrush(Color.FromArgb(20,
                progressBrush.Color.R, progressBrush.Color.G, progressBrush.Color.B));
            RootBorder.Background = bgBrush;
        }

        private void AnimateEntrance()
        {
            var slideIn = new DoubleAnimation
            {
                From = -50,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            RenderTransform = new TranslateTransform(-50, 0);
            BeginAnimation(RenderTransformProperty, null);
            RenderTransform = new TranslateTransform();
            ((TranslateTransform)RenderTransform).BeginAnimation(TranslateTransform.XProperty, slideIn);

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            BeginAnimation(OpacityProperty, fadeIn);
        }

        private void AnimateExit()
        {
            _closeTimer.Stop();
            if (_progressStoryboard != null)
                _progressStoryboard.Stop();

            var slideOut = new DoubleAnimation
            {
                From = 0,
                To = 80,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250)
            };

            fadeOut.Completed += (_, _) =>
            {
                var parent = Parent as Panel;
                parent?.Children.Remove(this);
            };

            ((TranslateTransform)RenderTransform).BeginAnimation(TranslateTransform.XProperty, slideOut);
            BeginAnimation(OpacityProperty, fadeOut);
        }

        private void AnimateProgress()
        {
            var animation = new DoubleAnimation
            {
                From = 380,
                To = 0,
                Duration = _duration
            };
            animation.Completed += (_, _) => AnimateExit();

            ProgressBar.Width = 380;
            ProgressBar.BeginAnimation(WidthProperty, animation);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            AnimateExit();
        }
    }
}
