using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using DataAccess.Validation;
using System.ComponentModel.DataAnnotations;
using System.Windows;

namespace Radio.Views.Correspondents
{
    /// <summary>
    /// نافذة إضافة/تعديل مراسل — تتولى جمع البيانات والتحقق منها ثم إرسالها للـ Service.
    /// </summary>
    public partial class CorrespondentFormDialog
    {
        private readonly ICorrespondentService _service;
        private readonly UserSession _session;
        private readonly CorrespondentDto? _existing;

        public CorrespondentFormDialog(CorrespondentDto? existing, ICorrespondentService service, UserSession session)
        {
            InitializeComponent();
            _existing = existing;
            _service = service;
            _session = session;

            // تعبئة الحقول في حالة التعديل
            if (_existing is not null)
            {
                Title = "تعديل بيانات المراسل";
                TxtName.Text = _existing.FullName;
                TxtPhone.Text = _existing.PhoneNumber;
                TxtLocations.Text = _existing.AssignedLocations;
            }
            else
            {
                Title = "إضافة مراسل جديد";
            }
        }

        /// <summary>
        /// حفظ المراسل (إضافة أو تعديل) بعد التحقق من صحة المدخلات.
        /// </summary>
        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var dto = new CorrespondentDto(
                _existing?.CorrespondentId ?? 0,
                TxtName.Text.Trim(),
                TxtPhone.Text.Trim(),
                TxtLocations.Text.Trim());

            try
            {
                // ✅ التحقق عبر ValidationPipeline — يطرح ValidationException
                ValidationPipeline.ValidateCorrespondent(dto);

                if (_existing is null)
                    await _service.CreateAsync(dto, _session);
                else
                    await _service.UpdateAsync(dto, _session);

                MessageService.Current.ShowSuccess(
                    _existing is null
                        ? "تمت إضافة المراسل بنجاح."
                        : "تم تعديل بيانات المراسل بنجاح.");

                DialogResult = true;
            }
            catch (ValidationException ex)
            {
                // ✅ أخطاء التحقق من المدخلات — تعرض كتحذير
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError(
                    _existing is null
                        ? "ليس لديك صلاحية لإضافة مراسل جديد."
                        : "ليس لديك صلاحية لتعديل بيانات المراسلين.");
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حفظ بيانات المراسل.");
            }
        }

        /// <summary>
        /// سحب النافذة عبر شريط العنوان (مع حماية من خطأ أثناء التصميم).
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                DragMove();
        }

        /// <summary>
        /// إغلاق النافذة.
        /// </summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}