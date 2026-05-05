using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Radio.Views.Episodes
{
    /// <summary>
    /// ViewModel لكل منصة — يدعم INotifyPropertyChanged للتحديث المباشر
    ///
    /// ⚠ تم نقل هذا الكلاس إلى ملف منفصل لتفادي خطأ CS0229
    /// </summary>
    public class PlatformSelectionVm : INotifyPropertyChanged
    {
        public int SocialMediaPlatformId { get; set; }
        public string PlatformName { get; set; } = string.Empty;
        public string PlatformIcon { get; set; } = "ShareVariant";

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private string? _url;
        public string? Url
        {
            get => _url;
            set { _url = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
