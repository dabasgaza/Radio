using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
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
        private List<GuestDto> _allGuests = new();

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
                // 👈 استدعاء رسالة التأكيد بأسلوب أنيق جداً (ينتظر رد المستخدم)
                bool isConfirmed = await MessageService.Current.ShowConfirmationAsync(
                    $"هل أنت متأكد من رغبتك بحذف الضيف: {guest.FullName}؟\nلا يمكن التراجع عن هذا الإجراء.",
                    "تأكيد الحذف");

                if (isConfirmed)
                {
                    try
                    {
                        await _guestService.SoftDeleteGuestAsync(guest.GuestId, _session);
                        await LoadDataAsync();

                        // لن نرسل رسالة نجاح هنا، لأن الـ Service نفسه سيتولى إرسالها!
                    }
                    catch (Exception ex)
                    {
                        MessageService.Current.ShowError(ex.Message);
                    }
                }
            }

        }
    }
}
