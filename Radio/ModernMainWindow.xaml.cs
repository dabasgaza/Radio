using DataAccess.Common;
using Radio.Messaging;
using Radio.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Radio
{
    public partial class ModernMainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private readonly NavigationService _navigationService;
        private readonly UserSession _session;
        private readonly IServiceProvider _serviceProvider;
        
        private DateTime? _broadcastStartTime;
        private DispatcherTimer _onAirTimer;

        private static readonly Dictionary<string, string> NavRouteMap = new()
        {
            ["MenuHome"] = "Home",
            ["MenuEpisodes"] = "Episodes",
            ["MenuGuests"] = "Guests",
            ["MenuCoverages"] = "Correspondents",
            ["MenuReports"] = "Reports",
            ["MenuPublishingRecords"] = "PublishingRecords",
            ["MenuUsers"] = "Users",
            ["MenuEmployees"] = "Employees",
            ["MenuStaffRoles"] = "StaffRoles",
            ["MenuSecurityRoles"] = "SecurityRoles",
            ["MenuPermissionMatrix"] = "PermissionMatrix",
            ["MenuPermissions"] = "Permissions",
            ["MenuSocialPlatforms"] = "SocialPlatforms",
        };

        private Dictionary<string, bool> _sectionStates = new()
        {
            ["Workflow"] = true,
            ["Publishing"] = false,
            ["Admin"] = false
        };

        public async Task ShowOverlay() => await this.ShowOverlayAsync();
        public async Task HideOverlay() => await this.HideOverlayAsync();

        public ModernMainWindow(UserSession session, IServiceProvider serviceProvider)
        {
            _session = session;
            _serviceProvider = serviceProvider;
            _navigationService = new NavigationService(serviceProvider, session);
            _navigationService.ViewChanged += OnViewChanged;

            InitializeComponent();
            
            Loaded += (_, _) => NotificationManager.RegisterHost(NotificationHost);

            InitializeUI();
            InitializeOnAirWidget();
        }

        private void InitializeUI()
        {
            ApplyPermissionSecurity();
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
        }

        private void ApplyPermissionSecurity()
        {
            bool canManageUsers = _session.HasPermission(AppPermissions.UserManage);
            bool canManageStaff = _session.HasPermission(AppPermissions.StaffManage);
            bool canViewReports = _session.HasPermission(AppPermissions.ViewReports);
            bool canManageGuests = _session.HasPermission(AppPermissions.GuestManage);
            bool canCoordinate = _session.HasPermission(AppPermissions.CoordinationManage);

            // Workflow Items
            MenuGuests.Visibility = canManageGuests ? Visibility.Visible : Visibility.Collapsed;
            MenuCoverages.Visibility = canCoordinate ? Visibility.Visible : Visibility.Collapsed;
            MenuReports.Visibility = canViewReports ? Visibility.Visible : Visibility.Collapsed;

            // Publishing Items
            bool showPublishing = canViewReports || canManageStaff; // Adjust as needed
            PublishingHeader.Visibility = showPublishing ? Visibility.Visible : Visibility.Collapsed;
            PublishingItems.Visibility = showPublishing ? Visibility.Visible : Visibility.Collapsed;

            // Admin Items
            MenuUsers.Visibility = canManageUsers ? Visibility.Visible : Visibility.Collapsed;
            MenuEmployees.Visibility = canManageStaff ? Visibility.Visible : Visibility.Collapsed;
            MenuStaffRoles.Visibility = canManageStaff ? Visibility.Visible : Visibility.Collapsed;
            MenuSecurityRoles.Visibility = canManageUsers ? Visibility.Visible : Visibility.Collapsed;
            MenuPermissionMatrix.Visibility = canManageUsers ? Visibility.Visible : Visibility.Collapsed;
            MenuPermissions.Visibility = canManageUsers ? Visibility.Visible : Visibility.Collapsed;
            MenuSocialPlatforms.Visibility = canManageStaff ? Visibility.Visible : Visibility.Collapsed;

            AdminHeaderLabel.Visibility = (canManageUsers || canManageStaff) ? Visibility.Visible : Visibility.Collapsed;
            AdminItems.Visibility = AdminHeaderLabel.Visibility;
        }

        public void NavigateToView(string viewName)
        {
            var view = _navigationService.NavigateTo(viewName);
            if (view != null)
            {
                MainContentArea.Content = view;

                // تحديث حالة الأزرار في القائمة الجانبية (اختياري ولكن يفضل للجمالية)
                var buttonName = NavRouteMap.FirstOrDefault(x => x.Value == viewName).Key;
                if (!string.IsNullOrEmpty(buttonName))
                {
                    var button = this.FindName(buttonName) as RadioButton;
                    if (button != null) button.IsChecked = true;
                }
            }
        }

        private void NavRailItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && NavRouteMap.TryGetValue(rb.Name, out var viewName))
            {
                NavigateToView(viewName);
            }
        }

        private void SectionHeader_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border header && header.Tag is string section)
            {
                bool isExpanded = _sectionStates.ContainsKey(section) && _sectionStates[section];
                _sectionStates[section] = !isExpanded;
                UpdateSectionVisibility(section, !isExpanded);
            }
        }

        private void UpdateSectionVisibility(string section, bool expand)
        {
            var panel = section switch
            {
                "Workflow" => WorkflowItems,
                "Publishing" => PublishingItems,
                "Admin" => AdminItems,
                _ => null
            };

            var chevron = section switch
            {
                "Workflow" => WorkflowChevron,
                "Publishing" => PublishingChevron,
                "Admin" => AdminChevron,
                _ => null
            };

            if (panel != null)
            {
                panel.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
            }

            if (chevron != null)
            {
                chevron.RenderTransform = new RotateTransform(expand ? 0 : 180);
                chevron.RenderTransformOrigin = new Point(0.5, 0.5);
            }
        }

        private void BtnCollapseToggle_Click(object sender, RoutedEventArgs e)
        {
            bool isCollapsed = ColSidebar.Width.Value == 0;
            
            DoubleAnimation animation = new DoubleAnimation
            {
                To = isCollapsed ? 260 : 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            // Using direct width change for simplicity in this example
            ColSidebar.BeginAnimation(ColumnDefinition.WidthProperty, new GridLengthAnimation
            {
                From = isCollapsed ? new GridLength(0) : new GridLength(260),
                To = isCollapsed ? new GridLength(260) : new GridLength(0),
                Duration = TimeSpan.FromMilliseconds(300)
            });

            CollapseIcon.Kind = isCollapsed ? MaterialDesignThemes.Wpf.PackIconKind.MenuOpen : MaterialDesignThemes.Wpf.PackIconKind.Menu;
        }

        private void InitializeOnAirWidget()
        {
            _onAirTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _onAirTimer.Tick += (s, e) => {
                if (_broadcastStartTime.HasValue)
                {
                    var elapsed = DateTime.Now - _broadcastStartTime.Value;
                    OnAirElapsedTime.Text = elapsed.ToString(@"hh\:mm\:ss");
                }
            };

            // Simple pulse animation
            var pulse = new DoubleAnimation(1.0, 0.4, new Duration(TimeSpan.FromSeconds(1)))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            LivePulse.BeginAnimation(OpacityProperty, pulse);
        }

        // Methods to start/stop broadcast (can be called from child views or services)
        public void StartBroadcast(string programName)
        {
            _broadcastStartTime = DateTime.Now;
            OnAirProgramName.Text = programName;
            OnAirWidget.Visibility = Visibility.Visible;
            _onAirTimer.Start();
        }

        private void BtnStopOnAir_Click(object sender, RoutedEventArgs e)
        {
            _onAirTimer.Stop();
            OnAirWidget.Visibility = Visibility.Collapsed;
            _broadcastStartTime = null;
        }

        private void BtnUserProfile_Click(object sender, RoutedEventArgs e)
        {
            UserProfilePopup.IsOpen = !UserProfilePopup.IsOpen;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Custom Window Controls
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                BtnMaximize_Click(sender, e);
            }
            else
            {
                this.DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                MaximizeIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.WindowMaximize;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                MaximizeIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.WindowRestore;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    // Helper class for GridLength animation (since WPF doesn't support it natively)
    public class GridLengthAnimation : AnimationTimeline
    {
        public override Type TargetPropertyType => typeof(GridLength);
        public GridLength From { get; set; }
        public GridLength To { get; set; }

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            double fromVal = From.Value;
            double toVal = To.Value;

            if (fromVal > toVal)
            {
                return new GridLength((1 - animationClock.CurrentProgress.Value) * (fromVal - toVal) + toVal, From.GridUnitType);
            }
            return new GridLength(animationClock.CurrentProgress.Value * (toVal - fromVal) + fromVal, From.GridUnitType);
        }

        protected override Freezable CreateInstanceCore() => new GridLengthAnimation();
    }
}
