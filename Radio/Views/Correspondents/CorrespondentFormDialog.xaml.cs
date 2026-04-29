using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using DataAccess.Validation;
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

            IsWindowDraggable = true;

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
                var validation = ValidationPipeline.ValidateCorrespondent(dto);
                if (!validation.IsSuccess)
                {
                    MessageService.Current.ShowWarning(validation.ErrorMessage ?? "أخطاء في التحقق.");
                    return;
                }

                BtnSave.IsEnabled = false;

                Result result;
                if (_existing is null)
                    result = await _service.CreateAsync(dto, _session);
                else
                    result = await _service.UpdateAsync(dto, _session);

                if (result.IsSuccess)
                {
                    MessageService.Current.ShowSuccess(
                        _existing is null
                            ? "تمت إضافة المراسل بنجاح."
                            : "تم تعديل بيانات المراسل بنجاح.");

                    DialogResult = true;
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت العملية.");
                }
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حفظ بيانات المراسل.");
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}