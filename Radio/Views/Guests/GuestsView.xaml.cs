using BroadcastWorkflow.Services;
using Domain.Models;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Guests
{
    /// <summary>
    /// Interaction logic for GuestsView.xaml
    /// </summary>
    public partial class GuestsView : UserControl
    {
        private readonly IGuestService _guestService;
        private readonly UserSession _session;
        private List<Guest> _allGuests = new();

        // Dependency Property for Role-based UI visibility in DataGrid
        public Visibility CanManageGuests => _session.HasPermission("GUEST_MANAGE") ? Visibility.Visible : Visibility.Collapsed;

        public GuestsView(IGuestService guestService, UserSession session)
        {
            InitializeComponent();
            _guestService = guestService;
            _session = session;

            BtnAddGuest.Visibility = CanManageGuests;
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            _allGuests = await _guestService.GetAllActiveAsync();
            DgGuests.ItemsSource = _allGuests;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = TxtSearch.Text.ToLower();
            DgGuests.ItemsSource = _allGuests.Where(g =>
                g.FullName.ToLower().Contains(filter) ||
                (g.PhoneNumber ?? "").Contains(filter)).ToList();
        }

        private async void BtnAddGuest_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new GuestFormDialog(null, _guestService, _session);
            if (dialog.ShowDialog() == true) await LoadDataAsync();
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Guest guest)
            {
                var dialog = new GuestFormDialog(guest, _guestService, _session);
                if (dialog.ShowDialog() == true) await LoadDataAsync();
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Guest guest)
            {
                var result = MessageBox.Show($"هل أنت متأكد من حذف الضيف: {guest.FullName}؟", "تأكيد الحذف",
                                            MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _guestService.SoftDeleteGuestAsync(guest.GuestId, _session);
                        await LoadDataAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "خطأ في الحذف", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
