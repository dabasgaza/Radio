using BroadcastWorkflow.Services;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Windows;

namespace Radio.Views.Correspondents
{
    /// <summary>
    /// Interaction logic for CoverageFormDialog.xaml
    /// </summary>
    public partial class CoverageFormDialog
    {
        private readonly ICoverageService _coverageService;
        private readonly ICorrespondentService _correspondentService;
        private readonly IGuestService _guestService;
        private readonly UserSession _session;
        private readonly CoverageDto? _existingCoverage;

        public CoverageFormDialog(ICoverageService coverageService, ICorrespondentService correspondentService, IGuestService guestService, UserSession session, CoverageDto? existingCoverage = null)
        {
            _coverageService = coverageService;
            _correspondentService = correspondentService;
            _guestService = guestService;
            _session = session;
            _existingCoverage = existingCoverage;

            InitializeComponent();

            // 2. تحميل البيانات الأولية
            _ = LoadInitialDataAsync();

        }

        /// <summary>
        /// تحميل قوائم المراسلين والضيوف لملء الـ ComboBoxes
        /// </summary>
        private async Task LoadInitialDataAsync()
        {
            try
            {
                // تحميل البيانات بالتوازي لتحسين الأداء
                var correspondentsTask = _correspondentService.GetAllActiveAsync();
                var guestsTask = _guestService.GetAllActiveAsync();

                await Task.WhenAll(correspondentsTask, guestsTask);

                CboCorrespondents.ItemsSource = await correspondentsTask;
                CboGuests.ItemsSource = await guestsTask;

                // 3. إذا كنا في وضع التعديل، نقوم بملء الحقول
                if (_existingCoverage != null)
                {
                    TxtTitle.Text = "تعديل بيانات التغطية الميدانية";
                    BtnSave.Content = "حفظ التعديلات";

                    CboCorrespondents.SelectedValue = _existingCoverage.CorrespondentId;
                    CboGuests.SelectedValue = _existingCoverage.GuestId;
                    TxtTopic.Text = _existingCoverage.Topic;
                    TxtLocation.Text = _existingCoverage.Location;
                    DpDate.SelectedDate = _existingCoverage.ScheduledTime;
                }
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"خطأ في تحميل القوائم: {ex.Message}");
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1. التحقق الأولي من المدخلات (UI Validation)
            if (CboCorrespondents.SelectedValue == null)
            {
                MessageService.Current.ShowWarning("يرجى اختيار المراسل المسؤول.");
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtTopic.Text))
            {
                MessageService.Current.ShowWarning("يرجى إدخال موضوع التغطية.");
                return;
            }

            // 2. تجهيز الـ DTO
            var dto = new CoverageDto
            {
                CoverageId = _existingCoverage?.CoverageId ?? 0,
                CorrespondentId = (int)CboCorrespondents.SelectedValue,
                GuestId = (int?)CboGuests.SelectedValue,
                Topic = TxtTopic.Text.Trim(),
                Location = TxtLocation.Text.Trim(),
                ScheduledTime = DpDate.SelectedDate,
                // يمكن إضافة TimePicker لاحقاً لدمج الوقت بدقة
                ActualTime = _existingCoverage?.ActualTime
            };

            try
            {
                BtnSave.IsEnabled = false;

                if (_existingCoverage == null)
                {
                    // إضافة جديدة
                    await _coverageService.CreateAsync(dto, _session);
                }
                else
                {
                    // تحديث موجود
                    await _coverageService.UpdateAsync(dto, _session);
                }

                // إغلاق النافذة بنجاح
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                // اصطياد أخطاء التزامن أو الصلاحيات أو السيرفر
                MessageService.Current.ShowError(ex.Message);
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }

    }
}
