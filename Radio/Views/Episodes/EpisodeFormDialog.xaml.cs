using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Runtime.Versioning;
using System.Windows;

namespace Radio.Views.Episodes
{
    /// <summary>
    /// Interaction logic for EpisodeFormDialog.xaml
    /// </summary>

    [SupportedOSPlatform("windows")] // 👈 أضف هذا السطر فوق الكلاس أو الميثود
    public partial class EpisodeFormDialog
    {
        private readonly ActiveEpisodeDto? _existingEpisode;
        private readonly IEpisodeService _episodeService;
        private readonly IProgramService _programService;
        private readonly IGuestService _guestService; // 👈 إضافة خدمة الضيوف

        private readonly UserSession _session;

        public EpisodeFormDialog(IEpisodeService episodeService,
            IProgramService programService,
            IGuestService guestService, UserSession session, ActiveEpisodeDto? existingEpisode)
        {
            InitializeComponent();
            _episodeService = episodeService;
            _programService = programService;
            _guestService = guestService;
            _existingEpisode = existingEpisode;
            _session = session;

            this.IsWindowDraggable = true;

            // إعداد القيم الافتراضية
            DpDate.SelectedDate = DateTime.Today;
            TpTime.SelectedTime = DateTime.Now;

            // تحميل قائمة البرامج عند فتح النافذة
            _ = LoadProgramsAsync();
        }

        private async Task LoadProgramsAsync()
        {
            try
            {
                // تحميل البرامج والضيوف بالتوازي لسرعة الأداء
                var programsTask = _programService.GetAllActiveAsync();
                var guestsTask = _guestService.GetAllActiveAsync();

                await Task.WhenAll(programsTask, guestsTask);

                CboPrograms.ItemsSource = await programsTask;
                CboGuests.ItemsSource = await guestsTask; // 👈 تعبئة قائمة الضيوف

                // في حالة التعديل، نحدد القيم المختارة مسبقاً
                if (_existingEpisode != null)
                {
                    TxtTitle.Text = "تعديل بيانات الحلقة";
                    BtnSave.Content = "حفظ التعديلات";

                    // تحديد البرنامج والضيف المختارين مسبقاً
                    CboPrograms.SelectedValue = _existingEpisode.ProgramId;
                    CboGuests.SelectedValue = _existingEpisode.GuestId;

                    // ملء النصوص والتواريخ
                    TxtEpisodeName.Text = _existingEpisode.EpisodeName;
                    TxtNotes.Text = _existingEpisode.SpecialNotes;

                    if (_existingEpisode.ScheduledExecutionTime.HasValue)
                    {
                        DpDate.SelectedDate = _existingEpisode.ScheduledExecutionTime.Value.Date;
                        TpTime.SelectedTime = _existingEpisode.ScheduledExecutionTime.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageService.Current
                    .ShowError($"خطأ في تحميل البيانات: {ex.InnerException?.Message ?? ex.Message}");
            }

        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1. التحقق من صحة المدخلات (UI Validation)
            if (CboPrograms.SelectedValue == null)
            {
                MessageService.Current.ShowWarning("يرجى اختيار البرنامج من القائمة المنسدلة.");
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtEpisodeName.Text))
            {
                MessageService.Current.ShowWarning("عنوان الحلقة مطلوب ولا يمكن تركه فارغاً.");
                return;
            }


            // 2. دمج التاريخ والوقت
            DateTime? scheduledTime = null;
            if (DpDate.SelectedDate.HasValue && TpTime.SelectedTime.HasValue)
            {
                var date = DpDate.SelectedDate.Value;
                var time = TpTime.SelectedTime.Value;
                scheduledTime = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, 0);
            }

            // 3. تجهيز البيانات (DTO)
            var dto = new EpisodeDto(
                0, // ID 0 للإضافة الجديدة
                (int)CboPrograms.SelectedValue,
                (int?)CboGuests.SelectedValue, // 👈 جلب المعرف من الـ ComboBox
                TxtEpisodeName.Text.Trim(),
                scheduledTime,
                TxtNotes.Text.Trim()
            );

            // 4. استدعاء الخدمة لحفظ البيانات
            try
            {
                BtnSave.IsEnabled = false; // منع النقرات المتعددة

                if (_existingEpisode != null)
                {
                    // في حالة التعديل، نستخدم معرف الحلقة الحالية
                    dto = dto with { EpisodeId = _existingEpisode.EpisodeId };
                    await _episodeService.UpdateEpisodeAsync(dto, _session);
                }
                else
                    await _episodeService.CreateEpisodeAsync(dto, _session);

                this.DialogResult = true; // إغلاق النافذة بنجاح
                this.Close();
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageService.Current.ShowWarning("صلاحيات غير كافية: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError("حدث خطأ أثناء حفظ الحلقة: " + ex.Message);
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }

}

