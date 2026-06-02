using System.Threading.Tasks; 
using System.Windows; 
using System.Windows.Media.Animation; 

namespace Ui.Themes { 
    public static class ThemeTransitionHelper { 
        public static async Task AnimateThemeSwitchAsync(FrameworkElement target, ThemeManager.ThemeMode mode, int fadeOutMs = 150, int fadeInMs = 200) { 
            if (target == null) { ThemeManager.ApplyTheme(mode); return; } 
            var fadeOut = new DoubleAnimation(1.0, 0.0, new Duration(System.TimeSpan.FromMilliseconds(fadeOutMs))) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } }; 
            target.BeginAnimation(UIElement.OpacityProperty, fadeOut); 
            await Task.Delay(fadeOutMs + 50); 
            ThemeManager.ApplyTheme(mode); 
            var fadeIn = new DoubleAnimation(0.0, 1.0, new Duration(System.TimeSpan.FromMilliseconds(fadeInMs))) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } }; 
            target.BeginAnimation(UIElement.OpacityProperty, fadeIn); 
            await Task.Delay(fadeInMs); 
        } 
        public static async Task AnimateDialogOpenAsync(FrameworkElement dialog, int durationMs = 250) { 
            dialog.Opacity = 0; 
            dialog.RenderTransform = new System.Windows.Media.ScaleTransform(0.95, 0.95, 0.5, 0.5); 
            dialog.RenderTransformOrigin = new Point(0.5, 0.5); 
            var fadeIn = new DoubleAnimation(0, 1, new Duration(System.TimeSpan.FromMilliseconds(durationMs))) { EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut } }; 
            var scaleUp = new DoubleAnimation(0.95, 1.0, new Duration(System.TimeSpan.FromMilliseconds(durationMs))) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.1 } }; 
            dialog.BeginAnimation(UIElement.OpacityProperty, fadeIn); 
            dialog.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleUp); 
            dialog.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleUp); 
            await Task.Delay(durationMs); 
        } 
    } 
}