using System.ComponentModel;

namespace DataAccess.DTOs
{
    public class PermissionViewModel : INotifyPropertyChanged
    {
        public int PermissionId { get; set; }
        public required string DisplayName { get; set; }
        public required string Module { get; set; }
        public bool IsAssigned { get; set; } = false;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
