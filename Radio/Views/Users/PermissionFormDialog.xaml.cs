using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Windows;

namespace Radio.Views.Users
{
    public partial class PermissionFormDialog
    {
        private readonly IPermissionService _permissionService;
        private readonly int? _permissionId;
        public bool IsSaved { get; private set; }

        public PermissionFormDialog(IPermissionService permissionService, PermissionDto? existingPermission = null)
        {
            InitializeComponent();
            _permissionService = permissionService;

            if (existingPermission != null)
            {
                _permissionId = existingPermission.PermissionId;
                TxtDialogTitle.Text = "تعديل تعريف الصلاحية";
                TxtSystemName.Text = existingPermission.SystemName;
                TxtDisplayName.Text = existingPermission.DisplayName;
                CmbModule.Text = existingPermission.Module;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtSystemName.Text) || 
                string.IsNullOrWhiteSpace(TxtDisplayName.Text) || 
                string.IsNullOrWhiteSpace(CmbModule.Text))
            {
                MessageService.Current.ShowWarning("الرجاء إكمال كافة الحقول الإجبارية");
                return;
            }

            var dto = new PermissionUpsertDto(
                TxtSystemName.Text.Trim().ToUpper(),
                TxtDisplayName.Text.Trim(),
                CmbModule.Text.Trim());

            BtnSave.IsEnabled = false;
            try
            {
                var result = _permissionId.HasValue
                    ? await _permissionService.UpdatePermissionAsync(_permissionId.Value, dto)
                    : await _permissionService.CreatePermissionAsync(dto);

                if (result.IsSuccess)
                {
                    IsSaved = true;
                    MessageService.Current.ShowSuccess("تم حفظ الصلاحية بنجاح");
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
