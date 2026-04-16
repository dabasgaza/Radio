using BroadcastWorkflow.Services;
using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services.Messaging;
using Domain.Models;
using Radio.Common;
using Radio.Views.Common;
using System.Windows;

namespace Radio.Views.Guests
{
    /// <summary>
    /// Interaction logic for GuestFormDialog.xaml
    /// </summary>
    public partial class GuestFormDialog
    {
        private readonly IGuestService _guestService;
        private readonly UserSession _session;
        private readonly Guest? _existingGuest;

        public GuestFormDialog(Guest? guest, IGuestService guestService, UserSession session)
        {
            InitializeComponent();
            _existingGuest = guest;
            _guestService = guestService;
            _session = session;

            if (_existingGuest != null)
            {
                TxtTitle.Text = "تعديل بيانات الضيف";
                TxtName.Text = _existingGuest.FullName;
                TxtOrg.Text = _existingGuest.Organization;
                TxtPhone.Text = _existingGuest.PhoneNumber;
                TxtEmail.Text = _existingGuest.EmailAddress;
                
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var dto = new GuestDto
            (
                _existingGuest?.GuestId ?? 0,
                TxtName.Text,
                TxtOrg.Text,
                TxtPhone.Text,
                TxtEmail.Text,
                TxtBio.Text,
                String.Empty
            );

            try
            {
                BtnSave.IsEnabled = false;

                // استدعاء الخدمة مباشرة (هي ستتحقق من كل شيء)
                if (_existingGuest == null)
                    await _guestService.CreateGuestAsync(dto, _session);
                else
                    await _guestService.UpdateGuestAsync(dto, _session);

                this.DialogResult = true;
            }
            catch (ConcurrencyException ex)
            {
                // 👈 إظهار نافذة المقارنة
                var diag = new ConcurrencyDialog(ex.DatabaseValues);

                if (diag.ShowDialog() == true)
                {
                    // إذا اختار المستخدم "الدهس"، سنقوم بإعادة المحاولة 
                    // ولكن يجب أولاً تحديث RowVersion الخاص بالكائن المحلي ليتطابق مع الداتابيز
                    // (هذا الجزء متقدم، عادة نطلب من المستخدم إعادة التحميل لضمان سلامة البيانات)
                    MessageService.Current.ShowInfo("يرجى إغلاق النافذة وإعادة فتحها للحصول على أحدث نسخة.");
                }
            }
            catch (ValidationException ex) // 👈 اصطياد أخطاء البيانات
            {
                // عرض كافة الأخطاء بشكل أنيق عبر نظام الرسائل المركزي
                string allErrors = string.Join("\n", ex.Errors);
                MessageService.Current.ShowWarning(allErrors, "تنبيه في البيانات");
            }
            catch (Exception ex) // 👈 اصطياد أخطاء السيرفر
            {
                MessageService.Current.ShowError(ex.Message);
            }
            finally { BtnSave.IsEnabled = true; }
        }


    }
}
