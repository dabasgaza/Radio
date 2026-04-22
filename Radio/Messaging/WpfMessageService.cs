using DataAccess.Services.Messaging;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Radio.Messaging;

public class WpfMessageService : IMessageService
{
    #region Toast Notifications

    public void ShowSuccess(string message, string title = "نجاح")
        => NotificationManager.Show(NotificationType.Success, title, message);

    public void ShowError(string message, string title = "خطأ")
        => NotificationManager.Show(NotificationType.Error, title, message);

    public void ShowWarning(string message, string title = "تحذير")
        => NotificationManager.Show(NotificationType.Warning, title, message);

    public void ShowInfo(string message, string title = "معلومة")
        => NotificationManager.Show(NotificationType.Info, title, message);

    #endregion

    #region Confirmation Dialog

    public async Task<bool> ShowConfirmationAsync(string message, string title = "تأكيد")
    {
        try
        {
            var view = new StackPanel
            {
                Margin = new Thickness(25),
                MinWidth = 300,
                FlowDirection = FlowDirection.RightToLeft
            };

            var titleBrush = new SolidColorBrush(Color.FromRgb(0x30, 0x3F, 0x9F));
            var bodyBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x21, 0x21));

            view.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = titleBrush
            });

            view.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 25),
                Foreground = bodyBrush
            });

            var btns = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            var btnNo = new Button
            {
                Content = "إلغاء",
                Style = (Style)Application.Current.FindResource("MaterialDesignOutlinedButton"),
                Margin = new Thickness(0, 0, 10, 0),
                Command = DialogHost.CloseDialogCommand,
                CommandParameter = false,
            };

            var btnYes = new Button
            {
                Content = "نعم، متأكد",
                Style = (Style)Application.Current.FindResource("MaterialDesignRaisedButton"),
                Margin = new Thickness(0, 0, 10, 0),
                Command = DialogHost.CloseDialogCommand,
                CommandParameter = true,
            };

            btns.Children.Add(btnYes);
            btns.Children.Add(btnNo);
            view.Children.Add(btns);

            var result = await DialogHost.Show(view);
            return result is bool boolResult && boolResult;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}