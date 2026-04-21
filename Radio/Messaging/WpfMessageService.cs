using DataAccess.Services.Messaging;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Radio.Messaging;

public class WpfMessageService : IMessageService
{
    #region Snackbar Messages

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
        Application.Current.Dispatcher.Invoke(() =>
        {
            var snackbar = FindActiveSnackbar();
            snackbar?.MessageQueue?.Enqueue(content, null, null, null, false, true, TimeSpan.FromSeconds(4));
        });
    }

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

            // ✅ ألوان آمنة — تعمل داخل DialogHost Popup بغض النظر عن الثيم
            var titleBrush = new SolidColorBrush(Color.FromRgb(0x30, 0x3F, 0x9F)); // Indigo 700
            var bodyBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x21, 0x21)); // Dark Gray

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

    #region Visual Tree Helpers

    /// <summary>
    /// البحث عن Snackbar في النافذة النشطة حالياً.
    /// </summary>
    private static Snackbar? FindActiveSnackbar()
    {
        var window = Application.Current.Windows
            .OfType<Window>()
            .LastOrDefault(w => w.IsActive)
            ?? Application.Current.Windows.OfType<Window>().LastOrDefault();

        if (window is null) return null;
        return FindVisualChild<Snackbar>(window);
    }

    /// <summary>
    /// البحث عن DialogHost في النافذة النشطة حالياً.
    /// </summary>
    private static DialogHost? FindActiveDialogHost()
    {
        var window = Application.Current.Windows
            .OfType<Window>()
            .LastOrDefault(w => w.IsActive)
            ?? Application.Current.Windows.OfType<Window>().LastOrDefault();

        if (window is null) return null;
        return FindVisualChild<DialogHost>(window);
    }

    /// <summary>
    /// البحث العودي عن عنصر في شجرة العناصر المرئية.
    /// </summary>
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T result)
                return result;

            var found = FindVisualChild<T>(child);
            if (found is not null)
                return found;
        }

        return null;
    }

    #endregion
}