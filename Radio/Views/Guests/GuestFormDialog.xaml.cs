using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using DataAccess.Validation;
using Radio.Views.Common;
using System.Windows;

namespace Radio.Views.Guests
{
    /// <summary>
    /// نافذة إضافة/تعديل ضيف — تتولى جمع البيانات والتحقق منها ثم إرسالها لـ GuestService.
    /// </summary>
    public partial class GuestFormDialog
    {
        private readonly IGuestService _guestService;
        private readonly UserSession _session;
        private readonly GuestDto? _existingGuest;

        public GuestFormDialog(GuestDto? guest, IGuestService guestService, UserSession session)
        {
            InitializeComponent();
            _existingGuest = guest;
            _guestService = guestService;
            _session = session;

            IsWindowDraggable = true;

            Title = _existingGuest is not null ? "تعديل بيانات الضيف" : "إضافة ضيف جديد";

            if (_existingGuest is not null)
            {
                TxtName.Text = _existingGuest.FullName;
                TxtOrg.Text = _existingGuest.Organization;
                TxtPhone.Text = _existingGuest.PhoneNumber;
                TxtEmail.Text = _existingGuest.EmailAddress;
            }
        }

        /// <summary>
        /// حفظ الضيف (إضافة أو تعديل) بعد التحقق من صحة المدخلات.
        /// </summary>
        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var dto = new GuestDto(
                _existingGuest?.GuestId ?? 0,
                TxtName.Text.Trim(),
                TxtOrg.Text.Trim(),
                TxtPhone.Text.Trim(),
                TxtEmail.Text.Trim(),
                TxtBio.Text.Trim(),
                string.Empty);

            try
            {
                var validation = ValidationPipeline.ValidateGuest(dto);
                if (!validation.IsSuccess)
                {
                    MessageService.Current.ShowWarning(validation.ErrorMessage ?? "أخطاء في التحقق.");
                    return;
                }

                BtnSave.IsEnabled = false;

                DataAccess.Common.Result result;
                if (_existingGuest is null)
                    result = await _guestService.CreateGuestAsync(dto, _session);
                else
                    result = await _guestService.UpdateGuestAsync(dto, _session);

                if (result.IsSuccess)
                {
                    MessageService.Current.ShowSuccess(
                        _existingGuest is null
                            ? "تمت إضافة الضيف بنجاح."
                            : "تم تعديل بيانات الضيف بنجاح.");

                    DialogResult = true;
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت العملية.");
                }
            }
            catch (ConcurrencyException ex)
            {
                var diag = new ConcurrencyDialog(ex.DatabaseValues);

                if (diag.ShowDialog() == true)
                {
                    try
                    {
                        Result retryResult;
                        if (_existingGuest is null)
                            retryResult = await _guestService.CreateGuestAsync(dto, _session);
                        else
                            retryResult = await _guestService.UpdateGuestAsync(dto, _session);

                        if (retryResult.IsSuccess)
                        {
                            MessageService.Current.ShowSuccess("تم الحفظ بنجاح بعد حل تعارض البيانات.");
                            DialogResult = true;
                        }
                        else
                        {
                            MessageService.Current.ShowWarning(
                                retryResult.ErrorMessage ?? "فشل الحفظ بعد محاولة حل التعارض.");
                        }
                    }
                    catch (ConcurrencyException)
                    {
                        MessageService.Current.ShowWarning(
                            "لا يزال هناك تعارض في البيانات. يرجى إغلاق النافذة وإعادة المحاولة.");
                    }
                    catch (Exception)
                    {
                        MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء إعادة المحاولة.");
                    }
                }
                else
                {
                    MessageService.Current.ShowInfo(
                        "تم إلغاء العملية. يرجى إغلاق النافذة وإعادة فتحها للحصول على أحدث نسخة.");
                }
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حفظ بيانات الضيف.");
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