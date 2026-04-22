using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using DataAccess.Validation;
using Radio.Views.Common;
using System.ComponentModel.DataAnnotations;
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
                ValidationPipeline.ValidateGuest(dto);

                BtnSave.IsEnabled = false;

                if (_existingGuest is null)
                    await _guestService.CreateGuestAsync(dto, _session);
                else
                    await _guestService.UpdateGuestAsync(dto, _session);

                MessageService.Current.ShowSuccess(
                    _existingGuest is null
                        ? "تمت إضافة الضيف بنجاح."
                        : "تم تعديل بيانات الضيف بنجاح.");

                DialogResult = true;
            }
            catch (ConcurrencyException ex)
            {
                var diag = new ConcurrencyDialog(ex.DatabaseValues);

                if (diag.ShowDialog() == true)
                {
                    try
                    {
                        // ✅ لا حاجة لإعادة BtnSave.IsEnabled = false — finally الخارجي يكفي
                        if (_existingGuest is null)
                            await _guestService.CreateGuestAsync(dto, _session);
                        else
                            await _guestService.UpdateGuestAsync(dto, _session);

                        MessageService.Current.ShowSuccess("تم الحفظ بنجاح بعد حل تعارض البيانات.");
                        DialogResult = true;
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
                    // ✅ إزالة finally الداخلي — finally الخارجي يعيد تفعيل الزر
                }
                else
                {
                    MessageService.Current.ShowInfo(
                        "تم إلغاء العملية. يرجى إغلاق النافذة وإعادة فتحها للحصول على أحدث نسخة.");
                }
            }
            catch (ValidationException ex)
            {
                string allErrors = string.Join("\n", ex.Message);
                MessageService.Current.ShowWarning(allErrors);
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError(
                    _existingGuest is null
                        ? "ليس لديك صلاحية لإضافة ضيف جديد."
                        : "ليس لديك صلاحية لتعديل بيانات الضيوف.");
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
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