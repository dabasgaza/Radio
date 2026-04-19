using DataAccess.Services.Messaging;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Radio.Messaging;

public class WpfMessageService : IMessageService
{
    private readonly ISnackbarMessageQueue _messageQueue;

    public WpfMessageService(ISnackbarMessageQueue messageQueue)
    {
        _messageQueue = messageQueue;
    }

    // 1. الإشعارات العابرة (Snackbars)
    public void ShowSuccess(string message, string title = "نجاح")
        => EnqueueMessage($"✅ {title}: {message}");

    public void ShowError(string message, string title = "خطأ")
        => EnqueueMessage($"❌ {title}: {message}");

    public void ShowWarning(string message, string title = "تحذير")
        => EnqueueMessage($"⚠️ {title}: {message}");

    public void ShowInfo(string message, string title = "معلومة")
        => EnqueueMessage($"ℹ️ {title}: {message}");

    private void EnqueueMessage(string content)
    {
        // ضمان العمل على الـ UI Thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            _messageQueue.Enqueue(content, null, null, null, false, true, TimeSpan.FromSeconds(4));
        });
    }

    // 2. رسالة التأكيد (نعم/لا) باستخدام DialogHost
    public async Task<bool> ShowConfirmationAsync(string message, string title = "تأكيد")
    {
        // ✨ نقوم ببناء الواجهة على الـ UI Thread مباشرة لتجنب تعقيدات الـ Nested Async
        return await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            var view = new StackPanel
            {
                Margin = new Thickness(25),
                MinWidth = 300,
                FlowDirection = FlowDirection.RightToLeft // ✨ دعم RTL للعربية
            };

            // ✨ استخدام أنماط Material Design الديناميكية بدلاً من الألوان الثابتة
            view.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = (Brush)Application.Current.FindResource("PrimaryHueMidBrush") // يتكيف مع الثيم
            });

            view.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 25),
                Foreground = (Brush)Application.Current.FindResource("MaterialDesignBody") // يتكيف مع الثيم
            });

            var btns = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left, // ✨ Left في RTL يعني يمين الشاشة
            };

            var btnNo = new Button
            {
                Content = "إلغاء",
                Style = (Style)Application.Current.FindResource("MaterialDesignOutlinedButton"),
                Margin = new Thickness(0, 0, 10, 0), // تعديل الهامش ليناسب RTL
                Command = DialogHost.CloseDialogCommand,
                CommandParameter = false,
                Foreground = (Brush)Application.Current.FindResource("MaterialDesignBodyLight"),
                BorderBrush = (Brush)Application.Current.FindResource("MaterialDesignBodyLight")
            };

            var btnYes = new Button
            {
                Content = "نعم، متأكد",
                Style = (Style)Application.Current.FindResource("MaterialDesignRaisedButton"),
                Margin = new Thickness(0, 0, 10, 0),
                Command = DialogHost.CloseDialogCommand,
                CommandParameter = true,
            };

            btns.Children.Add(btnYes); // ✨ زر "نعم" أولاً (يمين) في RTL
            btns.Children.Add(btnNo);  // زر "إلغاء" ثانياً (يسار) في RTL
            view.Children.Add(btns);

            var result = await DialogHost.Show(view, "RootDialog");
            return result is bool boolResult && boolResult;
        }).Task.Unwrap(); // ✨ استخدام Unwrap لفك الـ Task الداخلي بسلاسة
    }
}