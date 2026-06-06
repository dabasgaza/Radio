using MaterialDesignThemes.Wpf;
using Radio.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Radio.Controls
{
    public partial class BottomNavigationView : UserControl
    {
        private IEnumerable<NavigationItem>? _items;

        public static readonly DependencyProperty NavigationItemsProperty =
            DependencyProperty.Register(nameof(NavigationItems), typeof(IEnumerable<NavigationItem>),
                typeof(BottomNavigationView), new PropertyMetadata(null, OnItemsChanged));

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

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (BottomNavigationView)d;
            if (e.NewValue is IEnumerable<NavigationItem> items)
            {
                control._items = items;
                control.BuildItems();
            }
        }

        private void BuildItems()
        {
            if (_items == null) return;

            var visibleItems = _items.Where(i => i.IsVisible && i.Route != null).Take(5).ToList();
            NavItemsControl.ItemsSource = null;
            NavItemsControl.ItemTemplate = null;

            var factory = new FrameworkElementFactory(typeof(RadioButton));
            factory.SetValue(RadioButton.StyleProperty, FindResource("BottomNavItem"));
            factory.SetValue(RadioButton.TagProperty, new Binding("Label"));
            factory.SetValue(RadioButton.IsCheckedProperty, new Binding("IsChecked") { Mode = BindingMode.TwoWay });
            factory.AddHandler(RadioButton.ClickEvent, new RoutedEventHandler(OnNavClick));

            var spFactory = new FrameworkElementFactory(typeof(StackPanel));
            spFactory.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);

            var iconFactory = new FrameworkElementFactory(typeof(PackIcon));
            iconFactory.SetValue(PackIcon.WidthProperty, 20.0);
            iconFactory.SetValue(PackIcon.HeightProperty, 20.0);
            iconFactory.SetValue(PackIcon.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            iconFactory.SetValue(PackIcon.KindProperty, new Binding("Icon"));

            var labelFactory = new FrameworkElementFactory(typeof(TextBlock));
            labelFactory.SetValue(TextBlock.TextProperty, new Binding("Label"));
            labelFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            labelFactory.SetValue(TextBlock.FontSizeProperty, FindResource("FontSize.Micro"));
            labelFactory.SetValue(TextBlock.FontWeightProperty, System.Windows.FontWeights.Medium);

            spFactory.AppendChild(iconFactory);
            spFactory.AppendChild(labelFactory);

            factory.SetValue(RadioButton.ContentProperty, spFactory);

            var template = new DataTemplate(typeof(NavigationItem));
            template.VisualTree = factory;

            NavItemsControl.ItemTemplate = template;
            NavItemsControl.ItemsSource = visibleItems;
        }

        private void OnNavClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is NavigationItem item)
            {
                NavigationRequested?.Invoke(item.Route ?? item.Label);
            }
        }

        public void SetSelected(string routeName)
        {
            if (_items == null) return;

            foreach (var item in _items)
            {
                item.IsChecked = (item.Route ?? item.Label) == routeName;
            }
        }
    }
}
