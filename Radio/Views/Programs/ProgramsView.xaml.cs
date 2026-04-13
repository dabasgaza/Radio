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
                var programs = await _programService.GetAllActiveAsync();
                DgPrograms.ItemsSource = programs;
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
            if (sender is Button btn && btn.DataContext is Program prog)
            {
                var dialog = new ProgramFormDialog(prog, _programService, _session);
                if (dialog.ShowDialog() == true)
                {
                    await LoadDataAsync();
                }
            }
        }
    }

}
