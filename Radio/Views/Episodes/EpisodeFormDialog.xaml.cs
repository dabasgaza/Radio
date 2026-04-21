using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using DataAccess.Validation;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Versioning;
using System.Windows;

namespace Radio.Views.Episodes
{
    /// <summary>
    /// نافذة إضافة/تعديل حلقة — تتولى جمع البيانات والتحقق منها ثم إرسالها لـ EpisodeService.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class EpisodeFormDialog
    {
        private readonly ActiveEpisodeDto? _existingEpisode;
        private readonly IEpisodeService _episodeService;
        private readonly IProgramService _programService;
        private readonly IGuestService _guestService;
        private readonly UserSession _session;

        public EpisodeFormDialog(
            IEpisodeService episodeService,
            IProgramService programService,
            IGuestService guestService,
            UserSession session,
            ActiveEpisodeDto? existingEpisode = null)
        {
            InitializeComponent();
            _episodeService = episodeService;
            _programService = programService;
            _guestService = guestService;
            _existingEpisode = existingEpisode;
            _session = session;

            IsWindowDraggable = true;

            // ✅ عنوان ديناميكي حسب نوع العملية
            Title = _existingEpisode is not null ? "تعديل بيانات الحلقة" : "إضافة حلقة جديدة";

            // القيم الافتراضية
            DpDate.SelectedDate = DateTime.Today;
            TpTime.SelectedTime = DateTime.Now;

            // ✅ Loaded بدلاً من Fire-and-Forget
            Loaded += async (_, _) => await LoadInitialDataAsync();
        }

        #region Data Loading

        /// <summary>
        /// تحميل قوائم البرامج والضيوف وتعبئة الحقول في حالة التعديل.
        /// </summary>
        private async Task LoadInitialDataAsync()
        {
            try
            {
                var programsTask = _programService.GetAllActiveAsync();
                var guestsTask = _guestService.GetAllActiveAsync();

                await Task.WhenAll(programsTask, guestsTask);

                CboPrograms.ItemsSource = programsTask.Result;
                CboGuests.ItemsSource = guestsTask.Result;

                if (_existingEpisode is not null)
                {
                    CboPrograms.SelectedValue = _existingEpisode.ProgramId;
                    CboGuests.SelectedValue = _existingEpisode.GuestId;
                    TxtEpisodeName.Text = _existingEpisode.EpisodeName;
                    TxtNotes.Text = _existingEpisode.SpecialNotes;

                    if (_existingEpisode.ScheduledExecutionTime.HasValue)
                    {
                        DpDate.SelectedDate = _existingEpisode.ScheduledExecutionTime.Value.Date;
                        TpTime.SelectedTime = _existingEpisode.ScheduledExecutionTime.Value;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError("ليس لديك صلاحية لعرض بيانات البرامج أو الضيوف.");
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

        #endregion

        #region Save

        /// <summary>
        /// حفظ الحلقة (إضافة أو تعديل) بعد التحقق من صحة المدخلات.
        /// </summary>
        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // دمج التاريخ والوقت
            DateTime? scheduledTime = null;
            if (DpDate.SelectedDate.HasValue && TpTime.SelectedTime.HasValue)
            {
                var date = DpDate.SelectedDate.Value;
                var time = TpTime.SelectedTime.Value;
                scheduledTime = new DateTime(date.Year, date.Month, date.Day,
                    time.Hour, time.Minute, 0);
            }

            var dto = new EpisodeDto(
                _existingEpisode?.EpisodeId ?? 0,
                (int)CboPrograms.SelectedValue!,
                (int?)CboGuests.SelectedValue,
                TxtEpisodeName.Text.Trim(),
                scheduledTime,
                TxtNotes.Text.Trim());

            try
            {
                // ✅ تحقق عبر Pipeline
                ValidationPipeline.ValidateEpisode(dto);

                BtnSave.IsEnabled = false;

                if (_existingEpisode is null)
                    await _episodeService.CreateEpisodeAsync(dto, _session);
                else
                    await _episodeService.UpdateEpisodeAsync(dto, _session);

                MessageService.Current.ShowSuccess(
                    _existingEpisode is null
                        ? "تمت إضافة الحلقة بنجاح."
                        : "تم تعديل بيانات الحلقة بنجاح.");

                DialogResult = true;
            }
            catch (ValidationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError(
                    _existingEpisode is null
                        ? "ليس لديك صلاحية لإضافة حلقة جديدة."
                        : "ليس لديك صلاحية لتعديل بيانات الحلقات.");
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حفظ بيانات الحلقة.");
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }

        #endregion

        #region UI Events

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                DragMove();
        }

        #endregion
    }
}