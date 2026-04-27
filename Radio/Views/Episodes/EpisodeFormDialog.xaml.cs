using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using DataAccess.Validation;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;

namespace Radio.Views.Episodes
{
    /// <summary>
    /// نافذة إضافة/تعديل حلقة — تتولى جمع البيانات والتحقق منها ثم إرسالها لـ EpisodeService.
    /// </summary>
    public partial class EpisodeFormDialog
    {
        private readonly ActiveEpisodeDto? _existingEpisode;
        private readonly IEpisodeService _episodeService;
        private readonly IProgramService _programService;
        private readonly IGuestService _guestService;
        private readonly UserSession _session;

        // ✅ قائمة الضيوف المتاحة (للـ ComboBox داخل DataTemplate)
        public List<GuestDto> AllGuests { get; private set; } = [];

        // ✅ عناصر الضيوف المضافة ديناميكياً
        public ObservableCollection<GuestEntryViewModel> GuestEntries { get; } = new();

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

            // ✅ تمكين Data Binding — ضروري للـ RelativeSource في DataTemplate
            DataContext = this;

            IsWindowDraggable = true;
            Title = _existingEpisode is not null ? "تعديل بيانات الحلقة" : "إضافة حلقة جديدة";

            DpDate.SelectedDate = DateTime.Today;
            TpTime.SelectedTime = DateTime.Now;

            Loaded += async (_, _) => await LoadInitialDataAsync();
        }

        #region Data Loading

        private async Task LoadInitialDataAsync()
        {
            try
            {
                var programsTask = _programService.GetAllActiveAsync();
                var guestsTask = _guestService.GetAllActiveAsync();

                var programs = await programsTask;
                var guests = await guestsTask;

                CboPrograms.ItemsSource = programs;
                AllGuests = guests.ToList();

                if (_existingEpisode is not null)
                {
                    CboPrograms.SelectedValue = _existingEpisode.ProgramId;
                    TxtEpisodeName.Text = _existingEpisode.EpisodeName;
                    TxtNotes.Text = _existingEpisode.SpecialNotes;

                    if (_existingEpisode.ScheduledExecutionTime.HasValue)
                    {
                        DpDate.SelectedDate = _existingEpisode.ScheduledExecutionTime.Value.Date;
                        TpTime.SelectedTime = _existingEpisode.ScheduledExecutionTime.Value;
                    }

                    // ✅ تحميل ضيوف الحلقة الحالية — يتطلب تحديث خارجي (انظر الملاحظات)
                    LoadExistingGuests();
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

        /// <summary>
        /// تحميل ضيوف الحلقة في حالة التعديل.
        /// يتطلب إضافة خاصية GuestEntries إلى ActiveEpisodeDto
        /// أو إضافة ميثود GetEpisodeGuestsAsync لـ IEpisodeService.
        /// </summary>
        private void LoadExistingGuests()
        {
            // عند توفر بيانات الضيوف:
            // foreach (var g in _existingEpisode.GuestEntries)
            //     GuestEntries.Add(new GuestEntryViewModel
            //     {
            //         GuestId = g.GuestId,
            //         Topic = g.Topic,
            //         HostingTime = g.HostingTime
            //     });
        }

        #endregion

        #region Guest Management

        private void BtnAddGuest_Click(object sender, RoutedEventArgs e)
        {
            GuestEntries.Add(new GuestEntryViewModel());
        }

        private void BtnRemoveGuest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element
                && element.DataContext is GuestEntryViewModel entry)
                GuestEntries.Remove(entry);
        }

        /// <summary>
        /// جمع بيانات الضيوف من القائمة الديناميكية — يتجاهل العناصر الفارغة.
        /// </summary>
        private List<EpisodeGuestDto> CollectGuests()
        {
            return GuestEntries
                .Where(g => g.GuestId > 0)
                .Select(g => new EpisodeGuestDto(
                    g.GuestId,
                    g.Topic?.Trim(),
                    g.HostingTime))
                .ToList();
        }

        #endregion

        #region Save

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
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
                CollectGuests(),                    // ✅ ضيوف متعددين
                TxtEpisodeName.Text.Trim(),
                scheduledTime,
                TxtNotes.Text.Trim());

            try
            {
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