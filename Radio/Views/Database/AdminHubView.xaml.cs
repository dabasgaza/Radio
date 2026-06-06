using DataAccess.Common;
using DataAccess.Services;
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
        private readonly Dictionary<string, UserControl> _cachedViews = new();
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

        private void NavigateToSubView(string viewName)
        {
            try
            {
                var view = GetSubView(viewName);
                if (view != null)
                {
                    AdminHubContentArea.Content = view;
                    _navigationService.NavigateTo(viewName);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An unexpected error occurred during processing");
                DataAccess.Services.Messaging.MessageService.Current.ShowError($"خطأ أثناء تحميل لوحة الإدارة الفرعية: {ex.Message}");
            }
        }

        private UserControl GetSubView(string name)
        {
            if (_cachedViews.TryGetValue(name, out var cached))
            {
                return cached;
            }

            UserControl view = name switch
            {
                "Users" => new UsersView(_serviceProvider.GetRequiredService<IUserService>(), _session),
                "Employees" => new EmployeesView(_serviceProvider.GetRequiredService<IEmployeeService>(), _session),
                "SocialPlatforms" => new SocialPlatformsView(_serviceProvider.GetRequiredService<IPlatformService>(), _session),
                "StaffRoles" => new StaffRolesView(_serviceProvider.GetRequiredService<IEmployeeService>(), _session),
                "SecurityRoles" => new SecurityRolesView(_serviceProvider.GetRequiredService<IUserService>(), _session, _navigationService),
                "PermissionMatrix" => new PermissionMatrixView(_serviceProvider.GetRequiredService<IUserService>(), _session),
                "Permissions" => new PermissionsView(_serviceProvider.GetRequiredService<IPermissionService>()),
                "Database" => new DatabaseManagementView(_serviceProvider.GetRequiredService<IDatabaseManagementService>(), _session),
                "AuditLogs" => new AuditLogsView(_serviceProvider.GetRequiredService<IAuditLogService>(), _session),
                "Diagnostics" => new SystemDiagnosticsView(_serviceProvider.GetRequiredService<ISystemDiagnosticsService>(), _session),
                _ => throw new ArgumentException("Unknown admin sub-view name")
            };

            _cachedViews[name] = view;
            return view;
        }

        public void UpdateLayoutForScreenSize(double screenWidth)
        {
            bool useVertical = screenWidth < 1024;
            HorizontalNav.Visibility = useVertical ? Visibility.Collapsed : Visibility.Visible;
            VerticalNav.Visibility = useVertical ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}