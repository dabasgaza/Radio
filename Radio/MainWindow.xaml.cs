using DataAccess.Common;
using DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;
using Radio.Forms;
using Radio.Messaging;
using Radio.Services;
using Radio.Views.Users;
using System.Windows;
using System.Windows.Controls;

namespace Radio
{
    public partial class MainWindow
    {
        private readonly NavigationService _navigationService;
        private readonly UserSession _session;
        private readonly IServiceProvider _serviceProvider;

        private static readonly Dictionary<string, string> NavRouteMap = new()
        {
            ["MenuHome"] = "Home",
            ["MenuEpisodes"] = "Episodes",
            ["MenuGuests"] = "Guests",
            ["MenuCoverages"] = "Correspondents",
            ["MenuCorrespondents"] = "Coverage",
            ["MenuReports"] = "Reports",
            ["MenuPublishingRecords"] = "PublishingRecords",
            ["MenuUsers"] = "Users",
            ["MenuEmployees"] = "Employees",
            ["MenuStaffRoles"] = "StaffRoles",
            ["MenuSocialPlatforms"] = "SocialPlatforms",
        };

        public MainWindow(UserSession session, IServiceProvider serviceProvider)
        {
            _session = session;
            _serviceProvider = serviceProvider;
            _navigationService = new NavigationService(serviceProvider, session);
            _navigationService.ViewChanged += OnViewChanged;

            InitializeComponent();

            Loaded += (_, _) => NotificationManager.RegisterHost(NotificationHost);

            InitializeUI();
        }

        private void InitializeUI()
        {
            ApplyPermissionSecurity();

            SetDefaultNavSelection();

            _navigationService.NavigateTo("Home");
        }

        private void OnViewChanged(string viewName)
        {
            UpdateBreadcrumb();
        }

        private void UpdateBreadcrumb()
        {
            var path = string.Join(" / ", _navigationService.History.Reverse().Take(3));
            BreadcrumbBar.Text = path;
            BreadcrumbBar.Visibility = _navigationService.History.Count > 1
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void SetDefaultNavSelection()
        {
            MenuHome.IsChecked = true;
        }

        private void ApplyPermissionSecurity()
        {
            bool canManageUsers = _session.HasPermission(AppPermissions.UserManage);
            bool canManageStaff = _session.HasPermission(AppPermissions.StaffManage);
            bool canManagePrograms = _session.HasPermission(AppPermissions.ProgramManage);
            bool canCoordinate = _session.HasPermission(AppPermissions.CoordinationManage);
            bool canViewReports = _session.HasPermission(AppPermissions.ViewReports);
            bool canManageGuests = _session.HasPermission(AppPermissions.GuestManage);

            // Admin group: visible if any admin permission
            bool showAdmin = canManageUsers || canManageStaff;
            Div3.Visibility = showAdmin ? Visibility.Visible : Visibility.Collapsed;
            MenuAdminGroup.Visibility = showAdmin ? Visibility.Visible : Visibility.Collapsed;

            MenuUsers.Visibility = canManageUsers ? Visibility.Visible : Visibility.Collapsed;
            MenuPermissions.Visibility = canManageUsers ? Visibility.Visible : Visibility.Collapsed;
            MenuEmployees.Visibility = canManageStaff ? Visibility.Visible : Visibility.Collapsed;
            MenuStaffRoles.Visibility = canManageStaff ? Visibility.Visible : Visibility.Collapsed;
            MenuSocialPlatforms.Visibility = canManageStaff ? Visibility.Visible : Visibility.Collapsed;

            // Contacts group: visible if coordination or guest management
            bool showContacts = canCoordinate || canManageGuests;
            MenuContacts.Visibility = showContacts ? Visibility.Visible : Visibility.Collapsed;
            MenuGuests.Visibility = canManageGuests ? Visibility.Visible : Visibility.Collapsed;
            MenuCoverages.Visibility = canCoordinate ? Visibility.Visible : Visibility.Collapsed;
            MenuCorrespondents.Visibility = canCoordinate ? Visibility.Visible : Visibility.Collapsed;

            // Reports group
            MenuReportsGroup.Visibility = canViewReports ? Visibility.Visible : Visibility.Collapsed;
            MenuReports.Visibility = canViewReports ? Visibility.Visible : Visibility.Collapsed;
            MenuPublishingRecords.Visibility = Visibility.Visible;
        }

        private string TranslateRole(string roleName) => roleName switch
        {
            "آدمن" => "مدير النظام",
            "التنسيق" => "قسم التنسيق",
            "الإنتاج" => "قسم الإنتاج",
            "النشر الرقمي" => "قسم النشر الرقمي",
            _ => roleName
        };

        // ─── معالجات التنقل ────────────────────────────────────

        private void NavRailItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton rb || rb.Name == null)
                return;

            if (rb.Name == "MenuPermissions")
            {
                OpenPermissionDialog();
                return;
            }

            if (!NavRouteMap.TryGetValue(rb.Name, out var viewName))
                return;

            var view = _navigationService.NavigateTo(viewName);
            if (view != null)
                MainContentArea.Content = view;

            CollapseAllGroups();
        }

        private void GroupNav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton rb)
                return;

            CollapseAllGroups();

            if (rb.Name == "MenuContacts")
                ContactsSubMenu.Visibility = Visibility.Visible;
            else if (rb.Name == "MenuReportsGroup")
                ReportsSubMenu.Visibility = Visibility.Visible;
            else if (rb.Name == "MenuAdminGroup")
                AdminSubMenu.Visibility = Visibility.Visible;
        }

        private void CollapseAllGroups()
        {
            ContactsSubMenu.Visibility = Visibility.Collapsed;
            ReportsSubMenu.Visibility = Visibility.Collapsed;
            AdminSubMenu.Visibility = Visibility.Collapsed;
        }

        private void OpenPermissionDialog()
        {
            var userService = _serviceProvider.GetRequiredService<IUserService>();
            var view = new PermissionMatrixView(userService, _session);
            var window = new MahApps.Metro.Controls.MetroWindow
            {
                Content = view,
                Title = "إدارة الصلاحيات",
                Width = 1000,
                Height = 650,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            window.ShowDialog();
        }

        // ─── معالجات النافذة ────────────────────────────────────

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }
}
