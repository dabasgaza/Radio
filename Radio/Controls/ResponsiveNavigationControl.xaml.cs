using MaterialDesignThemes.Wpf;
using Radio.Models;
using Radio.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Radio.Controls
{
    public enum NavigationMode
    {
        Expanded,
        Collapsed,
        Drawer
    }

    public partial class ResponsiveNavigationControl : UserControl
    {
        public static readonly DependencyProperty NavigationItemsProperty =
            DependencyProperty.Register(nameof(NavigationItems), typeof(IEnumerable<NavigationItem>),
                typeof(ResponsiveNavigationControl), new PropertyMetadata(null));

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(nameof(Mode), typeof(NavigationMode),
                typeof(ResponsiveNavigationControl), new PropertyMetadata(NavigationMode.Expanded, OnModeChanged));

        public static readonly DependencyProperty NavigationCommandProperty =
            DependencyProperty.Register(nameof(NavigationCommand), typeof(RoutedCommand),
                typeof(ResponsiveNavigationControl), new PropertyMetadata(null));

        private readonly Dictionary<string, RadioButton> _buttons = new();
        private readonly Dictionary<string, PackIcon> _iconControls = new();
        private NavigationMode _currentMode = NavigationMode.Expanded;
        private bool _isInitialized = false;

        public event Action<string>? NavigationRequested;

        public ResponsiveNavigationControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            FontScaleService.ScaleChanged += OnFontScaleChanged;
        }

        private void OnFontScaleChanged(double _)
        {
            if (!_isInitialized || NavigationItems == null)
                return;

            var selected = _buttons.FirstOrDefault(kvp => kvp.Value.IsChecked == true).Key;
            BuildNavigation(NavigationItems);
            if (!string.IsNullOrEmpty(selected))
                SetSelected(selected);
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

        public RoutedCommand? NavigationCommand
        {
            get => (RoutedCommand)GetValue(NavigationCommandProperty);
            set => SetValue(NavigationCommandProperty, value);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized && NavigationItems != null)
            {
                BuildNavigation(NavigationItems);
                _isInitialized = true;
            }
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ResponsiveNavigationControl)d;
            control._currentMode = (NavigationMode)e.NewValue;
            control.ApplyMode();
        }

        private void ApplyMode()
        {
            var styleKey = _currentMode == NavigationMode.Collapsed ? "NavRail.Item.Collapsed" : "NavRail.Item";

            AccentBorder.Visibility = _currentMode == NavigationMode.Collapsed
                ? Visibility.Collapsed
                : Visibility.Visible;

            RootBorder.Width = _currentMode == NavigationMode.Expanded ? 260 :
                              _currentMode == NavigationMode.Collapsed ? 80 : 260;

            foreach (var kvp in _buttons)
            {
                var button = kvp.Value;
                button.Style = (Style)FindResource(styleKey);

                if (_currentMode == NavigationMode.Collapsed)
                {
                    button.Tag = "";
                    button.Padding = new Thickness(0);
                }
                else
                {
                    var item = button.DataContext as NavigationItem;
                    button.Tag = item?.Label ?? "";
                }
            }
        }

        private void BuildNavigation(IEnumerable<NavigationItem> items)
        {
            NavPanel.Children.Clear();
            _buttons.Clear();
            _iconControls.Clear();

            foreach (var item in items.Where(i => i.IsVisible))
            {
                if (item.IsSeparator)
                {
                    NavPanel.Children.Add(new Separator { Style = (Style)FindResource("NavRail.Divider") });
                    continue;
                }

                if (item.IsSectionHeader && !string.IsNullOrEmpty(item.Label))
                {
                    NavPanel.Children.Add(new TextBlock
                    {
                        Text = item.Label.ToUpper(),
                        Style = (Style)FindResource("SectionHeader")
                    });
                    continue;
                }

                var button = CreateNavButton(item);
                _buttons[item.Route ?? item.Label] = button;
                NavPanel.Children.Add(button);

                if (item.IsGroup && item.Children.Count > 0)
                {
                    foreach (var child in item.Children.Where(c => c.IsVisible))
                    {
                        var subButton = CreateNavButton(child, true);
                        _buttons[child.Route ?? child.Label] = subButton;
                        NavPanel.Children.Add(subButton);
                    }
                }
            }

            ApplyMode();
        }

        private RadioButton CreateNavButton(NavigationItem item, bool isSubItem = false)
        {
            var iconControl = new PackIcon
            {
                Kind = item.Icon,
                Width = 24,
                Height = 24,
                VerticalAlignment = VerticalAlignment.Center
            };

            var button = new RadioButton
            {
                Tag = item.Label,
                DataContext = item,
                Style = (Style)FindResource(isSubItem ? "NavRail.SubItem" : "NavRail.Item"),
                SnapsToDevicePixels = true
            };

            var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(iconControl);
            sp.Children.Add(new TextBlock
            {
                Text = item.Label,
                Margin = isSubItem ? new Thickness(16, 0, 0, 0) : new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = isSubItem
                    ? FontScaleService.GetScaled("FontSize.Button")
                    : FontScaleService.GetScaled("FontSize.Input"),
                Foreground = FindResource("OnSurfaceVariantBrush") as Brush
            });

            button.Content = sp;

            _iconControls[item.Route ?? item.Label] = iconControl;

            button.Click += (s, e) => NavigationRequested?.Invoke(item.Route ?? item.Label);
            return button;
        }

        public void SetSelected(string routeName)
        {
            foreach (var kvp in _buttons)
            {
                var button = kvp.Value;
                button.IsChecked = kvp.Key == routeName;

                var item = button.DataContext as NavigationItem;
                if (_iconControls.TryGetValue(kvp.Key, out var icon) && item != null)
                {
                    icon.Kind = kvp.Key == routeName
                        ? (item.ActiveIcon ?? item.Icon)
                        : item.Icon;
                }
            }
        }
    }
}
