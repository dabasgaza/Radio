using DataAccess.Common;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Radio.Models
{
    public enum NavCategory
    {
        Broadcast,
        System
    }

    public class NavigationItem : INotifyPropertyChanged
    {
        private bool _isChecked;
        private bool _isVisible = true;

        public string Label { get; set; } = string.Empty;
        public PackIconKind Icon { get; set; }
        public string? Route { get; set; }
        public string? RequiredPermission { get; set; }
        public NavCategory? Category { get; set; }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public static class NavigationBuilder
    {
        public static ObservableCollection<NavigationItem> CreateMainNavigation()
        {
            return new ObservableCollection<NavigationItem>
            {
                new NavigationItem
                {
                    Label = "لوحة التحكم",
                    Icon = PackIconKind.ViewDashboardOutline,
                    Route = "Home",
                    Category = null
                },
                new NavigationItem
                {
                    Label = "البرامج",
                    Icon = PackIconKind.TelevisionGuide,
                    Route = "Programs",
                    RequiredPermission = AppPermissions.ProgramManage,
                    Category = NavCategory.Broadcast
                },
                new NavigationItem
                {
                    Label = "الحلقات",
                    Icon = PackIconKind.PlayCircleOutline,
                    Route = "Episodes",
                    RequiredPermission = AppPermissions.EpisodeManage,
                    Category = NavCategory.Broadcast
                },
                new NavigationItem
                {
                    Label = "الضيوف",
                    Icon = PackIconKind.AccountVoice,
                    Route = "Guests",
                    RequiredPermission = AppPermissions.GuestManage,
                    Category = NavCategory.Broadcast
                },
                new NavigationItem
                {
                    Label = "المراسلون",
                    Icon = PackIconKind.MapMarkerRadiusOutline,
                    Route = "Correspondents",
                    RequiredPermission = AppPermissions.CoordinationManage,
                    Category = NavCategory.Broadcast
                },
                new NavigationItem
                {
                    Label = "التقارير",
                    Icon = PackIconKind.FileChartOutline,
                    Route = "Reports",
                    RequiredPermission = AppPermissions.ViewReports,
                    Category = NavCategory.Broadcast
                },
                new NavigationItem
                {
                    Label = "تغطيات المراسلين",
                    Icon = PackIconKind.MapMarkerMultipleOutline,
                    Route = "Coverages",
                    RequiredPermission = AppPermissions.CoordinationManage,
                    Category = NavCategory.Broadcast
                },
                new NavigationItem
                {
                    Label = "سجلات النشر",
                    Icon = PackIconKind.FileDocumentMultiple,
                    Route = "PublishingRecords",
                    RequiredPermission = AppPermissions.EpisodePublish,
                    Category = NavCategory.Broadcast
                },
                new NavigationItem
                {
                    Label = "منصات التواصل",
                    Icon = PackIconKind.ShareVariantOutline,
                    Route = "SocialPlatforms",
                    RequiredPermission = AppPermissions.StaffManage,
                    Category = NavCategory.Broadcast
                },
                new NavigationItem
                {
                    Label = "المستخدمين",
                    Icon = PackIconKind.AccountKeyOutline,
                    Route = "Users",
                    RequiredPermission = AppPermissions.UserManage,
                    Category = NavCategory.System
                },
                new NavigationItem
                {
                    Label = "الموظفين",
                    Icon = PackIconKind.AccountTieOutline,
                    Route = "Employees",
                    RequiredPermission = AppPermissions.StaffManage,
                    Category = NavCategory.System
                },
                new NavigationItem
                {
                    Label = "المسميات الوظيفية",
                    Icon = PackIconKind.BriefcaseOutline,
                    Route = "StaffRoles",
                    RequiredPermission = AppPermissions.StaffManage,
                    Category = NavCategory.System
                },
                new NavigationItem
                {
                    Label = "الأدوار الأمنية",
                    Icon = PackIconKind.ShieldAccountOutline,
                    Route = "SecurityRoles",
                    RequiredPermission = AppPermissions.UserManage,
                    Category = NavCategory.System
                },
                new NavigationItem
                {
                    Label = "مصفوفة الصلاحيات",
                    Icon = PackIconKind.Matrix,
                    Route = "PermissionMatrix",
                    RequiredPermission = AppPermissions.UserManage,
                    Category = NavCategory.System
                },
                new NavigationItem
                {
                    Label = "قاعدة البيانات",
                    Icon = PackIconKind.Database,
                    Route = "Database",
                    RequiredPermission = AppPermissions.DatabaseManage,
                    Category = NavCategory.System
                },
                new NavigationItem
                {
                    Label = "سجل العمليات",
                    Icon = PackIconKind.History,
                    Route = "AuditLogs",
                    RequiredPermission = AppPermissions.ViewAuditLogs,
                    Category = NavCategory.System
                },
                new NavigationItem
                {
                    Label = "التشخيصات",
                    Icon = PackIconKind.HeartPulse,
                    Route = "Diagnostics",
                    RequiredPermission = AppPermissions.DatabaseManage,
                    Category = NavCategory.System
                }
            };
        }

        public static ObservableCollection<NavigationItem> CreateBottomNavigation()
        {
            return new ObservableCollection<NavigationItem>
            {
                new NavigationItem
                {
                    Label = "الرئيسية",
                    Icon = PackIconKind.ViewDashboardOutline,
                    Route = "Home"
                },
                new NavigationItem
                {
                    Label = "الحلقات",
                    Icon = PackIconKind.PlayCircleOutline,
                    Route = "Episodes",
                    RequiredPermission = AppPermissions.EpisodeManage
                },
                new NavigationItem
                {
                    Label = "النشر",
                    Icon = PackIconKind.FileDocumentMultiple,
                    Route = "PublishingRecords",
                    RequiredPermission = AppPermissions.EpisodePublish
                },
                new NavigationItem
                {
                    Label = "النظام",
                    Icon = PackIconKind.CogOutline,
                    Route = "AdminHub",
                    RequiredPermission = AppPermissions.UserManage
                }
            };
        }

        public static ObservableCollection<NavigationItem> CreateAdminNavigation()
        {
            return new ObservableCollection<NavigationItem>
            {
                new NavigationItem { Label = "المستخدمين", Icon = PackIconKind.AccountKeyOutline, Route = "Users", RequiredPermission = AppPermissions.UserManage },
                new NavigationItem { Label = "الموظفين", Icon = PackIconKind.AccountTieOutline, Route = "Employees", RequiredPermission = AppPermissions.StaffManage },
                new NavigationItem { Label = "المسميات الوظيفية", Icon = PackIconKind.BriefcaseOutline, Route = "StaffRoles", RequiredPermission = AppPermissions.StaffManage },
                new NavigationItem { Label = "الأدوار الأمنية", Icon = PackIconKind.ShieldAccountOutline, Route = "SecurityRoles", RequiredPermission = AppPermissions.UserManage },
                new NavigationItem { Label = "مصفوفة الصلاحيات", Icon = PackIconKind.Matrix, Route = "PermissionMatrix", RequiredPermission = AppPermissions.UserManage },
                new NavigationItem { Label = "قاعدة البيانات", Icon = PackIconKind.Database, Route = "Database", RequiredPermission = AppPermissions.DatabaseManage },
                new NavigationItem { Label = "سجل العمليات", Icon = PackIconKind.History, Route = "AuditLogs", RequiredPermission = AppPermissions.ViewAuditLogs },
                new NavigationItem { Label = "التشخيصات", Icon = PackIconKind.HeartPulse, Route = "Diagnostics", RequiredPermission = AppPermissions.DatabaseManage }
            };
        }
    }
}
