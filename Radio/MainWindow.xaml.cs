using DataAccess.Common;
using DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;
using Radio.Forms;
using Radio.Messaging;
using Radio.Views.Correspondents;
using Radio.Views.Episodes;
using Radio.Views.Guests;
using Radio.Views.Home;
using Radio.Views.Programs;
using Radio.Views.Employees;
using Radio.Views.Reports;
using Radio.Views.StaffRoles;
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
        

        public MainWindow(UserSession session, IServiceProvider serviceProvider)
        {
            _session = session;
            _serviceProvider = serviceProvider;

            InitializeComponent();

            //TxtUserFullName.Text = _session.FullName;
            //ChipRole.Content = TranslateRole(_session.RoleName);

            Loaded += (_, _) => NotificationManager.RegisterHost(NotificationHost);

            InitializeUI();
        }

        private void InitializeUI()
        {
            // Set User Info in Header
            TxtUserFullName.Text = _session.FullName;
            ChipRole.Text = TranslateRole(_session.RoleName);

            // استدعاء نظام الأمان الجديد
            ApplyPermissionSecurity();

            // Default Navigation
            NavigateTo(new HomeView());
        }

        // 1. تحديث دالة تصفية القوائم بناءً على الصلاحيات الديناميكية
        private void ApplyPermissionSecurity()
        {
            // إدارة النظام
            bool canManageUsers = _session.HasPermission(AppPermissions.UserManage);
            MenuUsers.Visibility = canManageUsers ? Visibility.Visible : Visibility.Collapsed;
            MenuPermissions.Visibility = canManageUsers ? Visibility.Visible : Visibility.Collapsed;

            // طاقم العمل
            bool canManageStaff = _session.HasPermission(AppPermissions.StaffManage);
            MenuEmployees.Visibility = canManageStaff ? Visibility.Visible : Visibility.Collapsed;
            MenuStaffRoles.Visibility = canManageStaff ? Visibility.Visible : Visibility.Collapsed;

            // البرامج والمراسلين
            MenuPrograms.Visibility = _session.HasPermission(AppPermissions.ProgramManage) ? Visibility.Visible : Visibility.Collapsed;
            MenuCorrespondents.Visibility = _session.HasPermission(AppPermissions.CoordinationManage) ? Visibility.Visible : Visibility.Collapsed;

            // التقارير
            MenuReports.Visibility = _session.HasPermission(AppPermissions.ViewReports) ? Visibility.Visible : Visibility.Collapsed;

            // التغطيات تظهر فقط لمن يملك صلاحية إدارة التنسيق (لأنها مرتبطة بالمراسلين)
            MenuCoverages.Visibility = _session.HasPermission(AppPermissions.CoordinationManage)
                           ? Visibility.Visible : Visibility.Collapsed;

            // الحلقات والضيوف تظهر للجميع عادةً (والتحكم بالأزرار يكون داخلياً)
            MenuEpisodes.Visibility = Visibility.Visible; // تظهر للجميع

            MenuGuests.Visibility = _session.HasPermission(AppPermissions.GuestManage)
                           ? Visibility.Visible : Visibility.Collapsed;
        }

        private string TranslateRole(string roleName) => roleName switch
        {
            "آدمن" => "مدير النظام",
            "التنسيق" => "قسم التنسيق",
            "الإنتاج" => "قسم الإنتاج",
            "النشر الرقمي" => "قسم النشر الرقمي",
            _ => roleName
        };

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
            Environment.Exit(0);
            this.Close();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton selectedTab)
            {
                LoadView(selectedTab.Tag?.ToString());
            }

        }

        // ═══════════ مركز تحميل النوافذ ═══════════
        private void LoadView(string? viewName)
        {
            // مسح المحتوى الحالي أثناء التحميل
            MainContentArea.Content = null;
            var userService = _serviceProvider.GetRequiredService<IUserService>();

            try
            {
                switch (viewName)
                {
                    case "Home":
                        NavigateTo(new HomeView());
                        break;

                    case "Users":
                        // فتح واجهة إدارة المستخدمين
                        NavigateTo(new UsersView(userService, _session));
                        break;

                                        case "Employees":
                        var empService = _serviceProvider.GetRequiredService<IEmployeeService>();
                        NavigateTo(new EmployeesView(empService, _session));
                        break;

                    case "StaffRoles":
                        var staffService = _serviceProvider.GetRequiredService<IEmployeeService>();
                        NavigateTo(new StaffRolesView(staffService, _session));
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
                        var cService = _serviceProvider.GetRequiredService<ICorrespondentService>();
                                                var epEmpService = _serviceProvider.GetRequiredService<IEmployeeService>();
                        NavigateTo(new EpisodesView(epService, pService, _session, _serviceProvider, gService, cService, epEmpService));
                        break;

                    case "Guests":
                        var guestService = _serviceProvider.GetRequiredService<IGuestService>();
                        NavigateTo(new GuestsView(guestService, _session));
                        break;

                    case "Correspondents":
                        var corService = _serviceProvider.GetRequiredService<ICorrespondentService>();
                        NavigateTo(new CorrespondentsView(corService, _session));
                        break;

                    case "Coverage":
                        var covService = _serviceProvider.GetRequiredService<ICoverageService>();
                        NavigateTo(new CoverageView(covService, _session, _serviceProvider));
                        break;

                    case "Reports":
                        var reportService = _serviceProvider.GetRequiredService<IReportsService>();
                        NavigateTo(new ReportsView(reportService));
                        break;

                    default:
                        MainContentArea.Content = new TextBlock { Text = "قيد التطوير...", FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = System.Windows.Media.Brushes.LightGray };
                        break;
                }
            }
            catch (Exception ex)
            {
                // في حالة كان الـ UserControl غير موجود أو هناك خطأ في بنائه
                MainContentArea.Content = new TextBlock
                {
                    Text = $"خطأ في تحميل الشاشة: {ex.Message}",
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.Red
                };

                // يمكنك استدعاء MessageService هنا لإظهار الخطأ في Snackbar
                // MainSnackbar.MessageQueue.Enqueue($"خطأ: {ex.Message}");
            }
        }

    }
}
