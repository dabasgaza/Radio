using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Guests
{
    /// <summary>
    /// شاشة إدارة الضيوف — تعرض قائمة الضيوف النشطين مع إمكانية الإضافة والتعديل والحذف والبحث.
    /// </summary>
    public partial class GuestsView : UserControl
    {
        private readonly IGuestService _guestService;
        private readonly UserSession _session;
        private List<GuestDto> _allGuests = [];

        public GuestsView(IGuestService guestService, UserSession session)
        {
            InitializeComponent();
            _guestService = guestService;
            _session = session;

            // ✅ AppPermissions بدلاً من نص ثابت
            BtnAddGuest.Visibility = _session.HasPermission(AppPermissions.GuestManage)
                ? Visibility.Visible
                : Visibility.Collapsed;

            // ✅ Loaded بدلاً من Fire-and-Forget
            Loaded += async (_, _) => await LoadDataAsync();
        }

        #region Data Loading

        /// <summary>
        /// تحميل جميع الضيوف النشطين وربطهم بالـ DataGrid.
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                _allGuests = (await _guestService.GetAllActiveAsync()).ToList();
                DgGuests.ItemsSource = _allGuests;
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تحميل الضيوف.");
            }
        }

        #endregion

        #region Search

        /// <summary>
        /// البحث في قائمة الضيوف حسب الاسم أو رقم الهاتف.
        /// </summary>
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            string keyword = textBox.Text.Trim();

            // ✅ StringComparison بدلاً من ToLower()
            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allGuests
                : _allGuests.Where(g =>
                    g.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (g.PhoneNumber?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));

            DgGuests.ItemsSource = filtered.ToList();
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// فتح نافذة إضافة ضيف جديد.
        /// </summary>
        private async void BtnAddGuest_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new GuestFormDialog(null, _guestService, _session);
            if (dialog.ShowDialog() == true)
                await LoadDataAsync();   // ✅ await بدلاً من fire-and-forget
        }

        /// <summary>
        /// فتح نافذة تعديل ضيف موجود.
        /// </summary>
        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            // ✅ DTO بدلاً من Entity
            if (sender is not Button btn || btn.DataContext is not GuestDto guest)
                return;

            var dialog = new GuestFormDialog(guest, _guestService, _session);
            if (dialog.ShowDialog() == true)
                await LoadDataAsync();   // ✅ await بدلاً من fire-and-forget
        }

        /// <summary>
        /// حذف ضيف بشكل ناعم (Soft Delete) بعد تأكيد المستخدم.
        /// </summary>
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            // ✅ DTO بدلاً من Entity
            if (sender is not Button btn || btn.DataContext is not GuestDto guest)
                return;

            bool isConfirmed = await MessageService.Current.ShowConfirmationAsync(
                $"هل أنت متأكد من رغبتك بحذف الضيف: {guest.FullName}؟\nلا يمكن التراجع عن هذا الإجراء.",
                "تأكيد الحذف");

            if (!isConfirmed)
                return;

            try
            {
                var result = await _guestService.SoftDeleteGuestAsync(guest.GuestId, _session);
                if (result.IsSuccess)
                {
                    await LoadDataAsync();
                    MessageService.Current.ShowSuccess($"تم حذف الضيف «{guest.FullName}» بنجاح.");
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشل الحذف.");
                }
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حذف الضيف.");
            }
        }

        #endregion
    }
}