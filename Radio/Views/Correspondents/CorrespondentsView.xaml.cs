using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Correspondents
{
    /// <summary>
    /// شاشة إدارة المراسلين — تعرض قائمة المراسلين النشطين مع إمكانية الإضافة والتعديل والحذف.
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

            BtnAdd.Visibility = _session.HasPermission(AppPermissions.CoordinationManage)
                ? Visibility.Visible
                : Visibility.Collapsed;

            Loaded += async (_, _) => await LoadDataAsync();
        }

        #region Data Loading

        /// <summary>
        /// تحميل جميع المراسلين النشطين وربطهم بالـ DataGrid.
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                DgCorrespondents.ItemsSource = await _service.GetAllActiveAsync();
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError("ليس لديك صلاحية لعرض المراسلين.");
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تحميل المراسلين.");
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// فتح نافذة إضافة مراسل جديد.
        /// </summary>
        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CorrespondentFormDialog(null, _service, _session);
            if (dialog.ShowDialog() == true)
                await LoadDataAsync();
        }

        /// <summary>
        /// فتح نافذة تعديل مراسل موجود.
        /// </summary>
        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not CorrespondentDto cor)
                return;

            var dialog = new CorrespondentFormDialog(cor, _service, _session);
            if (dialog.ShowDialog() == true)
                await LoadDataAsync();
        }

        /// <summary>
        /// حذف مراسل بشكل ناعم (Soft Delete) بعد تأكيد المستخدم.
        /// </summary>
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            // ✅ DTO بدلاً من Entity
            if (sender is not Button btn || btn.DataContext is not CorrespondentDto cor)
                return;

            bool isConfirmed = await MessageService.Current.ShowConfirmationAsync(
                $"هل أنت متأكد من رغبتك بحذف المراسل: {cor.FullName}؟\nلا يمكن التراجع عن هذا الإجراء.",
                "تأكيد الحذف");

            if (!isConfirmed)
                return;

            try
            {
                await _service.SoftDeleteAsync(cor.CorrespondentId, _session);
                await LoadDataAsync();
                MessageService.Current.ShowSuccess($"تم حذف المراسل «{cor.FullName}» بنجاح.");
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError("ليس لديك صلاحية لحذف المراسلين.");
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حذف المراسل.");
            }
        }

        #endregion

        #region Navigation

        /// <summary>
        /// فتح نافذة إدارة تغطيات المراسل المحدد.
        /// </summary>
        private void BtnCoverage_Click(object sender, RoutedEventArgs e)
        {
            // ✅ DTO بدلاً من Entity
            if (sender is not Button btn || btn.DataContext is not CorrespondentDto cor)
                return;

            MessageService.Current.ShowInfo(
                $"ميزة إدارة تغطيات المراسل: {cor.FullName} — قيد التطوير.");
        }

        #endregion

        #region Search

        /// <summary>
        /// البحث في قائمة المراسلين حسب الاسم.
        /// </summary>
        private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                await LoadDataAsync(); // إعادة تحميل البيانات إذا حدث خطأ في الوصول إلى TextBox
                return;
            }

            string keyword = textBox.Text.Trim();

            // ✅ DTO بدلاً من Entity
            if (DgCorrespondents.ItemsSource is IEnumerable<CorrespondentDto> correspondents)
            {
                var filtered = string.IsNullOrWhiteSpace(keyword)
                    ? correspondents
                    : correspondents.Where(c =>
                        c.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                DgCorrespondents.ItemsSource = filtered.ToList();
            }
        }

        #endregion
    }
}