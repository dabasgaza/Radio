using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Domain.Models;
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
                var correspondents = await _service.GetAllActiveAsync();

                DgCorrespondents.ItemsSource = correspondents;

                _allCorrespondents = correspondents.ToList();

                TxtTotal.Text = $"{_allCorrespondents.Count}";

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
                var result = await _service.SoftDeleteAsync(cor.CorrespondentId, _session);
                if (result.IsSuccess)
                {
                    await LoadDataAsync();
                    MessageService.Current.ShowSuccess($"تم حذف المراسل «{cor.FullName}» بنجاح.");
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشل الحذف.");
                }
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
        private List<CorrespondentDto>? _allCorrespondents;

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            string keyword = textBox.Text.Trim();

            // لا نبحث إذا لم تُحمّل البيانات بعد
            if (_allCorrespondents == null)
                return;

            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allCorrespondents
                : _allCorrespondents.Where(c =>
                    c.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            DgCorrespondents.ItemsSource = filtered.ToList();
        }
        #endregion
    }
}