using DataAccess.Common;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Radio.Controls;
using Radio.Messaging;
using Radio.Models;
using Radio.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Radio
{
    public partial class ModernMainWindow : MahApps.Metro.Controls.MetroWindow
    {
        private readonly NavigationService _navigationService;
        private readonly UserSession _session;
        private NavigationMode _navigationMode = NavigationMode.Expanded;
        private bool _isDrawerOpen = false;

        private DateTime? _broadcastStartTime;
        private DispatcherTimer? _onAirTimer;

        public ModernMainWindow(NavigationService navigationService, CurrentSessionProvider sessionProvider, DialogHelper dialogHelper)
        {
            _navigationService = navigationService;
            _session = sessionProvider.CurrentSession!;
            _navigationService.ViewChanged += OnViewChanged;

            // ✨ الاشتراك في حدث طلب التنقل من Views الفرعية
            // يحل مشكلة SecurityRolesView → PermissionMatrix (التنقل المكسور)
            _navigationService.NavigationRequested += OnNavigationRequested;

            // ✨ الاشتراك في أحداث DialogHelper لعرض/إخفاء الغطاء الشفاف
            dialogHelper.OverlayShowRequested += async () => await this.ShowOverlayAsync();
            dialogHelper.OverlayHideRequested += async () => await this.HideOverlayAsync();

            InitializeComponent();
            NotificationManager.RegisterHost(NotificationHost);

            PopupUserName.Text = _session.FullName;
            PopupUserRole.Text = _session.RoleName;

            // Set avatar initials and first name in title bar and popup
            var nameParts = _session.FullName?.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            var initials = nameParts?.Length >= 2
                ? $"{nameParts[0][0]}{nameParts[1][0]}"
                : nameParts?.Length == 1 ? $"{nameParts[0][0]}" : "?";
            UserInitials.Text = initials;
            PopupUserInitials.Text = initials;

            var firstName = nameParts?.Length >= 1 ? nameParts[0] : "";
            UserFirstName.Text = _session.FullName;

            SidebarNav.NavigationRequested += OnNavigationRequested;
            BottomNav.NavigationRequested += OnNavigationRequested;

            InitializeNavigationItems();
            InitializeUI();
            InitializeOnAirWidget();
            InitializeFontScaleControls();
        }

        private void InitializeNavigationItems()
        {
            var mainItems = NavigationBuilder.CreateMainNavigation();
            ApplyPermissionFilter(mainItems);

            SidebarNav.NavigationItems = mainItems;

            var bottomItems = NavigationBuilder.CreateBottomNavigation();
            ApplyPermissionFilter(bottomItems);
            BottomNav.NavigationItems = bottomItems;
        }

        private void InitializeUI()
        {
            var homeView = _navigationService.NavigateTo("Home");
            if (homeView != null)
                MainContentArea.Content = homeView;
        }

        private void ApplyPermissionFilter(ObservableCollection<NavigationItem> items)
        {
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.RequiredPermission))
                {
                    item.IsVisible = _session.HasPermission(item.RequiredPermission);
                }
                else
                {
                    item.IsVisible = true;
                }
            }
        }

        private void OnViewChanged(string viewName)
        {
            UpdateBreadcrumb();
            BtnGoBack.Visibility = _navigationService.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
            SidebarNav.SetSelected(viewName);
            BottomNav.SetSelected(viewName);
        }

        private void UpdateBreadcrumb()
        {
            var path = string.Join(" / ", _navigationService.History.Reverse());
            BreadcrumbBar.Text = path;
        }

        public void NavigateToView(string viewName)
        {
            var view = _navigationService.NavigateTo(viewName);
            if (view != null)
            {
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                view.Opacity = 0;
                MainContentArea.Content = view;
                view.BeginAnimation(OpacityProperty, fadeIn);
            }
        }

        private void OnNavigationRequested(string viewName)
        {
            NavigateToView(viewName);
            if (_navigationMode == NavigationMode.Drawer)
                ToggleDrawer(false);
        }

        private void BtnGoBack_Click(object sender, RoutedEventArgs e)
        {
            var view = _navigationService.GoBack();
            if (view != null)
            {
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                view.Opacity = 0;
                MainContentArea.Content = view;
                view.BeginAnimation(OpacityProperty, fadeIn);
            }
        }

        private void BtnMenuToggle_Click(object sender, RoutedEventArgs e)
        {
            if (_navigationMode == NavigationMode.Drawer)
            {
                ToggleDrawer(!_isDrawerOpen);
            }
            else if (_navigationMode == NavigationMode.Collapsed)
            {
                ToggleDrawer(true);
            }
            else if (_navigationMode == NavigationMode.Expanded)
            {
                SetNavigationMode(NavigationMode.Collapsed);
            }
        }

        private void ToggleDrawer(bool open)
        {
            _isDrawerOpen = open;
            MenuIcon.Kind = open ? PackIconKind.MenuOpen : PackIconKind.Menu;
            SidebarNav.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetNavigationMode(NavigationMode mode)
        {
            _navigationMode = mode;
            MenuIcon.Kind = mode == NavigationMode.Expanded ? PackIconKind.Menu : PackIconKind.MenuOpen;

            switch (mode)
            {
                case NavigationMode.Expanded:
                    ColSidebar.MinWidth = 260;
                    ColSidebar.MaxWidth = 260;
                    ColSidebar.Width = GridLength.Auto;
                    SidebarNav.Visibility = Visibility.Visible;
                    BottomNav.Visibility = Visibility.Collapsed;
                    break;

                case NavigationMode.Collapsed:
                    ColSidebar.MinWidth = 72;
                    ColSidebar.MaxWidth = 72;
                    ColSidebar.Width = GridLength.Auto;
                    SidebarNav.Visibility = Visibility.Visible;
                    BottomNav.Visibility = Visibility.Collapsed;
                    break;

                case NavigationMode.Drawer:
                    ColSidebar.Width = new GridLength(0);
                    SidebarNav.Visibility = Visibility.Collapsed;
                    BottomNav.Visibility = Visibility.Visible;
                    break;
            }

            SidebarNav.Mode = mode == NavigationMode.Drawer ? NavigationMode.Expanded : mode;
        }

        private void Window_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            var width = e.NewSize.Width;
            NavigationMode newMode;

            if (width < 768)
                newMode = NavigationMode.Drawer;
            else if (width < 1200)
                newMode = NavigationMode.Collapsed;
            else
                newMode = NavigationMode.Expanded;

            if (newMode != _navigationMode)
            {
                SetNavigationMode(newMode);
            }
        }

        private void InitializeOnAirWidget()
        {
            _onAirTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _onAirTimer.Tick += (s, e) =>
            {
                if (_broadcastStartTime.HasValue)
                {
                    var elapsed = DateTime.Now - _broadcastStartTime.Value;
                    OnAirElapsedTime.Text = elapsed.ToString(@"hh\:mm\:ss");
                }
            };

            var pulse = new DoubleAnimation(1.0, 0.4, new Duration(TimeSpan.FromSeconds(1)))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            LivePulse.BeginAnimation(OpacityProperty, pulse);
        }

        public void StartBroadcast(string programName)
        {
            _broadcastStartTime = DateTime.Now;
            OnAirProgramName.Text = programName;
            OnAirWidget.Visibility = Visibility.Visible;
            _onAirTimer?.Start();
        }

        private void BtnStopOnAir_Click(object sender, RoutedEventArgs e)
        {
            _onAirTimer?.Stop();
            OnAirWidget.Visibility = Visibility.Collapsed;
            _broadcastStartTime = null;
        }

        private void BtnUserProfile_Click(object sender, RoutedEventArgs e)
        {
            UserProfilePopup.IsOpen = !UserProfilePopup.IsOpen;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            // ✨ تنظيف الذاكرة المؤقتة عند تسجيل الخروج لمنع تسرب الذاكرة
            _navigationService.ClearCache();

            var loginWindow = App.ServiceProvider.GetRequiredService<Forms.LoginWindow>();
            loginWindow.Show();

            // ✅ تعيين MainWindow صراحةً — WPF لا يحدّثها تلقائياً بعد إغلاق ModernMainWindow
            Application.Current.MainWindow = loginWindow;

            Close();
        }

        public new System.Threading.Tasks.Task ShowOverlay() => this.ShowOverlayAsync();
        public new System.Threading.Tasks.Task HideOverlay() => this.HideOverlayAsync();

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
                MaximizeIcon.Kind = PackIconKind.WindowMaximize;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                MaximizeIcon.Kind = PackIconKind.WindowRestore;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void InitializeFontScaleControls()
        {
            UpdateFontScaleUi();
            FontScaleService.ScaleChanged += _ => Dispatcher.BeginInvoke(UpdateFontScaleUi);
            PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
                return;

            if (e.Key == Key.OemPlus || e.Key == Key.Add)
            {
                FontScaleService.Increase();
                e.Handled = true;
            }
            else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            {
                FontScaleService.Decrease();
                e.Handled = true;
            }
            else if (e.Key == Key.D0 || e.Key == Key.NumPad0)
            {
                FontScaleService.Reset();
                e.Handled = true;
            }
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e) => FontScaleService.Increase();

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e) => FontScaleService.Decrease();

        private void BtnZoomReset_Click(object sender, RoutedEventArgs e) => FontScaleService.Reset();

        private void UpdateFontScaleUi()
        {
            if (ZoomPercentageText != null)
                ZoomPercentageText.Text = $"{FontScaleService.Percent}%";

            if (BtnZoomIn != null)
                BtnZoomIn.IsEnabled = FontScaleService.CanIncrease;
            if (BtnZoomOut != null)
                BtnZoomOut.IsEnabled = FontScaleService.CanDecrease;
        }
    }
}
