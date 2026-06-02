using MaterialDesignThemes.Wpf;
using Radio.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Controls
{
    public partial class BottomNavigationView : UserControl
    {
        public static readonly DependencyProperty NavigationItemsProperty =
            DependencyProperty.Register(nameof(NavigationItems), typeof(IEnumerable<NavigationItem>),
                typeof(BottomNavigationView), new PropertyMetadata(null, OnNavigationItemsChanged));

        private readonly List<RadioButton> _buttons = new();
        private readonly Dictionary<string, PackIcon> _iconControls = new();

        public BottomNavigationView()
        {
            InitializeComponent();
        }

        public IEnumerable<NavigationItem>? NavigationItems
        {
            get => (IEnumerable<NavigationItem>)GetValue(NavigationItemsProperty);
            set => SetValue(NavigationItemsProperty, value);
        }

        public event Action<string>? NavigationRequested;

        private static void OnNavigationItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (BottomNavigationView)d;
            control.BuildNavigation(e.NewValue as IEnumerable<NavigationItem>);
        }

        private void BuildNavigation(IEnumerable<NavigationItem>? items)
        {
            NavPanel.Children.Clear();
            _buttons.Clear();
            _iconControls.Clear();

            if (items == null) return;

            foreach (var item in items.Where(i => i.IsVisible && !i.IsSeparator).Take(5))
            {
                var icon = new PackIcon
                {
                    Kind = item.Icon,
                    Width = 20,
                    Height = 20
                };

                var button = new RadioButton
                {
                    Tag = item.Label,
                    DataContext = item,
                    Style = (Style)FindResource("BottomNav.Item"),
                    SnapsToDevicePixels = true
                };

                var sp = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Children =
                    {
                        icon,
                        new TextBlock
                        {
                            Text = item.Label,
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Center
                        }
                    }
                };

                button.Content = sp;

                _iconControls[item.Route ?? item.Label] = icon;

                button.Click += (s, e) => NavigationRequested?.Invoke(item.Route ?? item.Label);
                _buttons.Add(button);
                NavPanel.Children.Add(button);
            }
        }

        public void SetSelected(string routeName)
        {
            foreach (var kvp in _iconControls)
            {
                var icon = kvp.Value;
                var isSelected = kvp.Key == routeName;
                var item = _buttons
                    .FirstOrDefault(b => (b.DataContext as NavigationItem)?.Route == kvp.Key ||
                                        (b.DataContext as NavigationItem)?.Label == kvp.Key)
                    ?.DataContext as NavigationItem;

                icon.Kind = isSelected
                    ? (item?.ActiveIcon ?? icon.Kind)
                    : (item?.Icon ?? icon.Kind);
            }

            foreach (var button in _buttons)
            {
                button.IsChecked = false;
                var item = button.DataContext as NavigationItem;
                if (item?.Route == routeName || item?.Label == routeName)
                {
                    button.IsChecked = true;
                    break;
                }
            }
        }
    }
}
