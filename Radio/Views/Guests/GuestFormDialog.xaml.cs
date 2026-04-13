using BroadcastWorkflow.Services;
using DataAccess.DTOs;
using Domain.Models;
using System.Windows;

namespace Radio.Views.Guests
{
    /// <summary>
    /// Interaction logic for GuestFormDialog.xaml
    /// </summary>
    public partial class GuestFormDialog
    {
        private readonly IGuestService _guestService;
        private readonly UserSession _session;
        private readonly Guest? _existingGuest;

        public GuestFormDialog(Guest? guest, IGuestService guestService, UserSession session)
        {
            InitializeComponent();
            _existingGuest = guest;
            _guestService = guestService;
            _session = session;

            if (_existingGuest != null)
            {
                TxtTitle.Text = "تعديل بيانات الضيف";
                TxtName.Text = _existingGuest.FullName;
                TxtOrg.Text = _existingGuest.Organization;
                TxtPhone.Text = _existingGuest.PhoneNumber;
                TxtEmail.Text = _existingGuest.EmailAddress;
                TxtBio.Text = _existingGuest.Bio;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // UI Validation
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("الاسم الكامل مطلوب."); return;
            }
            if (string.IsNullOrWhiteSpace(TxtPhone.Text) && string.IsNullOrWhiteSpace(TxtEmail.Text))
            {
                MessageBox.Show("يجب إدخال رقم الهاتف أو البريد الإلكتروني على الأقل."); return;
            }

            var dto = new GuestDto(
                _existingGuest?.GuestId ?? 0,
                TxtName.Text.Trim(),
                TxtOrg.Text.Trim(),
                TxtPhone.Text.Trim(),
                TxtEmail.Text.Trim(),
                TxtBio.Text.Trim(),
                null
            );

            try
            {
                if (_existingGuest == null)
                    await _guestService.CreateGuestAsync(dto, _session);
                else
                    await _guestService.UpdateGuestAsync(dto, _session);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
