using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using DataAccess.Validation;
using System.Windows;

namespace Radio.Views.Correspondents
{
    /// <summary>
    /// نافذة إضافة/تعديل تغطية ميدانية — تتولى جمع البيانات والتحقق منها
    /// ثم إرسالها لـ CoverageService.
    /// </summary>
    public partial class CoverageFormDialog
    {
        private readonly ICoverageService _coverageService;
        private readonly ICorrespondentService _correspondentService;
        private readonly IGuestService _guestService;
        private readonly UserSession _session;
        private readonly CoverageDto? _existingCoverage;

        public CoverageFormDialog(
            ICoverageService coverageService,
            ICorrespondentService correspondentService,
            IGuestService guestService,
            UserSession session,
            CoverageDto? existingCoverage = null)
        {
            _coverageService = coverageService;
            _correspondentService = correspondentService;
            _guestService = guestService;
            _session = session;
            _existingCoverage = existingCoverage;

            InitializeComponent();

            Title = _existingCoverage is null
                ? "إضافة تغطية ميدانية جديدة"
                : "تعديل بيانات التغطية الميدانية";

            Loaded += async (_, _) => await LoadInitialDataAsync();
        }

        /// <summary>
        /// تحميل قوائم المراسلين والضيوف لملء الـ ComboBoxes.
        /// </summary>
        private async Task LoadInitialDataAsync()
        {
            try
            {
                // ✅ تحميل متوازي + await مباشر بدلاً من .Result
                var correspondentsTask = _correspondentService.GetAllActiveAsync();
                var guestsTask = _guestService.GetAllActiveAsync();

                var correspondents = await correspondentsTask;
                var guests = await guestsTask;

                CboCorrespondents.ItemsSource = correspondents;
                CboGuests.ItemsSource = guests;

                if (_existingCoverage is not null)
                {
                    CboCorrespondents.SelectedValue = _existingCoverage.CorrespondentId;
                    CboGuests.SelectedValue = _existingCoverage.GuestId;
                    TxtTopic.Text = _existingCoverage.Topic;
                    TxtLocation.Text = _existingCoverage.Location;
                    DpDate.SelectedDate = _existingCoverage.ScheduledTime;
                    DpTime.SelectedTime = _existingCoverage.ScheduledTime;
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تحميل البيانات الأولية.");
            }
        }

        /// <summary>
        /// حفظ التغطية (إضافة أو تعديل) بعد التحقق من صحة المدخلات.
        /// </summary>
        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // ✅ دمج التاريخ والوقت في قيمة واحدة
            DateTime? scheduledTime = null;

            if (DpDate.SelectedDate.HasValue)
            {
                var date = DpDate.SelectedDate.Value.Date;
                var time = DpTime.SelectedTime?.TimeOfDay ?? TimeSpan.Zero;
                scheduledTime = date.Add(time);
            }

            var dto = new CoverageDto
            {
                CoverageId = _existingCoverage?.CoverageId ?? 0,
                CorrespondentId = (int)CboCorrespondents.SelectedValue!,
                GuestId = (int?)CboGuests.SelectedValue,
                Topic = TxtTopic.Text.Trim(),
                Location = TxtLocation.Text.Trim(),
                ScheduledTime = scheduledTime,
                ActualTime = _existingCoverage?.ActualTime
            };

            try
            {
                var validation = ValidationPipeline.ValidateCoverage(dto);
                if (!validation.IsSuccess)
                {
                    MessageService.Current.ShowWarning(validation.ErrorMessage ?? "أخطاء في التحقق.");
                    return;
                }

                BtnSave.IsEnabled = false;

                Result result;
                if (_existingCoverage is null)
                    result = await _coverageService.CreateAsync(dto, _session);
                else
                    result = await _coverageService.UpdateAsync(dto, _session);

                if (result.IsSuccess)
                {
                    MessageService.Current.ShowSuccess(
                        _existingCoverage is null
                            ? "تمت إضافة التغطية الميدانية بنجاح."
                            : "تم تعديل بيانات التغطية الميدانية بنجاح.");

                    DialogResult = true;
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت العملية.");
                }
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حفظ بيانات التغطية.");
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