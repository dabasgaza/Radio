using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Radio.Messaging;
using Radio.Services;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Users
{
    public partial class SecurityRolesView
    {
        private readonly IUserService _userService;
        private readonly UserSession _session;
        private readonly NavigationService _navigationService;
        private readonly DialogHelper _dialogHelper;

        public SecurityRolesView(IUserService userService, UserSession session, NavigationService navigationService, DialogHelper dialogHelper)
        {
            InitializeComponent();
            _userService = userService;
            _session = session;
            _navigationService = navigationService;
            _dialogHelper = dialogHelper;
            Loaded += async (_, _) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var roles = await _userService.GetRolesAsync();
            DgRoles.ItemsSource = roles;
        }

        private async void BtnAddRole_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RoleFormDialog(_userService, _session);
            await _dialogHelper.ShowDialogAsync(dialog);

            if (dialog.IsSaved)
            {
                MessageService.Current.ShowSuccess(Messages.Actioned("إضافة", "الدور"));
                await LoadDataAsync();
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: RoleDto dto })
            {
                var dialog = new RoleFormDialog(_userService, _session, dto);
                await _dialogHelper.ShowDialogAsync(dialog);

                if (dialog.IsSaved)
                {
                    MessageService.Current.ShowSuccess(Messages.Updated("الدور", dto.RoleName));
                    await LoadDataAsync();
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
            if (sender is Button { DataContext: RoleDto dto })
            {
                // ✨ استخدام RequestNavigation بدلاً من NavigateTo مباشرة
                // NavigateTo يُعيد View لكن لا يُعرضه لأن MainWindow لا يعرف بالطلب
                // RequestNavigation يُطلق حدث يسمع MainWindow فيُنفذ التنقل الفعلي
                _navigationService.RequestNavigation("PermissionMatrix");
            }
        }
    }
}
