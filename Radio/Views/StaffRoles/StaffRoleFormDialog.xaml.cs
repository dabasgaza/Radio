using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Windows;

namespace Radio.Views.StaffRoles
{
    /// <summary>
    /// نافذة إضافة/تعديل دور وظيفي — حقل واحد: اسم الدور.
    /// </summary>
    public partial class StaffRoleFormDialog
    {
        private readonly IEmployeeService _employeeService;
        private readonly UserSession _session;
        private readonly int _roleId;

        public StaffRoleFormDialog(IEmployeeService employeeService, UserSession session, int roleId)
        {
            InitializeComponent();
            _employeeService = employeeService;
            _session = session;
            _roleId = roleId;

            IsWindowDraggable = true;
            Title = _roleId == 0 ? "إضافة دور وظيفي" : "تعديل الدور الوظيفي";

            if (_roleId != 0)
                Loaded += async (_, _) => await LoadRoleAsync();
        }

        private async Task LoadRoleAsync()
        {
            var roles = await _employeeService.GetAllRolesAsync();
            var role = roles.FirstOrDefault(r => r.StaffRoleId == _roleId);
            if (role != null)
                TxtRoleName.Text = role.RoleName;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtRoleName.Text))
            {
                MessageService.Current.ShowWarning("اسم الدور مطلوب.");
                return;
            }

            var dto = new StaffRoleDto(_roleId, TxtRoleName.Text.Trim());

            try
            {
                BtnSave.IsEnabled = false;

                Result result;
                if (_roleId == 0)
                    result = await _employeeService.CreateRoleAsync(dto, _session);
                else
                    result = await _employeeService.UpdateRoleAsync(dto, _session);

                if (result.IsSuccess)
                {
                    MessageService.Current.ShowSuccess(
                        _roleId == 0 ? "تمت إضافة الدور بنجاح." : "تم تعديل الدور بنجاح.");
                    DialogResult = true;
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت العملية.");
                }
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع.");
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}