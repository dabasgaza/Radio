using DataAccess.DTOs;
using DataAccess.Services;
using Domain.Models;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Programs
{
    /// <summary>
    /// Interaction logic for ProgramsView.xaml
    /// </summary>
    public partial class ProgramsView : UserControl
    {
        private readonly IProgramService _programService;
        private readonly UserSession _session;
        private List<ProgramDto> _allPrograms = new();

        public ProgramsView(IProgramService programService, UserSession session)
        {
            InitializeComponent();
            _programService = programService;
            _session = session;

            // فقط قسم التنسيق يمكنه إضافة برامج جديدة
            BtnAddProgram.Visibility = _session.HasPermission("PROGRAM_MANAGE") ? Visibility.Visible : Visibility.Collapsed;

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _allPrograms = await _programService.GetAllActiveAsync();
                DgPrograms.ItemsSource = _allPrograms;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل البرامج: " + ex.Message);
            }
        }

        private async void BtnAddProgram_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProgramFormDialog(null, _programService, _session);
            if (dialog.ShowDialog() == true)
            {
                await LoadDataAsync();
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ProgramDto prog)
            {
                var dialog = new ProgramFormDialog(prog, _programService, _session);
                if (dialog.ShowDialog() == true)
                {
                    await LoadDataAsync();
                }
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = TxtSearch.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                string filter = TxtSearch.Text.ToLower();

                DgPrograms.ItemsSource = _allPrograms.Where(g =>
                    g.ProgramName.ToLower().Contains(filter)).ToList();

            }
            else
            {
                var filtered = _allPrograms
                    .Where(p => p.ProgramName.ToLower().Contains(keyword))
                    .ToList();

                DgPrograms.ItemsSource = filtered;
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {

        }
    }

}
