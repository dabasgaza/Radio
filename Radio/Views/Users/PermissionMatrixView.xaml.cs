using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Users
{
    /// <summary>
    /// شاشة مصفوفة الصلاحيات — تسمح بعرض وتعديل صلاحيات الأدوار.
    /// </summary>
    public partial class PermissionMatrixView
    {
        private readonly IUserService _userService;
        private readonly UserSession _session;
        private RoleDto? _selectedRole;

        public PermissionMatrixView(IUserService userService, UserSession session)
        {
            InitializeComponent();
            _userService = userService;
            _session = session;

            IsWindowDraggable = true;

            Loaded += async (_, _) => await LoadRolesAsync();
        }

        #region Data Loading

        /// <summary>
        /// تحميل قائمة الأدوار وربطها بالـ ListBox.
        /// </summary>
        private async Task LoadRolesAsync()
        {
            try
            {
                LstRoles.ItemsSource = await _userService.GetRolesAsync();
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError("ليس لديك صلاحية لعرض الأدوار.");
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تحميل الأدوار.");
            }
        }

        #endregion

        #region Role Selection

        /// <summary>
        /// عند اختيار دور — تحميل صلاحياته وعرضها في المصفوفة.
        /// </summary>
        private async void LstRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstRoles.SelectedItem is not RoleDto role)
                return;

            _selectedRole = role;

            try
            {
                var allPermissions = await _userService.GetPermissionsMatrixAsync(role.RoleId);
                ItemsPermissions.ItemsSource = allPermissions;
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError("ليس لديك صلاحية لعرض صلاحيات هذا الدور.");
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تحميل صلاحيات الدور.");
            }
        }

        #endregion

        #region Save

        /// <summary>
        /// حفظ الصلاحيات المحددة للدور الحالي.
        /// </summary>
        private async void BtnSavePermissions_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRole is null)
            {
                MessageService.Current.ShowWarning("يرجى اختيار دور أولاً.");
                return;
            }

            var selectedIds = ItemsPermissions.Items
                .OfType<PermissionViewModel>()
                .Where(vm => vm.IsAssigned)
                .Select(vm => vm.PermissionId)
                .ToList();

            if (selectedIds.Count == 0)
            {
                MessageService.Current.ShowWarning("لم يتم اختيار أي صلاحية. إذا أردت إزالة جميع الصلاحيات، يرجى التواصل مع مسؤول النظام.");
                return;
            }

            try
            {
                BtnSavePermissions.IsEnabled = false;

                await _userService.UpdateRolePermissionsAsync(
                    _selectedRole.RoleId, selectedIds, _session);

                MessageService.Current.ShowSuccess(
                    "تم تحديث صلاحيات الدور بنجاح. سيحتاج المستخدمون لإعادة تسجيل الدخول لتفعيلها.");
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError("ليس لديك صلاحية لتعديل صلاحيات الأدوار.");
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حفظ الصلاحيات.");
            }
            finally
            {
                BtnSavePermissions.IsEnabled = true;
            }
        }

        #endregion

        #region UI Events

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                DragMove();
        }

        #endregion
    }
}