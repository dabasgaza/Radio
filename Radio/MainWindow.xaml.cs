using BroadcastWorkflow.Services;
using DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;
using Radio.Forms;
using Radio.Views.Correspondents;
using Radio.Views.Episodes;
using Radio.Views.Guests;
using Radio.Views.Home;
using Radio.Views.Programs;
using Radio.Views.Reports;
using Radio.Views.Users;
using System.Windows;
using System.Windows.Controls;

namespace Radio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly UserSession _session;
        private readonly IServiceProvider _serviceProvider;

        //سيتم حذف هذا الكونستركتور لاحقاً بعد بناء شاشة تسجيل الدخول وربطها بالجلسة 
        //public MainWindow(IServiceProvider serviceProvider)
        //{
        //    InitializeComponent();
        //    _session = new UserSession();
        //      _serviceProvider = serviceProvider;

        //    InitializeUI();
        //}
        public MainWindow(UserSession session, IServiceProvider serviceProvider)
        {
            _session = session;
            _serviceProvider = serviceProvider;

            InitializeComponent();

            TxtUserFullName.Text = _session.FullName;
            ChipRole.Content = TranslateRole(_session.RoleName);

            InitializeUI();
        }

        private void InitializeUI()
        {
            // Set User Info in Header
            TxtUserFullName.Text = _session.FullName;
            ChipRole.Content = TranslateRole(_session.RoleName);

            // استدعاء نظام الأمان الجديد
            ApplyPermissionSecurity();

            // Default Navigation
            NavigateTo(new HomeView());
        }

        // 1. تحديث دالة تصفية القوائم بناءً على الصلاحيات الديناميكية
        private void ApplyPermissionSecurity()
        {
            // صلاحية إدارة النظام (المستخدمين والصلاحيات)
            bool canManageUsers = _session.HasPermission("USER_MANAGE");
            MenuUsers.Visibility = canManageUsers ? Visibility.Visible : Visibility.Collapsed;

            MenuPermissions.Visibility = canManageUsers ? Visibility.Visible : Visibility.Collapsed;

            // صلاحية إدارة البرامج
            MenuPrograms.Visibility = _session.HasPermission("PROGRAM_MANAGE") ? Visibility.Visible : Visibility.Collapsed;

            // صلاحية إدارة المراسلين
            MenuCorrespondents.Visibility = _session.HasPermission("CORR_MANAGE") ? Visibility.Visible : Visibility.Collapsed;

            // صلاحية عرض التقارير
            MenuReports.Visibility = _session.HasPermission("VIEW_REPORTS") ? Visibility.Visible : Visibility.Collapsed;

            // ملاحظة: الضيوف والحلقات عادة تظهر للكل، ولكن الأزرار بداخلها 
            // يتم التحكم بها داخل الـ UserControl الخاص بكل منها.
        }


        private string TranslateRole(string roleName) => roleName switch
        {
            "آدمن" => "مدير النظام",
            "التنسيق" => "قسم التنسيق",
            "الإنتاج" => "قسم الإنتاج",
            "النشر الرقمي" => "قسم النشر الرقمي",
            _ => roleName
        };

        private void BtnMenu_Click(object sender, RoutedEventArgs e) => NavDrawer.IsLeftDrawerOpen = true;

        private void MenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuListBox.SelectedItem is ListBoxItem item)
            {
                string tag = item.Tag?.ToString();
                var userService = _serviceProvider.GetRequiredService<IUserService>();

                switch (tag)
                {
                    case "Home":
                        NavigateTo(new HomeView());
                        break;

                    case "Users":
                        // فتح واجهة إدارة المستخدمين
                        NavigateTo(new UsersView(userService, _session));
                        break;

                    case "Permissions":
                        // Open permission matrix as a window (PermissionMatrixView is a MetroWindow)
                        var permWindow = new PermissionMatrixView(userService, _session)
                        {
                            Owner = this
                        };
                        permWindow.ShowDialog();
                        break;

                    case "Programs":
                        var progService = _serviceProvider.GetRequiredService<IProgramService>();
                        NavigateTo(new ProgramsView(progService, _session));
                        break;

                    case "Episodes":
                        var epService = _serviceProvider.GetRequiredService<IEpisodeService>();
                        var pService = _serviceProvider.GetRequiredService<IProgramService>();
                        NavigateTo(new EpisodesView(epService, pService, _session, _serviceProvider));
                        break;

                    case "Guests":
                        var guestService = _serviceProvider.GetRequiredService<IGuestService>();
                        NavigateTo(new GuestsView(guestService, _session));
                        break;

                    case "Correspondents":
                        var corService = _serviceProvider.GetRequiredService<ICorrespondentService>();
                        NavigateTo(new CorrespondentsView(corService, _session));
                        break;

                    case "Reports":
                        var reportService = _serviceProvider.GetRequiredService<IReportsService>();
                        NavigateTo(new ReportsView(reportService));
                        break;
                }
            }
            NavDrawer.IsLeftDrawerOpen = false;

        }

        private void NavigateTo(UserControl view)
        {
            MainContentArea.Content = view;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
            this.Close();
        }

        private void ApplyRoleSecurity()
        {
            if (!string.Equals(_session.RoleName, "Coordination", StringComparison.OrdinalIgnoreCase))
            {
                MenuPrograms.Visibility = Visibility.Collapsed;
                MenuCorrespondents.Visibility = Visibility.Collapsed;
                MenuUsers.Visibility = Visibility.Collapsed;
            }
        }


    }
}