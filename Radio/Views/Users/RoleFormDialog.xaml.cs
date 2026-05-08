using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Radio.Messaging;
using System.Windows;

namespace Radio.Views.Users
{
    public partial class RoleFormDialog
    {
        private readonly IUserService _userService;
        private readonly UserSession _session;
        private readonly int? _roleId;
        public bool IsSaved { get; private set; }

        public RoleFormDialog(IUserService userService, UserSession session, RoleDto? existingRole = null)
        {
            InitializeComponent();
            _userService = userService;
            _session = session;

            if (existingRole != null)
            {
                _roleId = existingRole.RoleId;
                TxtDialogTitle.Text = "تعديل تعريف الدور";
                TxtRoleName.Text = existingRole.RoleName;
                TxtDescription.Text = existingRole.RoleDescription;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtRoleName.Text))
            {
                MessageService.Current.ShowWarning("الرجاء إدخال اسم الدور");
                return;
            }

            var dto = new RoleDto
            {
                RoleId = _roleId ?? 0,
                RoleName = TxtRoleName.Text.Trim(),
                RoleDescription = TxtDescription.Text.Trim()
            };

            BtnSave.IsEnabled = false;
            try
            {
                var result = _roleId.HasValue
                    ? await _userService.UpdateRoleAsync(dto, _session)
                    : await _userService.CreateRoleAsync(dto, _session);

                if (result.IsSuccess)
                {
                    IsSaved = true;
                    MessageService.Current.ShowSuccess("تم حفظ الدور بنجاح");
                    Close();
                }
                else
                {
                    MessageService.Current.ShowError(result.ErrorMessage ?? "فشل الحفظ");
                }
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
