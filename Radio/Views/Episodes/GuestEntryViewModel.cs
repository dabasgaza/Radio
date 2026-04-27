using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Radio.Views.Episodes
{
    public class GuestEntryViewModel : INotifyPropertyChanged
    {
        private int _guestId;
        public int GuestId
        {
            get => _guestId;
            set { _guestId = value; OnPropertyChanged(); }
        }

        private string? _topic;
        public string? Topic
        {
            get => _topic;
            set { _topic = value; OnPropertyChanged(); }
        }

        private TimeSpan? _hostingTime;
        public TimeSpan? HostingTime
        {
            get => _hostingTime;
            set { _hostingTime = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
