using DataAccess.Services.Messaging;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Messaging
{
    public class WpfMessageService : IMessageService
    {
        private readonly ISnackbarMessageQueue _messageQueue;

        public WpfMessageService(ISnackbarMessageQueue messageQueue)
        {
            _messageQueue = messageQueue;
        }

        // 1. الإشعارات العابرة (Snackbars)
        public void ShowSuccess(string message, string title = "نجاح")
            => EnqueueMessage($"✅  {title}: {message}");

        public void ShowError(string message, string title = "خطأ")
            => EnqueueMessage($"❌  {title}: {message}");

        public void ShowWarning(string message, string title = "تحذير")
            => EnqueueMessage($"⚠️  {title}: {message}");

        public void ShowInfo(string message, string title = "معلومة")
            => EnqueueMessage($"ℹ️  {title}: {message}");

        private void EnqueueMessage(string content)
        {
            // استخدام Dispatcher.Invoke لضمان عملها حتى لو استدعيت من Thread خلفي
            Application.Current.Dispatcher.Invoke(() => {
                _messageQueue.Enqueue(content, null, null, null, false, true, TimeSpan.FromSeconds(4));
            });
        }

        // 2. رسالة التأكيد (نعم/لا) باستخدام DialogHost
        public async Task<bool> ShowConfirmationAsync(string message, string title = "تأكيد")
        {
            var innerTask = await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var view = new StackPanel { Margin = new Thickness(25), MinWidth = 300 };

                view.Children.Add(new TextBlock { Text = title, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.Cyan, Margin = new Thickness(0, 0, 0, 15) });
                view.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 16, Foreground = System.Windows.Media.Brushes.White, Margin = new Thickness(0, 0, 0, 25) });

                var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

                var btnNo = new Button { Content = "إلغاء", Style = (Style)Application.Current.FindResource("MaterialDesignOutlinedButton"), Margin = new Thickness(10, 0, 0, 0), Command = DialogHost.CloseDialogCommand, CommandParameter = false, Foreground = System.Windows.Media.Brushes.Gray, BorderBrush = System.Windows.Media.Brushes.Gray };
                var btnYes = new Button { Content = "نعم، متأكد", Style = (Style)Application.Current.FindResource("MaterialDesignRaisedButton"), Margin = new Thickness(10, 0, 0, 0), Command = DialogHost.CloseDialogCommand, CommandParameter = true, Background = System.Windows.Media.Brushes.Cyan, Foreground = System.Windows.Media.Brushes.Black };

                btns.Children.Add(btnNo);
                btns.Children.Add(btnYes);
                view.Children.Add(btns);

                var result = await DialogHost.Show(view , "RootDialog");

                return result is bool boolResult && boolResult;
            });

            return await innerTask;
        }
    }
}
