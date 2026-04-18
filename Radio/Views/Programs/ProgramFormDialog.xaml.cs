using DataAccess.DTOs;
using DataAccess.Services;
using Domain.Models;
using System.Windows;

namespace Radio.Views.Programs
{
    /// <summary>
    /// Interaction logic for ProgramFormDialog.xaml
    /// </summary>
    public partial class ProgramFormDialog
    {
        private readonly IProgramService _programService;
        private readonly UserSession _session;
        private readonly Program _existingProgram;

        public ProgramFormDialog(Program prog, IProgramService programService, UserSession session)
        {
            InitializeComponent();
            _existingProgram = prog;
            _programService = programService;
            _session = session;

            if (_existingProgram != null)
            {
                TxtTitle.Text = "تعديل البرنامج";
                TxtName.Text = _existingProgram.ProgramName;
                TxtCategory.Text = _existingProgram.Category;
                TxtDesc.Text = _existingProgram.ProgramDescription;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("اسم البرنامج مطلوب.");
                return;
            }

            var dto = new ProgramDto(
                _existingProgram?.ProgramId ?? 0,
                TxtName.Text.Trim(),
                TxtCategory.Text.Trim(),
                TxtDesc.Text.Trim()
            );

            try
            {
                if (_existingProgram == null)
                    await _programService.CreateProgramAsync(dto, _session);
                else
                    await _programService.UpdateProgramAsync(dto, _session);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الحفظ: " + ex.Message);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }

}
