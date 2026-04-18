using DataAccess.Common;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Domain.Models;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Correspondents
{
    /// <summary>
    /// Interaction logic for CorrespondentsView.xaml
    /// </summary>
    public partial class CorrespondentsView : UserControl
    {
        private readonly ICorrespondentService _service;
        private readonly UserSession _session;

        public CorrespondentsView(ICorrespondentService service, UserSession session)
        {
            InitializeComponent();
            _service = service;
            _session = session;
            // استخدم الصلاحية التي عرفناها في جدول الصلاحيات
            BtnAdd.Visibility = _session.HasPermission(AppPermissions.CoordinationManage) ? Visibility.Visible : Visibility.Collapsed;
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            DgCorrespondents.ItemsSource = await _service.GetAllActiveAsync();
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CorrespondentFormDialog(null, _service, _session);
            if (dialog.ShowDialog() == true) await LoadDataAsync();
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Correspondent cor)
            {
                var dialog = new CorrespondentFormDialog(cor, _service, _session);
                if (dialog.ShowDialog() == true) await LoadDataAsync();
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Correspondent cor)
            {
                // 👈 استدعاء رسالة التأكيد بأسلوب أنيق جداً (ينتظر رد المستخدم)
                bool isConfirmed = await MessageService.Current.ShowConfirmationAsync(
                    $"هل أنت متأكد من رغبتك بحذف المراسل: {cor.FullName}؟\nلا يمكن التراجع عن هذا الإجراء.",
                    "تأكيد الحذف");

                if (isConfirmed)
                {
                    try
                    {
                        await _service.SoftDeleteAsync(cor.CorrespondentId, _session);

                        await LoadDataAsync();

                        // لن نرسل رسالة نجاح هنا، لأن الـ Service نفسه سيتولى إرسالها!
                    }
                    catch (Exception ex)
                    {
                        MessageService.Current.ShowError(ex.Message);
                    }
                }
            }

        }

        private void BtnCoverage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Correspondent cor)
            {
                // Placeholder: This would open a separate dialog to manage Coverage records
                MessageBox.Show($"إدارة تغطيات المراسل: {cor.FullName}");
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
