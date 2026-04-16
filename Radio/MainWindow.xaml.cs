using BroadcastWorkflow.Services;
using DataAccess.Common;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Radio.Forms;
using Radio.Messaging;
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
        private readonly IReportsService _reportsService;

        //سيتم حذف هذا الكونستركتور لاحقاً بعد بناء شاشة تسجيل الدخول وربطها بالجلسة 
        //public MainWindow(IServiceProvider serviceProvider)
        //{
        //    InitializeComponent();
        //    _session = new UserSession();
        //      _serviceProvider = serviceProvider;

        //    InitializeUI();
        //}
        public MainWindow(UserSession session, IServiceProvider serviceProvider, IReportsService reportsService)
        {
            _session = session;
            _serviceProvider = serviceProvider;
            _reportsService = reportsService;

            InitializeComponent();

            TxtUserFullName.Text = _session.FullName;
            ChipRole.Content = TranslateRole(_session.RoleName);

            // 👈 تهيئة نظام الإشعارات المركزي وربطه بالـ Snackbar الخاص بهذه النافذة
            var wpfMessaging = new WpfMessageService(MainSnackbar.MessageQueue!);
            MessageService.Initialize(wpfMessaging);

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
            NavigateTo(new ReportsView(_reportsService));
        }

        // 1. تحديث دالة تصفية القوائم بناءً على الصلاحيات الديناميكية
        private void ApplyPermissionSecurity()
        {
            // إدارة النظام
            bool canManageUsers = _session.HasPermission(AppPermissions.UserManage);
            MenuUsers.Visibility = canManageUsers ? Visibility.Visible : Visibility.Collapsed;
            MenuPermissions.Visibility = canManageUsers ? Visibility.Visible : Visibility.Collapsed;

            // البرامج والمراسلين
            MenuPrograms.Visibility = _session.HasPermission(AppPermissions.ProgramManage) ? Visibility.Visible : Visibility.Collapsed;
            MenuCorrespondents.Visibility = _session.HasPermission(AppPermissions.CoordinationManage) ? Visibility.Visible : Visibility.Collapsed;

            // التقارير
            MenuReports.Visibility = _session.HasPermission(AppPermissions.ViewReports) ? Visibility.Visible : Visibility.Collapsed;

            // التغطيات تظهر فقط لمن يملك صلاحية إدارة التنسيق (لأنها مرتبطة بالمراسلين)
            MenuCoverages.Visibility = _session.HasPermission(AppPermissions.CoordinationManage)
                           ? Visibility.Visible : Visibility.Collapsed;

            // الحلقات والضيوف تظهر للجميع عادةً (والتحكم بالأزرار يكون داخلياً)
            MenuEpisodes.Visibility = Visibility.Visible;
            MenuGuests.Visibility = Visibility.Visible;
        }

        private string TranslateRole(string roleName) => roleName switch
        {
            "آدمن" => "مدير النظام",
            "التنسيق" => "قسم التنسيق",
            "الإنتاج" => "قسم الإنتاج",
            "النشر الرقمي" => "قسم النشر الرقمي",
            _ => roleName
        };

        private void MenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainContentArea == null) return;

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
                        var gService = _serviceProvider.GetRequiredService<IGuestService>();
                        NavigateTo(new EpisodesView(epService, pService, _session, _serviceProvider, gService));
                        break;

                    case "Guests":
                        var guestService = _serviceProvider.GetRequiredService<IGuestService>();
                        NavigateTo(new GuestsView(guestService, _session));
                        break;

                    case "Correspondents":
                        var corService = _serviceProvider.GetRequiredService<ICorrespondentService>();
                        NavigateTo(new CorrespondentsView(corService, _session));
                        break;

                    case "CoverageView":
                        var covService = _serviceProvider.GetRequiredService<ICoverageService>();
                        NavigateTo(new CoverageView(covService, _session, _serviceProvider));
                        break;

                    case "Reports":
                        var reportService = _serviceProvider.GetRequiredService<IReportsService>();
                        NavigateTo(new ReportsView(reportService));
                        break;
                }
            }
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

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}