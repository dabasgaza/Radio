using DataAccess.Common;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Radio.Models;
using Radio.Services;
using Radio.Views.Employees;
using Radio.Views.SocialPlatforms;
using Radio.Views.StaffRoles;
using Radio.Views.Users;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Database
{
    public partial class AdminHubView : UserControl
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly UserSession _session;
        private readonly NavigationService _navigationService;

        // ✨ ذاكرة مؤقتة محدودة الحجم مع آلية LRU للعروض الفرعية
        // منفصلة عن NavigationService._viewCache لتجنب تضارب الشجرة البصرية (Visual Tree)
        // لأن العرض قد يكون معروضاً في AdminHubContentArea أو MainContentArea
        private readonly Dictionary<string, UserControl> _cachedViews = new();
        private readonly LinkedList<string> _subViewAccessOrder = new(); // تتبع ترتيب الوصول لـ LRU
        private const int MaxSubViewCache = 6; // الحد الأقصى للعروض الفرعية المخزنة

        private readonly List<RadioButton> _chips = new();

        public AdminHubView(IServiceProvider serviceProvider, UserSession session, NavigationService navigationService)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _session = session;
            _navigationService = navigationService;

            Loaded += AdminHubView_Loaded;
        }

        private void AdminHubView_Loaded(object sender, RoutedEventArgs e)
        {
            BuildNavigation();
            ApplyPermissions();
            SelectDefaultChip();
        }

        private void BuildNavigation()
        {
            var navItems = NavigationBuilder.CreateAdminNavigation();
            foreach (var item in navItems)
            {
                var chip = CreateChip(item);
                _chips.Add(chip);
                ChipPanel.Children.Add(chip);
                VerticalChipPanel.Children.Add(CreateChip(item, true));
            }
        }

        private RadioButton CreateChip(NavigationItem item, bool isVertical = false)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var icon = new PackIcon
            {
                Width = isVertical ? 20 : 16,
                Height = isVertical ? 20 : 16,
                Margin = new Thickness(0, 0, isVertical ? 10 : 6, 0),
                Kind = item.Icon
            };

            var textBlock = new TextBlock
            {
                Text = item.Label,
                VerticalAlignment = VerticalAlignment.Center
            };

            stackPanel.Children.Add(icon);
            stackPanel.Children.Add(textBlock);

            var radioButton = new RadioButton
            {
                Tag = item.Route,
                Content = stackPanel,
                Style = (Style)FindResource(isVertical ? "AdminChip.Vertical" : "AdminChip.Horizontal")
            };
            radioButton.Checked += Chip_Click;
            return radioButton;
        }

        private void ApplyPermissions()
        {
            bool canManageUsers = _session.HasPermission(AppPermissions.UserManage);
            bool canManageStaff = _session.HasPermission(AppPermissions.StaffManage);
            bool canManageDatabase = _session.HasPermission(AppPermissions.DatabaseManage);

            foreach (var chip in _chips)
            {
                var route = chip.Tag?.ToString();
                chip.Visibility = GetItemVisibility(route, canManageUsers, canManageStaff, canManageDatabase);
            }
        }

        private Visibility GetItemVisibility(string? route, bool canManageUsers, bool canManageStaff, bool canManageDatabase)
        {
            return route switch
            {
                "Users" or "SecurityRoles" or "PermissionMatrix" or "Permissions" =>
                    canManageUsers ? Visibility.Visible : Visibility.Collapsed,
                "Employees" or "StaffRoles" =>
                    canManageStaff ? Visibility.Visible : Visibility.Collapsed,
                "Database" or "AuditLogs" or "Diagnostics" =>
                    canManageDatabase ? Visibility.Visible : Visibility.Collapsed,
                "SocialPlatforms" =>
                    canManageStaff ? Visibility.Visible : Visibility.Collapsed,
                _ => Visibility.Visible
            };
        }

        private void SelectDefaultChip()
        {
            foreach (var chip in _chips)
            {
                if (chip.Visibility == Visibility.Visible)
                {
                    chip.IsChecked = true;
                    NavigateToSubView(chip.Tag as string ?? string.Empty);
                    break;
                }
            }
        }

        private void Chip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton chip && chip.Tag is string viewName)
            {
                NavigateToSubView(viewName);
            }
        }

        /// <summary>
        /// التنقل إلى عرض فرعي داخل لوحة الإدارة.
        /// ✨ يستخدم NotifySubViewChanged بدلاً من NavigateTo لتجنب إنشاء نسخة مكررة
        /// من العرض في NavigationService._viewCache (كان يسبب تسرب ذاكرة مزدوج).
        /// </summary>
        private void NavigateToSubView(string viewName)
        {
            try
            {
                var view = GetSubView(viewName);
                if (view != null)
                {
                    AdminHubContentArea.Content = view;

                    // ✨ تحديث حالة التنقل (الشريط الجانبي + مسار التنقل)
                    // دون إنشاء نسخة مكررة من العرض في NavigationService
                    _navigationService.NotifySubViewChanged(viewName);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "خطأ أثناء تحميل لوحة الإدارة الفرعية: {ViewName}", viewName);
                MessageService.Current.ShowError($"خطأ أثناء تحميل لوحة الإدارة الفرعية: {ex.Message}");
            }
        }

        /// <summary>
        /// الحصول على عرض فرعي من الذاكرة المؤقتة أو إنشائه.
        /// ✨ آلية LRU: عند تجاوز الحد الأقصى، يُزال العرض الأقل استعمالاً.
        /// </summary>
        private UserControl GetSubView(string name)
        {
            if (_cachedViews.TryGetValue(name, out var cached))
            {
                // تحديث ترتيب LRU — نقل العنصر إلى المقدمة
                _subViewAccessOrder.Remove(name);
                _subViewAccessOrder.AddFirst(name);
                return cached;
            }

            // ✨ حل مشكلة عدم تمرير DialogHelper للعروض الفرعية
            // كان يسبب NullReferenceException عند فتح أي حوار من خلال لوحة الإدارة
            var dialogHelper = _serviceProvider.GetRequiredService<DialogHelper>();

            UserControl view = name switch
            {
                "Users" => new UsersView(_serviceProvider.GetRequiredService<IUserService>(), _session, dialogHelper),
                "Employees" => new EmployeesView(_serviceProvider.GetRequiredService<IEmployeeService>(), _session, dialogHelper),
                "SocialPlatforms" => new SocialPlatformsView(_serviceProvider.GetRequiredService<IPlatformService>(), _session, dialogHelper),
                "StaffRoles" => new StaffRolesView(_serviceProvider.GetRequiredService<IEmployeeService>(), _session, dialogHelper),
                "SecurityRoles" => new SecurityRolesView(_serviceProvider.GetRequiredService<IUserService>(), _session, _navigationService, dialogHelper),
                "PermissionMatrix" => new PermissionMatrixView(_serviceProvider.GetRequiredService<IUserService>(), _session),
                "Permissions" => new PermissionsView(_serviceProvider.GetRequiredService<IPermissionService>()),
                "Database" => new DatabaseManagementView(_serviceProvider.GetRequiredService<IDatabaseManagementService>(), _session),
                "AuditLogs" => new AuditLogsView(_serviceProvider.GetRequiredService<IAuditLogService>(), _session),
                "Diagnostics" => new SystemDiagnosticsView(_serviceProvider.GetRequiredService<ISystemDiagnosticsService>(), _session),
                _ => throw new ArgumentException($"عرض فرعي غير معروف: {name}")
            };

            // ✨ إزالة الأقل استعمالاً إذا تجاوزنا الحد الأقصى
            if (_cachedViews.Count >= MaxSubViewCache && _subViewAccessOrder.Count > 0)
            {
                var lruViewName = _subViewAccessOrder.Last!.Value;
                _cachedViews.Remove(lruViewName);
                _subViewAccessOrder.RemoveLast();
            }

            _cachedViews[name] = view;
            _subViewAccessOrder.AddFirst(name);
            return view;
        }

        /// <summary>
        /// تفريغ الذاكرة المؤقتة للعروض الفرعية — يُستخدم عند تسجيل الخروج.
        /// </summary>
        public void ClearSubViewCache()
        {
            _cachedViews.Clear();
            _subViewAccessOrder.Clear();
        }

        public void UpdateLayoutForScreenSize(double screenWidth)
        {
            bool useVertical = screenWidth < 1024;
            HorizontalNav.Visibility = useVertical ? Visibility.Collapsed : Visibility.Visible;
            VerticalNav.Visibility = useVertical ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
