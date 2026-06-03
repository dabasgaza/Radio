using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Windows;

namespace Radio.Views.Users
{
    /// <summary>
    /// هذا الديالوج لم يعد مستخدماً — الصلاحيات تُدار بالكود عبر AppPermissions و DbSeeder.
    /// </summary>
    public partial class PermissionFormDialog
    {
        public bool IsSaved { get; private set; }

        public PermissionFormDialog(IPermissionService permissionService, PermissionDto? existingPermission = null)
        {
            InitializeComponent();
            MessageService.Current.ShowWarning("الصلاحيات تُدار تلقائياً ولا يمكن تعديلها يدوياً.");
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e) => Close();
        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
