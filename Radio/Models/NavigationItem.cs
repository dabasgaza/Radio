using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Radio.Models
{
    public class NavigationItem : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private bool _isChecked;
        private bool _isVisible = true;

        public string Label { get; set; } = string.Empty;
        public PackIconKind Icon { get; set; }
        public string? Route { get; set; }
        public string? RequiredPermission { get; set; }
        public bool IsSeparator { get; set; }
        public bool IsGroup => Children.Count > 0;
        public ObservableCollection<NavigationItem> Children { get; set; } = new();

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsChecked));
            }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
