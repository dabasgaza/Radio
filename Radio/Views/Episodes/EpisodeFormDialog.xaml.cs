using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using DataAccess.Validation;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Radio.Views.Episodes
{
    /// <summary>
    /// نموذج عرض لصف واحد في DataGrid الضيوف
    /// </summary>
    public class GuestRow : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public int GuestId { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public string? Topic { get; set; }
        public TimeSpan? HostingTime { get; set; }

        public string HostingTimeDisplay =>
            HostingTime.HasValue
                ? HostingTime.Value.ToString(@"hh\:mm")
                : string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>
    /// نافذة إضافة/تعديل حلقة — Code-Behind نقي
    /// </summary>
    public partial class EpisodeFormDialog
    {
        private readonly ActiveEpisodeDto? _existingEpisode;
        private readonly IEpisodeService _episodeService;
        private readonly IProgramService _programService;
        private readonly IGuestService _guestService;
        private readonly UserSession _session;

        /// <summary>
        /// قائمة الضيوف المُضافين مؤقتاً — تُعرض في DataGrid
        /// </summary>
        public ObservableCollection<GuestRow> GuestList { get; } = new();

        /// <summary>
        /// معرّف الحلقة التي تم إنشاؤها للتو (في حالة الإضافة فقط)
        /// </summary>
        public int CreatedEpisodeId { get; private set; }

        /// <summary>
        /// بيانات النموذج التي تم إرسالها للخدمة
        /// </summary>
        public EpisodeDto? SubmittedDto { get; private set; }

        /// <summary>
        /// اسم البرنامج المُختار في ComboBox
        /// </summary>
        public string SubmittedProgramName { get; private set; } = string.Empty;

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
                CboGuest.ItemsSource = guests.ToList();

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

                    await LoadExistingGuests();
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

        private async Task LoadExistingGuests()
        {
            var guests = await _episodeService.GetEpisodeGuestsAsync(_existingEpisode!.EpisodeId);

            var allGuests = CboGuest.ItemsSource as System.Collections.IEnumerable;
            if (allGuests == null) return;

            foreach (var g in guests)
            {
                var guestName = "غير معروف";
                foreach (var item in allGuests)
                {
                    if ((int)((dynamic)item).GuestId == g.GuestId)
                    {
                        guestName = ((dynamic)item).FullName;
                        break;
                    }
                }

                GuestList.Add(new GuestRow
                {
                    GuestId = g.GuestId,
                    GuestName = guestName,
                    Topic = g.Topic,
                    HostingTime = g.HostingTime
                });
            }
        }

        #endregion

        #region Guest Management

        /// <summary>
        /// الصف الذي يتم تعديله حالياً — null يعني وضع الإضافة
        /// </summary>
        private GuestRow? _editingRow;

        /// <summary>
        /// إضافة ضيف جديد أو تحديث الضيف المحدد
        /// </summary>
        private void BtnAddGuest_Click(object sender, RoutedEventArgs e)
        {
            if (CboGuest.SelectedValue is not int guestId || guestId <= 0)
            {
                MessageService.Current.ShowWarning("يرجى اختيار ضيف أولاً.");
                return;
            }

            var guestName = ((dynamic)CboGuest.SelectedItem).FullName;
            var topic = TxtTopic.Text.Trim();
            var hostingTime = TpHostingTime.SelectedTime?.TimeOfDay;

            if (_editingRow is not null)
            {
                // ✏️ وضع التعديل — تحديث الصف الموجود
                _editingRow.GuestId = guestId;
                _editingRow.GuestName = guestName;
                _editingRow.Topic = topic;
                _editingRow.HostingTime = hostingTime;

                ExitEditMode();
            }
            else
            {
                // ➕ وضع الإضافة — التحقق من التكرار
                if (GuestList.Any(g => g.GuestId == guestId))
                {
                    MessageService.Current.ShowWarning("هذا الضيف مُضاف مسبقاً.");
                    return;
                }

                GuestList.Add(new GuestRow
                {
                    GuestId = guestId,
                    GuestName = guestName,
                    Topic = topic,
                    HostingTime = hostingTime
                });
            }

            // تفريغ صف الإدخال
            CboGuest.SelectedIndex = -1;
            TxtTopic.Text = string.Empty;
            TpHostingTime.SelectedTime = null;
        }

        /// <summary>
        /// النقر المزدوج على صف → نقل البيانات لصف الإدخال لتعديلها
        /// </summary>
        private void DgGuests_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // تجاهل النقر على رأس الأعمدة
            if (e.OriginalSource is not FrameworkElement element) return;
            if (element.DataContext is not GuestRow row) return;

            // تعبئة صف الإدخال ببيانات الصف المحدد
            CboGuest.SelectedValue = row.GuestId;
            TxtTopic.Text = row.Topic ?? string.Empty;

            if (row.HostingTime.HasValue)
            {
                var today = DateTime.Today;
                TpHostingTime.SelectedTime = today.Add(row.HostingTime.Value);
            }
            else
            {
                TpHostingTime.SelectedTime = null;
            }

            // الدخول في وضع التعديل
            _editingRow = row;

            // تغيير شكل الزر
            IcoAddGuest.Kind = PackIconKind.Check;
            BtnAddGuest.ToolTip = "حفظ التعديل";
        }

        /// <summary>
        /// الخروج من وضع التعديل وإعادة الزر لحالته الأصلية
        /// </summary>
        private void ExitEditMode()
        {
            _editingRow = null;
            IcoAddGuest.Kind = PackIconKind.Plus;
            BtnAddGuest.ToolTip = "إضافة الضيف للقائمة";
        }

        private void BtnRemoveGuest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element
                && element.DataContext is GuestRow row)
            {
                // إذا كان الصف المحذوف هو المعروف حالياً — الخروج من وضع التعديل
                if (_editingRow == row)
                    ExitEditMode();

                GuestList.Remove(row);
            }
        }

        /// <summary>
        /// جمع بيانات الضيوف من DataGrid للإرسال لقاعدة البيانات
        /// </summary>
        private List<EpisodeGuestDto> CollectGuests()
        {
            return GuestList
                .Select(g => new EpisodeGuestDto(
                    g.Id,
                    g.GuestId,
                    g.Topic,
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
                CollectGuests(),
                TxtEpisodeName.Text.Trim(),
                scheduledTime,
                TxtNotes.Text.Trim());

            try
            {
                var validation = ValidationPipeline.ValidateEpisode(dto);
                if (!validation.IsSuccess)
                {
                    MessageService.Current.ShowWarning(validation.ErrorMessage ?? "أخطاء في التحقق.");
                    return;
                }

                BtnSave.IsEnabled = false;

                // الحصول على اسم البرنامج المختار
                SubmittedProgramName = ((dynamic)CboPrograms.SelectedItem).ProgramName;
                SubmittedDto = dto;

                if (_existingEpisode is null)
                {
                    var result = await _episodeService.CreateEpisodeAsync(dto, _session);
                    if (result.IsSuccess)
                    {
                        CreatedEpisodeId = result.Value;
                        MessageService.Current.ShowSuccess("تمت إضافة الحلقة بنجاح.");
                        DialogResult = true;
                    }
                    else
                    {
                        MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت عملية الحفظ.");
                    }
                }
                else
                {
                    var result = await _episodeService.UpdateEpisodeAsync(dto, _session);
                    if (result.IsSuccess)
                    {
                        CreatedEpisodeId = _existingEpisode.EpisodeId;
                        MessageService.Current.ShowSuccess("تم تعديل بيانات الحلقة بنجاح.");
                        DialogResult = true;
                    }
                    else
                    {
                        MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت عملية الحفظ.");
                    }
                }
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
