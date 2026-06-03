using DataAccess.Common;
using MaterialDesignThemes.Wpf;
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

        private DateTime? _broadcastStartTime;
        private DispatcherTimer _onAirTimer;

        public ModernMainWindow(NavigationService navigationService, CurrentSessionProvider sessionProvider)
        {
            _navigationService = navigationService;
            _session = sessionProvider.CurrentSession!;
            _navigationService.ViewChanged += OnViewChanged;

            InitializeComponent();
            NotificationManager.RegisterHost(NotificationHost);

            ResponsiveNav.NavigationRequested += OnNavigationRequested;
            BottomNav.NavigationRequested += OnNavigationRequested;

            InitializeNavigationItems();
            InitializeUI();
            InitializeOnAirWidget();
            InitializeFontScaleControls();
        }

        private ObservableCollection<NavigationItem>? _navItems;

        private void InitializeNavigationItems()
        {
            _navItems = NavigationBuilder.CreateMainNavigation();
            ResponsiveNav.NavigationItems = _navItems;
            BottomNav.NavigationItems = NavigationBuilder.CreateBottomNavigation();
        }

        private void InitializeUI()
        {
            ApplyPermissionSecurity();
            var homeView = _navigationService.NavigateTo("Home");
            if (homeView != null)
                MainContentArea.Content = homeView;
        }

        private void OnViewChanged(string viewName)
        {
            UpdateBreadcrumb();
            BtnGoBack.Visibility = _navigationService.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
            ResponsiveNav.SetSelected(viewName);
            BottomNav.SetSelected(viewName);
        }

        private void UpdateBreadcrumb()
        {
            var path = string.Join(" / ", _navigationService.History.Reverse());
            BreadcrumbBar.Text = path;
        }

        private void ApplyPermissionSecurity()
        {
            if (_navItems != null)
            {
                foreach (var item in _navItems)
                {
                    if (!string.IsNullOrEmpty(item.RequiredPermission))
                    {
                        item.IsVisible = _session.HasPermission(item.RequiredPermission);
                    }
                }
            }

            if (BottomNav.NavigationItems != null)
            {
                foreach (var item in BottomNav.NavigationItems)
                {
                    if (!string.IsNullOrEmpty(item.RequiredPermission))
                    {
                        item.IsVisible = _session.HasPermission(item.RequiredPermission);
                    }
                }
            }
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
            if (_navigationMode == NavigationMode.Expanded)
            {
                ToggleDrawer(false);
            }
            else if (_navigationMode == NavigationMode.Collapsed)
            {
                ToggleDrawer(true);
            }
        }

        private bool _isDrawerOpen = false;
        private void ToggleDrawer(bool open)
        {
            _isDrawerOpen = open;
            MenuIcon.Kind = open ? PackIconKind.MenuOpen : PackIconKind.Menu;

            _navigationMode = open ? NavigationMode.Expanded : NavigationMode.Collapsed;
            ResponsiveNav.Mode = _navigationMode;

            ColSidebar.MinWidth = open ? 260 : 64;
            ColSidebar.MaxWidth = open ? 260 : 64;
            ColSidebar.Width = GridLength.Auto;
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
                _navigationMode = newMode;
                ColSidebar.MinWidth = newMode == NavigationMode.Drawer ? 0 :
                                    newMode == NavigationMode.Collapsed ? 64 : 260;
                ColSidebar.MaxWidth = newMode == NavigationMode.Drawer ? 260 :
                                     newMode == NavigationMode.Collapsed ? 64 : 260;
                ColSidebar.Width = new GridLength(
                    newMode == NavigationMode.Drawer ? (double)ColSidebar.ActualWidth :
                    newMode == NavigationMode.Collapsed ? 64 : 260);

                if (newMode == NavigationMode.Drawer)
                {
                    BottomNav.Visibility = Visibility.Visible;
                    ResponsiveNav.Visibility = Visibility.Collapsed;
                    AppTitle.Visibility = Visibility.Collapsed;
                    ColSidebar.Width = new GridLength(0);
                }
                else
                {
                    BottomNav.Visibility = Visibility.Collapsed;
                    ResponsiveNav.Visibility = Visibility.Visible;
                    AppTitle.Visibility = Visibility.Visible;
                    ColSidebar.Width = GridLength.Auto;
                }

                ResponsiveNav.Mode = newMode;
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

        public async Task ShowOverlay() => await this.ShowOverlayAsync();
        public async Task HideOverlay() => await this.HideOverlayAsync();

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
            FontScaleService.ScaleChanged += _ => Dispatcher.Invoke(UpdateFontScaleUi);
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