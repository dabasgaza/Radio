using Radio.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Radio.Controls
{
    public enum NavigationMode
    {
        Expanded,
        Collapsed,
        Drawer
    }

    public partial class ModernSidebarControl : UserControl
    {
        public static readonly DependencyProperty NavigationItemsProperty =
            DependencyProperty.Register(nameof(NavigationItems), typeof(IEnumerable<NavigationItem>),
                typeof(ModernSidebarControl), new PropertyMetadata(null, OnNavigationItemsChanged));

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(nameof(Mode), typeof(NavigationMode),
                typeof(ModernSidebarControl), new PropertyMetadata(NavigationMode.Expanded, OnModeChanged));

        private NavCategory _currentCategory = NavCategory.Broadcast;
        private ObservableCollection<NavigationItem>? _allItems;
        private NavigationMode _mode = NavigationMode.Expanded;

        public event Action<string>? NavigationRequested;

        public ModernSidebarControl()
        {
            InitializeComponent();
        }

        public IEnumerable<NavigationItem>? NavigationItems
        {
            get => (IEnumerable<NavigationItem>)GetValue(NavigationItemsProperty);
            set => SetValue(NavigationItemsProperty, value);
        }

        public NavigationMode Mode
        {
            get => (NavigationMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        private static void OnNavigationItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ModernSidebarControl)d;
            if (e.NewValue is IEnumerable<NavigationItem> items)
            {
                control._allItems = new ObservableCollection<NavigationItem>(items.Where(i => i.IsVisible));
                control.ApplyFilter();
            }
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ModernSidebarControl)d;
            control._mode = (NavigationMode)e.NewValue;
            control.ApplyMode();
        }

        private void ApplyMode()
        {
            bool isExpanded = _mode == NavigationMode.Expanded;

            // 244px and 56px widths leave exactly 8px margin on each side inside 260px/72px Grid columns
            double targetWidth = isExpanded ? 244 : 56;

            // ── Smooth Width Transition Animation ──
            var widthAnimation = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            RootBorder.BeginAnimation(WidthProperty, widthAnimation);

            // ── Floating Card Corner Radius (Fully rounded corners) ──
            RootBorder.CornerRadius = new CornerRadius(16);

            TabsExpanded.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
            TabsCollapsed.Visibility = isExpanded ? Visibility.Collapsed : Visibility.Visible;

            DashboardBtn.Style = isExpanded
                ? (Style)FindResource("GlassNavItem")
                : (Style)FindResource("GlassNavItemMin");

            if (isExpanded)
            {
                DashboardBtn.Margin = new Thickness(4, 6, 4, 4);
                TabSeparator.Margin = new Thickness(12, 6, 12, 4);
            }
            else
            {
                DashboardBtn.Margin = new Thickness(4, 6, 4, 4);
                TabSeparator.Margin = new Thickness(8, 4, 8, 2);
            }

            ApplyFilter();
        }

        private void TabBroadcast_Click(object sender, RoutedEventArgs e)
        {
            _currentCategory = NavCategory.Broadcast;
            TabBroadcast.IsChecked = true;
            TabBroadcastMin.IsChecked = true;
            TabSystem.IsChecked = false;
            TabSystemMin.IsChecked = false;
            ApplyFilter();
        }

        private void TabSystem_Click(object sender, RoutedEventArgs e)
        {
            _currentCategory = NavCategory.System;
            TabBroadcast.IsChecked = false;
            TabBroadcastMin.IsChecked = false;
            TabSystem.IsChecked = true;
            TabSystemMin.IsChecked = true;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (_allItems == null) return;

            bool isExpanded = _mode == NavigationMode.Expanded;

            // Filter out Home/Dashboard route since it is hardcoded statically at the top of the Sidebar
            var filtered = _allItems
                .Where(i => i.Route != "Home")
                .Where(i => i.Category == null || i.Category == _currentCategory)
                .Where(i => i.IsVisible)
                .ToList();

            var template = (DataTemplate)FindResource(isExpanded ? "ExpandedItemTemplate" : "CollapsedItemTemplate");

            NavItemsControl.ItemsSource = null;
            NavItemsControl.ItemTemplate = template;
            NavItemsControl.ItemsSource = filtered;
        }

        private void NavItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb == DashboardBtn)
            {
                SetSelected("Home");
                NavigationRequested?.Invoke("Home");
                return;
            }

            if (sender is FrameworkElement fe && fe.DataContext is NavigationItem item)
            {
                SetSelected(item.Route ?? item.Label);
                NavigationRequested?.Invoke(item.Route ?? item.Label);
            }
        }

        public void SetSelected(string routeName)
        {
            if (routeName == "Home" || routeName == "لوحة التحكم")
            {
                DashboardBtn.IsChecked = true;
                if (_allItems != null)
                {
                    foreach (var item in _allItems)
                    {
                        item.IsChecked = false;
                    }
                }
                return;
            }

            DashboardBtn.IsChecked = false;

            if (_allItems == null) return;
            foreach (var item in _allItems)
            {
                var itemRoute = item.Route ?? item.Label;
                item.IsChecked = itemRoute == routeName;
            }
        }
    }
}
