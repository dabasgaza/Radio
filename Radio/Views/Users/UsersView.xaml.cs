using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Radio.Messaging;
using Radio.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Radio.Views.Users
{
    /// <summary>
    /// شاشة إدارة المستخدمين — تعرض قائمة المستخدمين مع إمكانية الإضافة والتعديل وتفعيل/تعطيل الحسابات.
    /// </summary>
    public partial class UsersView : UserControl
    {
        private readonly IUserService _userService;
        private readonly UserSession _session;
        private readonly DialogHelper _dialogHelper;
        private List<UserDto> _allUsers = new(); // ✨ تخزين مؤقت للبحث بدون إعادة تحميل

        public UsersView(IUserService userService, UserSession session, DialogHelper dialogHelper)
        {
            InitializeComponent();
            _userService = userService;
            _session = session;
            _dialogHelper = dialogHelper;

            // ✅ AppPermissions بدلاً من فحص IsAdmin مباشرة
            BtnAddUser.Visibility = _session.HasPermission(AppPermissions.UserManage)
                ? Visibility.Visible
                : Visibility.Collapsed;

            // ✅ Loaded بدلاً من Fire-and-Forget
            Loaded += async (_, _) => await LoadDataAsync();
        }

        #region Data Loading

        /// <summary>
        /// تحميل قائمة المستخدمين وربطهم بالـ DataGrid مع تحديث الإحصائيات.
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                _allUsers = users.ToList(); // ✨ تخزين مؤقت للبحث
                DgUsers.ItemsSource = _allUsers;
                UpdateStats();
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An unexpected error occurred during processing");
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تحميل قائمة المستخدمين.");
            }
        }

        /// <summary>
        /// ✨ فلترة قائمة المستخدمين بناءً على نص البحث.
        /// يبحث في: الاسم الكامل، اسم المستخدم، اسم الدور، البريد الإلكتروني.
        /// </summary>
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var keyword = TxtSearch.Text?.Trim().ToLower() ?? string.Empty;

            if (string.IsNullOrEmpty(keyword))
            {
                DgUsers.ItemsSource = _allUsers;
            }
            else
            {
                var filtered = _allUsers.Where(u =>
                    (u.FullName?.ToLower().Contains(keyword) == true) ||
                    (u.Username?.ToLower().Contains(keyword) == true) ||
                    (u.RoleName?.ToLower().Contains(keyword) == true) ||
                    (u.EmailAddress?.ToLower().Contains(keyword) == true)
                ).ToList();
                DgUsers.ItemsSource = filtered;
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// فتح نافذة إضافة مستخدم جديد.
        /// </summary>
        private async void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserFormDialog(null, _userService, _session);
            if (await _dialogHelper.ShowDialogAsync(dialog) == true)
            {
                MessageService.Current.ShowSuccess(Messages.Actioned("إضافة", "المستخدم"));
                await LoadDataAsync();
            }
        }

        /// <summary>
        /// فتح نافذة تعديل بيانات مستخدم موجود.
        /// </summary>
        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not UserDto user)
                return;

            var dialog = new UserFormDialog(user, _userService, _session);
            if (await _dialogHelper.ShowDialogAsync(dialog) == true)
            {
                MessageService.Current.ShowSuccess(Messages.Updated("المستخدم", user.FullName));
                await LoadDataAsync();
            }
        }

        #endregion

        #region Status Toggle

        /// <summary>
        /// تفعيل أو تعطيل حساب مستخدم مع مزامنة حالة الزر وعرض الرسالة المناسبة.
        /// </summary>
        private async void Status_Click(object sender, RoutedEventArgs e)
        {
            // ✅ DTO بدلاً من Entity
            if (sender is not ToggleButton toggle || toggle.DataContext is not UserDto user)
                return;

            bool newStatus = toggle.IsChecked ?? false;

            try
            {
                var result = await _userService.ToggleUserStatusAsync(user.UserId, newStatus, _session);
                if (result.IsSuccess)
                {
                    MessageService.Current.ShowSuccess(
                        newStatus
                            ? Messages.ActionedWithName("تفعيل", "المستخدم", user.FullName)
                            : Messages.ActionedWithName("تعطيل", "المستخدم", user.FullName));
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت العملية.");
                    toggle.IsChecked = !newStatus;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An unexpected error occurred during processing");
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تغيير حالة المستخدم.");
                toggle.IsChecked = !newStatus;
            }
            finally
            {
                await LoadDataAsync();
            }
        }

        #endregion

        #region Stats

        /// <summary>
        /// تحديث إحصائيات المستخدمين (الإجمالي / النشط / المعطّل).
        /// </summary>
        private void UpdateStats()
        {
            if (DgUsers.ItemsSource is not IEnumerable<UserDto> users)
                return;

            var list = users.ToList();
            TxtTotalUsers.Text = list.Count.ToString();
            TxtActiveUsers.Text = list.Count(u => u.IsActive).ToString();
            TxtInactiveUsers.Text = list.Count(u => !u.IsActive).ToString();
        }

        #endregion

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is UserDto user)
            {
                bool isConfirmed = await MessageService.Current.ShowConfirmationAsync(
                $"هل أنت متأكد من رغبتك بحذف المستخدم: {user.FullName}؟\nلا يمكن التراجع عن هذا الإجراء.",
                "تأكيد الحذف");

                if (!isConfirmed)
                    return;
                try
                {
                    var result = await _userService.DeleteUserAsync(user.UserId, _session);
                    if (result.IsSuccess)
                    {
                        await LoadDataAsync();
                        MessageService.Current.ShowSuccess(Messages.Deleted("المستخدم", user.FullName));
                    }
                    else
                    {
                        MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشل الحذف.");
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "An unexpected error occurred during processing");
                    MessageService.Current.ShowError("فشل الحذف: " + ex.Message);
                }
            }
        }
    }
}