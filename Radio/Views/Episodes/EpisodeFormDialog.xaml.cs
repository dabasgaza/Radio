using BroadcastWorkflow.Services;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Windows;

namespace Radio.Views.Episodes
{
    /// <summary>
    /// Interaction logic for EpisodeFormDialog.xaml
    /// </summary>
    public partial class EpisodeFormDialog
    {
        private readonly IEpisodeService _episodeService;
        private readonly IProgramService _programService;
        private readonly UserSession _session;

        public EpisodeFormDialog(IEpisodeService episodeService, IProgramService programService, UserSession session)
        {
            InitializeComponent();
            _episodeService = episodeService;
            _programService = programService;
            _session = session;

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
                var programs = await _programService.GetAllActiveAsync();
                CboPrograms.ItemsSource = programs;
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError("حدث خطأ أثناء تحميل قائمة البرامج: " + ex.Message);
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
                TxtEpisodeName.Text.Trim(),
                scheduledTime,
                TxtNotes.Text.Trim()
            );

            // 4. استدعاء الخدمة لحفظ البيانات
            try
            {
                BtnSave.IsEnabled = false; // منع النقرات المتعددة
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
    }

}

