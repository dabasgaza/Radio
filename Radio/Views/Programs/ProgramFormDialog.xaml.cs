using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using DataAccess.Validation;
using System.ComponentModel.DataAnnotations;
using System.Windows;

namespace Radio.Views.Programs
{
    public partial class ProgramFormDialog
    {
        private readonly IProgramService _programService;
        private readonly UserSession _session;
        private readonly ProgramDto? _originalDto;

        public ProgramFormDialog(ProgramDto? dtoForEdit, IProgramService programService, UserSession session)
        {
            InitializeComponent();
            _programService = programService;
            _session = session;
            _originalDto = dtoForEdit;

            Title = _originalDto is not null ? "تعديل البرنامج" : "إضافة برنامج جديد";

            if (_originalDto is not null)
            {
                TxtName.Text = _originalDto.ProgramName;
                TxtCategory.Text = _originalDto.Category;
                TxtDesc.Text = _originalDto.ProgramDescription;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var dto = new ProgramDto(
                _originalDto?.ProgramId ?? 0,
                TxtName.Text.Trim(),
                TxtCategory.Text.Trim(),
                TxtDesc.Text.Trim());

            try
            {
                ValidationPipeline.ValidateProgram(dto);

                SetLoading(true);

                if (_originalDto is null)
                    await _programService.CreateProgramAsync(dto, _session);
                else
                    await _programService.UpdateProgramAsync(dto, _session);

                MessageService.Current.ShowSuccess(
                    _originalDto is null
                        ? "تم إضافة البرنامج بنجاح."
                        : "تم تعديل البرنامج بنجاح.");

                DialogResult = true;
            }
            catch (ValidationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError("ليس لديك صلاحية لتنفيذ هذه العملية.");
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ أثناء حفظ البيانات.");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void SetLoading(bool isLoading)
        {
            BtnSave.IsEnabled = !isLoading;
            TxtName.IsEnabled = !isLoading;
            TxtCategory.IsEnabled = !isLoading;
            TxtDesc.IsEnabled = !isLoading;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}