using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Users
{
    public partial class PermissionsView
    {
        private readonly IPermissionService _permissionService;

        public PermissionsView(IPermissionService permissionService)
        {
            InitializeComponent();
            _permissionService = permissionService;
            Loaded += async (_, _) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var result = await _permissionService.GetAllPermissionsAsync();
            if (result.IsSuccess)
            {
                DgPermissions.ItemsSource = result.Value;
            }
            else
            {
                MessageService.Current.ShowError("فشل تحميل قائمة الصلاحيات");
            }
        }

        private async void BtnAddPermission_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as ModernMainWindow;
            if (mainWindow != null) await mainWindow.ShowOverlay();

            try
            {
                var dialog = new PermissionFormDialog(_permissionService);
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
            if (sender is Button { DataContext: PermissionDto dto })
            {
                var mainWindow = Window.GetWindow(this) as ModernMainWindow;
                if (mainWindow != null) await mainWindow.ShowOverlay();

                try
                {
                    var dialog = new PermissionFormDialog(_permissionService, dto);
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
            if (sender is Button { DataContext: PermissionDto dto })
            {
                if (MessageBox.Show($"هل أنت متأكد من حذف الصلاحية '{dto.DisplayName}'؟\nسيؤدي ذلك إلى إزالتها من كافة الأدوار المرتبطة بها.", 
                    "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    var result = await _permissionService.DeletePermissionAsync(dto.PermissionId);
                    if (result.IsSuccess)
                    {
                        MessageService.Current.ShowSuccess("تم حذف الصلاحية بنجاح");
                        await LoadDataAsync();
                    }
                    else
                    {
                        MessageService.Current.ShowError(result.ErrorMessage ?? "فشل الحذف");
                    }
                }
            }
        }
    }
}
