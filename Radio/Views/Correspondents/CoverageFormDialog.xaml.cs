using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using DataAccess.Validation;
using System.ComponentModel.DataAnnotations;
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

            // ✅ عنوان ديناميكي حسب نوع العملية
            Title = _existingCoverage is null
                ? "إضافة تغطية ميدانية جديدة"
                : "تعديل بيانات التغطية الميدانية";

            // ✅ نستخدم Loaded بدلاً من Fire-and-Forget
            Loaded += async (_, _) => await LoadInitialDataAsync();
        }

        /// <summary>
        /// تحميل قوائم المراسلين والضيوف لملء الـ ComboBoxes.
        /// </summary>
        private async Task LoadInitialDataAsync()
        {
            try
            {
                // ✅ تحميل البيانات بالتوازي لتحسين الأداء
                var correspondentsTask = _correspondentService.GetAllActiveAsync();
                var guestsTask = _guestService.GetAllActiveAsync();

                await Task.WhenAll(correspondentsTask, guestsTask);

                CboCorrespondents.ItemsSource = correspondentsTask.Result;
                CboGuests.ItemsSource = guestsTask.Result;

                // تعبئة الحقول في حالة التعديل
                if (_existingCoverage is not null)
                {
                    CboCorrespondents.SelectedValue = _existingCoverage.CorrespondentId;
                    CboGuests.SelectedValue = _existingCoverage.GuestId;
                    TxtTopic.Text = _existingCoverage.Topic;
                    TxtLocation.Text = _existingCoverage.Location;
                    DpDate.SelectedDate = _existingCoverage.ScheduledTime;
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError("ليس لديك صلاحية لعرض بيانات المراسلين أو الضيوف.");
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
            var dto = new CoverageDto
            {
                CoverageId = _existingCoverage?.CoverageId ?? 0,
                CorrespondentId = (int)CboCorrespondents.SelectedValue!,
                GuestId = (int?)CboGuests.SelectedValue,
                Topic = TxtTopic.Text.Trim(),
                Location = TxtLocation.Text.Trim(),
                ScheduledTime = DpDate.SelectedDate,
                ActualTime = _existingCoverage?.ActualTime
            };

            try
            {
                // ✅ التحقق عبر ValidationPipeline
                ValidationPipeline.ValidateCoverage(dto);

                BtnSave.IsEnabled = false;

                if (_existingCoverage is null)
                    await _coverageService.CreateAsync(dto, _session);
                else
                    await _coverageService.UpdateAsync(dto, _session);

                MessageService.Current.ShowSuccess(
                    _existingCoverage is null
                        ? "تمت إضافة التغطية الميدانية بنجاح."
                        : "تم تعديل بيانات التغطية الميدانية بنجاح.");

                DialogResult = true;
            }
            catch (ValidationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError(
                    _existingCoverage is null
                        ? "ليس لديك صلاحية لإضافة تغطية ميدانية."
                        : "ليس لديك صلاحية لتعديل بيانات التغطيات.");
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
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

        /// <summary>
        /// سحب النافذة عبر شريط العنوان.
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