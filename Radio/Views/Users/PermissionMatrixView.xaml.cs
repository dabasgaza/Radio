using DataAccess.Services;
using Domain.Models;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Users
{
    /// <summary>
    /// Interaction logic for PermissionMatrixView.xaml
    /// </summary>
    public partial class PermissionMatrixView
    {
        private readonly IUserService _userService; // سنضيف دوال الصلاحيات لخدمة المستخدم
        private readonly UserSession _session;
        private Role? _selectedRole;

        // Caller must provide the current UserSession
        public PermissionMatrixView(IUserService userService, UserSession session)
        {
            InitializeComponent();
            _userService = userService;
            _session = session;
            _ = LoadRoles();
        }

        private async Task LoadRoles() => LstRoles.ItemsSource = await _userService.GetRolesAsync();

        private async void LstRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstRoles.SelectedItem is Role role)
            {
                _selectedRole = role;
                // جلب كل الصلاحيات مع تحديد ما يملكه هذا الدور حالياً
                var allPermissions = await _userService.GetPermissionsMatrixAsync(role.RoleId);
                ItemsPermissions.ItemsSource = allPermissions;
            }
        }

        private async void BtnSavePermissions_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRole == null) return;

            // تجميع الصلاحيات المختارة (IsChecked == true)
            var selectedIds = new List<int>();
            foreach (var item in ItemsPermissions.Items)
            {
                if (item is PermissionViewModel vm && vm.IsAssigned)
                    selectedIds.Add(vm.PermissionId);
            }

            try
            {
                // pass the required UserSession argument
                await _userService.UpdateRolePermissionsAsync(_selectedRole.RoleId, selectedIds, _session);
                MessageBox.Show("تم تحديث الصلاحيات بنجاح. سيحتاج المستخدمون لإعادة تسجيل الدخول لتفعيلها.");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }

    // ViewModel بسيط للعرض في المصفوفة
    public class PermissionViewModel
    {
        public int PermissionId { get; set; }
        public string DisplayName { get; set; } = null!; // 👈 يجب أن تكون public
        public string Module { get; set; } = null!;      // 👈 يجب أن تكون public
        public bool IsAssigned { get; set; }             // 👈 يجب أن تكون public

    }
}
