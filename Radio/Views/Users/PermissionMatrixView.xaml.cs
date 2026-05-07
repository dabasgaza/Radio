using DataAccess.Common;
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
    public partial class PermissionMatrixView : UserControl
    {
        private readonly IUserService _userService;
        private readonly UserSession _session;
        private RoleDto? _selectedRole;

        public PermissionMatrixView(IUserService userService, UserSession session)
        {
            InitializeComponent();
            _userService = userService;
            _session = session;

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

                var result = await _userService.UpdateRolePermissionsAsync(
                    _selectedRole.RoleId, selectedIds, _session);

                if (result.IsSuccess)
                {
                    MessageService.Current.ShowSuccess(
                        "تم تحديث صلاحيات الدور بنجاح. سيحتاج المستخدمون لإعادة تسجيل الدخول لتفعيلها.");
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشل تحديث الصلاحيات.");
                }
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
    }
}