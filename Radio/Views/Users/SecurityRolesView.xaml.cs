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
                    MessageService.Current.ShowSuccess(Messages.Actioned("إضافة", "الدور"));
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
MessageService.Current.ShowSuccess(Messages.Updated("الدور", dto.RoleName));
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
                var isConfirmed = await MessageService.Current.ShowConfirmationAsync(
                    $"هل أنت متأكد من حذف الدور '{dto.RoleName}'؟",
                    "تأكيد الحذف");

                if (isConfirmed)
                {
                    var result = await _userService.DeleteRoleAsync(dto.RoleId, _session);
                    if (result.IsSuccess)
                    {
                        MessageService.Current.ShowSuccess(Messages.Deleted("الدور", dto.RoleName));
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
