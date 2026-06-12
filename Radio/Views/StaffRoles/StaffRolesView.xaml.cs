using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Radio.Messaging;
using Radio.Services;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.StaffRoles
{
    /// <summary>
    /// شاشة إدارة المسميات الوظيفية — إضافة وتعديل وحذف الأدوار.
    /// </summary>
    public partial class StaffRolesView : UserControl
    {
        private readonly IEmployeeService _employeeService;
        private readonly UserSession _session;
        private readonly DialogHelper _dialogHelper;

        public StaffRolesView(IEmployeeService employeeService, UserSession session, DialogHelper dialogHelper)
        {
            InitializeComponent();
            _employeeService = employeeService;
            _session = session;
            _dialogHelper = dialogHelper;

            BtnAdd.Visibility = _session.HasPermission(AppPermissions.StaffManage)
                ? Visibility.Visible
                : Visibility.Collapsed;

            Loaded += async (_, _) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var roles = await _employeeService.GetAllRolesAsync();
                DgRoles.ItemsSource = roles;
                TxtTotalRoles.Text = roles.Count.ToString();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An unexpected error occurred during processing");
                MessageService.Current.ShowError("حدث خطأ أثناء تحميل الأدوار الوظيفية.");
            }
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new StaffRoleFormDialog(_employeeService, _session, roleId: 0);
            if (await _dialogHelper.ShowDialogAsync(dialog) == true)
            {
                MessageService.Current.ShowSuccess(Messages.Actioned("إضافة", "الدور"));
                await LoadDataAsync();
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            var role = btn.DataContext as StaffRoleDto;
            if (role == null) return;

            var dialog = new StaffRoleFormDialog(_employeeService, _session, role.StaffRoleId);
            if (await _dialogHelper.ShowDialogAsync(dialog) == true)
            {
                MessageService.Current.ShowSuccess(Messages.Updated("الدور", role.RoleName));
                await LoadDataAsync();
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            var role = btn.DataContext as StaffRoleDto;
            if (role == null) return;

            var confirmed = await MessageService.Current.ShowConfirmationAsync(
                $"هل أنت متأكد من حذف الدور: {role.RoleName}؟", "تأكيد الحذف");

            if (!confirmed) return;

            var result = await _employeeService.SoftDeleteRoleAsync(role.StaffRoleId, _session);
            if (result.IsSuccess)
            {
                await LoadDataAsync();
                MessageService.Current.ShowSuccess(Messages.Deleted("الدور", role.RoleName));
            }
            else
            {
                MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشل الحذف.");
            }
        }
    }
}