using DataAccess.Services;
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
            BtnAdd.Visibility = _session.HasPermission("CORR_MANAGE") ? Visibility.Visible : Visibility.Collapsed;
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
                if (MessageBox.Show("حذف المراسل؟", "تأكيد", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    await _service.SoftDeleteAsync(cor.CorrespondentId, _session);
                    await LoadDataAsync();
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

    }
}
