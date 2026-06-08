using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Radio.Controls
{
    public partial class SkeletonLoader : UserControl
    {
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(SkeletonLoader),
                new PropertyMetadata(true, OnIsLoadingChanged));

        public static readonly DependencyProperty ChildContentProperty =
            DependencyProperty.Register(nameof(ChildContent), typeof(object), typeof(SkeletonLoader),
                new PropertyMetadata(null));

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public object ChildContent
        {
            get => GetValue(ChildContentProperty);
            set => SetValue(ChildContentProperty, value);
        }

        private Grid? _skeletonContent;
        private ContentPresenter? _realContent;

        public SkeletonLoader()
        {
            InitializeComponent();
            Loaded += SkeletonLoader_Loaded;
            Unloaded += SkeletonLoader_Unloaded;
        }

        private void EnsureParts()
        {
            if (_skeletonContent is not null && _realContent is not null) return;

            var rootGrid = FindDescendant<Grid>(this);
            if (rootGrid is null) return;

            int count = VisualTreeHelper.GetChildrenCount(rootGrid);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(rootGrid, i);
                if (child is Grid g && g != rootGrid)
                    _skeletonContent = g;
                else if (child is ContentPresenter cp)
                    _realContent = cp;
            }
        }

        private static T? FindDescendant<T>(DependencyObject parent)
            where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                    return tChild;
                var result = FindDescendant<T>(child);
                if (result is not null)
                    return result;
            }
            return null;
        }

        private void SkeletonLoader_Loaded(object sender, RoutedEventArgs e)
        {
            EnsureParts();
            if (IsLoading)
            {
                StartShimmer();
            }
        }

        private void SkeletonLoader_Unloaded(object sender, RoutedEventArgs e)
        {
            StopShimmer();
        }

        private void StartShimmer()
        {
            var brush = (LinearGradientBrush)FindResource("ShimmerBrush");
            var animation = new DoubleAnimation
            {
                From = -1,
                To = 2,
                Duration = TimeSpan.FromSeconds(1.8),
                RepeatBehavior = RepeatBehavior.Forever
            };
            brush.GradientStops[0].BeginAnimation(GradientStop.OffsetProperty, animation);
        }

        private void StopShimmer()
        {
            if (FindResource("ShimmerBrush") is LinearGradientBrush brush)
            {
                brush.GradientStops[0].BeginAnimation(GradientStop.OffsetProperty, null);
            }
        }

        private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SkeletonLoader)d;
            bool isLoading = (bool)e.NewValue;

            control.EnsureParts();

            if (control._skeletonContent is not null)
                control._skeletonContent.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            if (control._realContent is not null)
                control._realContent.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;

            if (isLoading)
            {
                control.StartShimmer();
            }
            else
            {
                control.StopShimmer();

                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                control._realContent?.BeginAnimation(OpacityProperty, fadeIn);
            }
        }
    }
}
