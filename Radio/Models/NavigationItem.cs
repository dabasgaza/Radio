using DataAccess.Common;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Radio.Models
{
    public class NavigationItem : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private bool _isChecked;
        private bool _isVisible = true;
        private bool _isSectionHeader;

        public string Label { get; set; } = string.Empty;
        public PackIconKind Icon { get; set; }
        public string? Route { get; set; }
        public string? RequiredPermission { get; set; }
        public PackIconKind? ActiveIcon { get; set; }
        public bool IsSeparator { get; set; }
        public bool IsSectionHeader
        {
            get => _isSectionHeader;
            set { _isSectionHeader = value; OnPropertyChanged(); }
        }
        public bool IsGroup => Children.Count > 0 && !IsSectionHeader;
        public ObservableCollection<NavigationItem> Children { get; set; } = new();

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsChecked));
            }
        }

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
                    ActiveIcon = PackIconKind.ViewDashboard,
                    Route = "Home",
                    RequiredPermission = null
                },
                new NavigationItem { IsSectionHeader = true, Label = "سير العمل" },
                new NavigationItem
                {
                    Label = "الحلقات",
                    Icon = PackIconKind.PlayCircleOutline,
                    ActiveIcon = PackIconKind.PlayCircle,
                    Route = "Episodes",
                    RequiredPermission = AppPermissions.EpisodeManage
                },
                new NavigationItem
                {
                    Label = "الضيوف",
                    Icon = PackIconKind.AccountVoice,
                    ActiveIcon = PackIconKind.AccountVoice,
                    Route = "Guests",
                    RequiredPermission = AppPermissions.GuestManage
                },
                new NavigationItem
                {
                    Label = "المراسلون",
                    Icon = PackIconKind.MapMarkerRadiusOutline,
                    ActiveIcon = PackIconKind.MapMarkerRadius,
                    Route = "Correspondents",
                    RequiredPermission = AppPermissions.CoordinationManage
                },
                new NavigationItem
                {
                    Label = "التقارير",
                    Icon = PackIconKind.FileChartOutline,
                    ActiveIcon = PackIconKind.FileChart,
                    Route = "Reports",
                    RequiredPermission = AppPermissions.ViewReports
                },
                new NavigationItem { IsSeparator = true },
                new NavigationItem
                {
                    Label = "المستخدمين",
                    Icon = PackIconKind.AccountKeyOutline,
                    ActiveIcon = PackIconKind.AccountKey,
                    Route = "Users",
                    RequiredPermission = AppPermissions.UserManage
                },
                new NavigationItem
                {
                    Label = "الموظفين",
                    Icon = PackIconKind.AccountTieOutline,
                    ActiveIcon = PackIconKind.AccountTie,
                    Route = "Employees",
                    RequiredPermission = AppPermissions.StaffManage
                },
                new NavigationItem
                {
                    Label = "المسميات الوظيفية",
                    Icon = PackIconKind.BriefcaseOutline,
                    ActiveIcon = PackIconKind.Briefcase,
                    Route = "StaffRoles",
                    RequiredPermission = AppPermissions.StaffManage
                },
                new NavigationItem
                {
                    Label = "الأدوار الأمنية",
                    Icon = PackIconKind.ShieldAccountOutline,
                    ActiveIcon = PackIconKind.ShieldAccount,
                    Route = "SecurityRoles",
                    RequiredPermission = AppPermissions.UserManage
                },
                new NavigationItem
                {
                    Label = "مصفوفة الصلاحيات",
                    Icon = PackIconKind.Matrix,
                    ActiveIcon = PackIconKind.Matrix,
                    Route = "PermissionMatrix",
                    RequiredPermission = AppPermissions.UserManage
                },
                new NavigationItem
                {
                    Label = "قاعدة البيانات",
                    Icon = PackIconKind.Database,
                    ActiveIcon = PackIconKind.Database,
                    Route = "Database",
                    RequiredPermission = AppPermissions.DatabaseManage
                },
                new NavigationItem
                {
                    Label = "سجل العمليات",
                    Icon = PackIconKind.History,
                    ActiveIcon = PackIconKind.History,
                    Route = "AuditLogs",
                    RequiredPermission = AppPermissions.ViewAuditLogs
                },
                new NavigationItem
                {
                    Label = "التشخيصات",
                    Icon = PackIconKind.HeartPulse,
                    ActiveIcon = PackIconKind.HeartPulse,
                    Route = "Diagnostics",
                    RequiredPermission = AppPermissions.DatabaseManage
                }
            };
        }

        public static ObservableCollection<NavigationItem> CreateAdminNavigation()
        {
            return new ObservableCollection<NavigationItem>
            {
                new NavigationItem
                {
                    Label = "المستخدمين",
                    Icon = PackIconKind.AccountKeyOutline,
                    ActiveIcon = PackIconKind.AccountKey,
                    Route = "Users",
                    RequiredPermission = AppPermissions.UserManage
                },
                new NavigationItem
                {
                    Label = "الموظفين",
                    Icon = PackIconKind.AccountTieOutline,
                    ActiveIcon = PackIconKind.AccountTie,
                    Route = "Employees",
                    RequiredPermission = AppPermissions.StaffManage
                },
                new NavigationItem
                {
                    Label = "المسميات الوظيفية",
                    Icon = PackIconKind.BriefcaseOutline,
                    ActiveIcon = PackIconKind.Briefcase,
                    Route = "StaffRoles",
                    RequiredPermission = AppPermissions.StaffManage
                },
                new NavigationItem
                {
                    Label = "الأدوار الأمنية",
                    Icon = PackIconKind.ShieldAccountOutline,
                    ActiveIcon = PackIconKind.ShieldAccount,
                    Route = "SecurityRoles",
                    RequiredPermission = AppPermissions.UserManage
                },
                new NavigationItem
                {
                    Label = "مصفوفة الصلاحيات",
                    Icon = PackIconKind.Matrix,
                    ActiveIcon = PackIconKind.Matrix,
                    Route = "PermissionMatrix",
                    RequiredPermission = AppPermissions.UserManage
                },
                new NavigationItem
                {
                    Label = "قاعدة البيانات",
                    Icon = PackIconKind.Database,
                    ActiveIcon = PackIconKind.Database,
                    Route = "Database",
                    RequiredPermission = AppPermissions.DatabaseManage
                },
                new NavigationItem
                {
                    Label = "سجل العمليات",
                    Icon = PackIconKind.History,
                    ActiveIcon = PackIconKind.History,
                    Route = "AuditLogs",
                    RequiredPermission = AppPermissions.ViewAuditLogs
                },
                new NavigationItem
                {
                    Label = "التشخيصات",
                    Icon = PackIconKind.HeartPulse,
                    ActiveIcon = PackIconKind.HeartPulse,
                    Route = "Diagnostics",
                    RequiredPermission = AppPermissions.DatabaseManage
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
                    ActiveIcon = PackIconKind.ViewDashboard,
                    Route = "Home"
                },
                new NavigationItem
                {
                    Label = "الحلقات",
                    Icon = PackIconKind.PlayCircleOutline,
                    ActiveIcon = PackIconKind.PlayCircle,
                    Route = "Episodes"
                },
                new NavigationItem
                {
                    Label = "النشر",
                    Icon = PackIconKind.FileDocumentMultiple,
                    ActiveIcon = PackIconKind.FileDocumentMultiple,
                    Route = "PublishingRecords"
                },
                new NavigationItem
                {
                    Label = "النظام",
                    Icon = PackIconKind.CogOutline,
                    ActiveIcon = PackIconKind.Cog,
                    Route = "AdminHub"
                }
            };
        }
    }
}
