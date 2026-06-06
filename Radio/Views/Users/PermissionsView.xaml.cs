using DataAccess.Services;
using DataAccess.Services.Messaging;

namespace Radio.Views.Users
{
    /// <summary>
    /// شاشة عرض الصلاحيات — للقراءة فقط.
    /// الصلاحيات تُعرَّف في AppPermissions وتُزامَن تلقائياً مع قاعدة البيانات عند تشغيل التطبيق.
    /// </summary>
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
                DgPermissions.ItemsSource = result.Value;
            else
                MessageService.Current.ShowError("فشل تحميل قائمة الصلاحيات");
        }
    }
}
