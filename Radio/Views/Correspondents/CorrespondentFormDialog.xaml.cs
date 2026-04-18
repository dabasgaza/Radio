using DataAccess.DTOs;
using DataAccess.Services;
using Domain.Models;
using System.Windows;

namespace Radio.Views.Correspondents
{
    /// <summary>
    /// Interaction logic for CorrespondentFormDialog.xaml
    /// </summary>
    public partial class CorrespondentFormDialog
    {
        private readonly ICorrespondentService _service;
        private readonly UserSession _session;
        private readonly Correspondent? _existing;

        public CorrespondentFormDialog(Correspondent? existing, ICorrespondentService service, UserSession session)
        {
            InitializeComponent(); // Ensure .xaml matches fields in GuestFormDialog
            _existing = existing;
            _service = service;
            _session = session;

            if (_existing != null)
            {
                TxtName.Text = _existing.FullName;
                TxtPhone.Text = _existing.PhoneNumber;
                TxtLocations.Text = _existing.AssignedLocations;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var dto = new CorrespondentDto(_existing?.CorrespondentId ?? 0, TxtName.Text, TxtPhone.Text, TxtLocations.Text);
            try
            {
                if (_existing == null) await _service.CreateAsync(dto, _session);
                else await _service.UpdateAsync(dto, _session);
                DialogResult = true;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
