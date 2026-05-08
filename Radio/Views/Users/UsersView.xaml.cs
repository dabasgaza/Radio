using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
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

        public UsersView(IUserService userService, UserSession session)
        {
            InitializeComponent();
            _userService = userService;
            _session = session;

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
                DgUsers.ItemsSource = users;
                UpdateStats();
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تحميل قائمة المستخدمين.");
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// فتح نافذة إضافة مستخدم جديد.
        /// </summary>
        private async void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as ModernMainWindow;
            if (mainWindow != null) await mainWindow.ShowOverlay();

            try
            {
                var dialog = new UserFormDialog(null, _userService, _session);
                dialog.Owner = mainWindow;
                if (dialog.ShowDialog() == true)
                    await LoadDataAsync();
            }
            finally
            {
                if (mainWindow != null) await mainWindow.HideOverlay();
            }
        }

        /// <summary>
        /// فتح نافذة تعديل بيانات مستخدم موجود.
        /// </summary>
        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not UserDto user)
                return;

            var mainWindow = Window.GetWindow(this) as ModernMainWindow;
            if (mainWindow != null) await mainWindow.ShowOverlay();

            try
            {
                var dialog = new UserFormDialog(user, _userService, _session);
                dialog.Owner = mainWindow;
                if (dialog.ShowDialog() == true)
                    await LoadDataAsync();
            }
            finally
            {
                if (mainWindow != null) await mainWindow.HideOverlay();
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
                            ? $"تم تفعيل حساب المستخدم «{user.FullName}» بنجاح."
                            : $"تم تعطيل حساب المستخدم «{user.FullName}» بنجاح.");
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت العملية.");
                    toggle.IsChecked = !newStatus;
                }
            }
            catch (Exception)
            {
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
                        MessageService.Current.ShowSuccess($"تم حذف المستخدم «{user.FullName}» بنجاح.");
                    }
                    else
                    {
                        MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشل الحذف.");
                    }
                }
                catch (Exception ex)
                {
                    MessageService.Current.ShowError("فشل الحذف: " + ex.Message);
                }
            }
        }
    }
}