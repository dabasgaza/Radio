using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Radio.Messaging;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Users
{
    public partial class SecurityRolesView
    {
        private readonly IUserService _userService;
        private readonly UserSession _session;

        public SecurityRolesView(IUserService userService, UserSession session)
        {
            InitializeComponent();
            _userService = userService;
            _session = session;
            Loaded += async (_, _) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var roles = await _userService.GetRolesAsync();
            DgRoles.ItemsSource = roles;
        }

        private async void BtnAddRole_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as ModernMainWindow;
            if (mainWindow != null) await mainWindow.ShowOverlay();

            try
            {
                var dialog = new RoleFormDialog(_userService, _session);
                dialog.Owner = mainWindow;
                dialog.ShowDialog();

                if (dialog.IsSaved)
                {
                    await LoadDataAsync();
                }
            }
            finally
            {
                if (mainWindow != null) await mainWindow.HideOverlay();
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: RoleDto dto })
            {
                var mainWindow = Window.GetWindow(this) as ModernMainWindow;
                if (mainWindow != null) await mainWindow.ShowOverlay();

                try
                {
                    var dialog = new RoleFormDialog(_userService, _session, dto);
                    dialog.Owner = mainWindow;
                    dialog.ShowDialog();

                    if (dialog.IsSaved)
                    {
                        await LoadDataAsync();
                    }
                }
                finally
                {
                    if (mainWindow != null) await mainWindow.HideOverlay();
                }
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: RoleDto dto })
            {
                if (MessageBox.Show($"هل أنت متأكد من حذف الدور '{dto.RoleName}'؟", 
                    "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    var result = await _userService.DeleteRoleAsync(dto.RoleId, _session);
                    if (result.IsSuccess)
                    {
                        MessageService.Current.ShowSuccess("تم حذف الدور بنجاح");
                        await LoadDataAsync();
                    }
                    else
                    {
                        MessageService.Current.ShowError(result.ErrorMessage ?? "فشل الحذف");
                    }
                }
            }
        }

        private void BtnManagePermissions_Click(object sender, RoutedEventArgs e)
        {
            // هذا الزر سينقلك لمصفوفة الصلاحيات
            // في تطبيقنا، المصفوفة تعرض كل الأدوار، ولكن يمكننا تحسينها مستقبلاً لتركز على دور واحد
            if (sender is Button { DataContext: RoleDto dto })
            {
                 // سنقوم بمجرد فتح شاشة مصفوفة الصلاحيات
                 // (يمكن تطوير NavigationService لاحقاً ليدعم تمرير Parameters)
                 var modernWindow = Window.GetWindow(this) as ModernMainWindow;
                 modernWindow?.NavigateToView("PermissionMatrix");
            }
        }
    }
}
