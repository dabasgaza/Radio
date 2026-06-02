using DataAccess.Common;
using DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;
using Radio.Views.Employees;
using Radio.Views.SocialPlatforms;
using Radio.Views.StaffRoles;
using Radio.Views.Users;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Database
{
    public partial class AdminHubView : UserControl
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly UserSession _session;
        private readonly Dictionary<string, UserControl> _cachedViews = new();
        private bool _isUpdatingSelection = false;

        public AdminHubView(IServiceProvider serviceProvider, UserSession session)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _session = session;

            Loaded += AdminHubView_Loaded;
        }

        private void AdminHubView_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyPermissions();
            
            // Navigate to default view (Users or whatever is visible first)
            if (ListStaffSecurity.Items.Count > 0 && ListStaffSecurity.Visibility == Visibility.Visible)
            {
                ListStaffSecurity.SelectedIndex = 0;
            }
            else if (ListSystemMaintenance.Items.Count > 0 && ListSystemMaintenance.Visibility == Visibility.Visible)
            {
                ListSystemMaintenance.SelectedIndex = 0;
            }
        }

        private void ApplyPermissions()
        {
            bool canManageUsers = _session.HasPermission(AppPermissions.UserManage);
            bool canManageStaff = _session.HasPermission(AppPermissions.StaffManage);

            // Hide/Show ListBox items or entire ListBoxes based on permission
            ListStaffSecurity.Visibility = (canManageUsers || canManageStaff) ? Visibility.Visible : Visibility.Collapsed;
            ListSystemMaintenance.Visibility = _session.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            ListConfiguration.Visibility = canManageStaff ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ListNavigation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelection) return;

            if (sender is ListBox listBox && listBox.SelectedItem is ListBoxItem item && item.Tag is string viewName)
            {
                _isUpdatingSelection = true;

                // Deselect other list boxes
                if (listBox != ListStaffSecurity) ListStaffSecurity.SelectedItem = null;
                if (listBox != ListSystemMaintenance) ListSystemMaintenance.SelectedItem = null;
                if (listBox != ListConfiguration) ListConfiguration.SelectedItem = null;

                _isUpdatingSelection = false;

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
                }
            }
            catch (Exception ex)
            {
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
                "SecurityRoles" => new SecurityRolesView(_serviceProvider.GetRequiredService<IUserService>(), _session),
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
    }
}
