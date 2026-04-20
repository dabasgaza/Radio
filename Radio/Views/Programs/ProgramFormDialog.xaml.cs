using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System;
using System.Windows;

namespace Radio.Views.Programs
{
    public partial class ProgramFormDialog
    {
        private readonly IProgramService _programService;
        private readonly UserSession _session;
        private readonly ProgramDto _originalDto;

        // ⚠️ التعديل الهيكلي: استقبال DTO فقط وليس كيان الـ Database
        public ProgramFormDialog(ProgramDto dtoForEdit, IProgramService programService, UserSession session)
        {
            InitializeComponent();
            _programService = programService;
            _session = session;
            _originalDto = dtoForEdit;

            if (_originalDto != null)
            {
                TxtTitle.Text = "تعديل البرنامج";
                TxtName.Text = _originalDto.ProgramName;
                TxtCategory.Text = _originalDto.Category;
                TxtDesc.Text = _originalDto.ProgramDescription;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validation (UI Level)
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageService.Current.ShowWarning("يرجى إدخال اسم البرنامج.");
                return;
            }

            // 2. تجهيز الـ DTO
            var dto = new ProgramDto(
                _originalDto?.ProgramId ?? 0,
                TxtName.Text.Trim(),
                TxtCategory.Text.Trim(),
                TxtDesc.Text.Trim()
            );

            // 3. إدارة حالة الواجهة (منع الإرسال المتكرر)
            SetLoading(true);

            try
            {
                // 4. استدعاء الخدمة (النجاح يعني عدم رمي استثناء)
                if (_originalDto == null)
                    await _programService.CreateProgramAsync(dto, _session);
                else
                    await _programService.UpdateProgramAsync(dto, _session);

                MessageService.Current.ShowSuccess(_originalDto == null ? "تم إضافة البرنامج بنجاح" : "تم تعديل البرنامج بنجاح");

                this.DialogResult = true;
                this.Close();
            }
            catch (UnauthorizedAccessException)
            {
                // خطأ صلاحيات (مثلاً: المستخدم ليس لديه صلاحية الإضافة)
                MessageService.Current.ShowError("ليس لديك صلاحية لتنفيذ هذه العملية.");
            }
            catch (InvalidOperationException ex)
            {
                // خطأ في قواعد العمل (مثلاً: اسم البرنامج مكرر، أو قيمة غير صالحة)
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                // خطأ النظام (شبكة، قاعدة بيانات)
                MessageService.Current.ShowError("حدث خطأ أثناء حفظ البيانات.");
            }
            finally
            {
                // 5. إعادة تفعيل الواجهة في كل الأحوال
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
            if (e.ClickCount == 1) this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}