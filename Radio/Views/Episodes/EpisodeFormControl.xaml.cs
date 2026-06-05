using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using MahApps.Metro.Controls;
using Radio.Messaging;

namespace Radio.Views.Episodes
{
    public partial class EpisodeFormControl : MetroWindow
    {
        private readonly IEpisodeService _episodeService;
        private readonly IProgramService _programService;
        private readonly IGuestService _guestService;
        private readonly ICorrespondentService _correspondentService;
        private readonly IEmployeeService _employeeService;
        private readonly UserSession _session;
        private readonly int? _episodeId;
        private int _selectedProgramId;
        private DateTime? _selectedDateTime;
        private List<ConflictInfo> _currentConflicts = [];
        private readonly EpisodeDraftService _draftService = new();
        private DispatcherTimer? _autoSaveTimer;

        // ── قوائم Observable مرتبطة بالجداول ──
        public ObservableCollection<GuestRow> GuestList { get; } = [];
        public ObservableCollection<CorrespondentRow> CorrespondentList { get; } = [];
        public ObservableCollection<EmployeeRow> EmployeeList { get; } = [];

        public EpisodeFormControl(
            IEpisodeService episodeService,
            IProgramService programService,
            IGuestService guestService,
            ICorrespondentService correspondentService,
            IEmployeeService employeeService,
            UserSession session,
            int? episodeId = null)
        {
            InitializeComponent();
            Loaded += (_, _) => NotificationManager.RegisterHost(NotificationHost);
            Unloaded += (_, _) => NotificationManager.RegisterHost(null!);
            _episodeService = episodeService;
            _programService = programService;
            _guestService = guestService;
            _correspondentService = correspondentService;
            _employeeService = employeeService;
            _session = session;
            _episodeId = episodeId;

            // ── تسجيل اختصارات لوحة المفاتيح ──
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, (_, _) => BtnSave_Click(null!, null!)));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (_, _) => BtnCancel_Click(null!, null!)));

            // ── تفعيل سحب النافذة وتحديد العنوان حسب نوع العملية ──
            IsWindowDraggable = true;

            Title = _episodeId.HasValue ? "تعديل بيانات الحلقة" : "جدولة حلقة إذاعية";
            TxtTitle.Text = _episodeId.HasValue ? "تعديل بيانات الحلقة" : "جدولة حلقة إذاعية";

            DgGuests.ItemsSource = GuestList;
            DgCorrespondents.ItemsSource = CorrespondentList;
            DgEmployees.ItemsSource = EmployeeList;

            // ── تعطيل ذكي لزر الحفظ ──
            TxtEpisodeName.TextChanged += (_, _) => UpdateSaveButtonState();
            DpDate.SelectedDateChanged += (_, _) => UpdateSaveButtonState();
            CbPrograms.SelectionChanged += (_, _) => UpdateSaveButtonState();

            Loaded += async (_, _) => await InitializeDataAsync();
        }

        private void UpdateSaveButtonState()
        {
            bool hasProgram = CbPrograms.SelectedValue != null;
            bool hasName = !string.IsNullOrWhiteSpace(TxtEpisodeName.Text);
            bool hasDate = DpDate.SelectedDate.HasValue;
            BtnSave.IsEnabled = hasProgram && hasName && hasDate;
        }

        private async Task InitializeDataAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                CbPrograms.ItemsSource = await _programService.GetAllActiveAsync();
                CbAllGuests.ItemsSource = await _guestService.GetAllActiveAsync();
                CbAllCorrespondents.ItemsSource = await _correspondentService.GetAllActiveAsync();
                CbAllEmployees.ItemsSource = await _employeeService.GetAllActiveAsync();

                if (_episodeId.HasValue)
                {
                    var ep = await _episodeService.GetActiveEpisodeByIdAsync(_episodeId.Value);
                    if (ep == null) return;

                    TxtEpisodeName.Text = ep.EpisodeName;
                    DpDate.SelectedDate = ep.ScheduledExecutionTime?.Date;
                    if (ep.ScheduledExecutionTime.HasValue)
                        TpBroadcastTime.SelectedTime = DateTime.Today.Add(ep.ScheduledExecutionTime.Value.TimeOfDay);
                    TxtSpecialNotes.Text = ep.SpecialNotes;
                    CbPrograms.SelectedValue = ep.ProgramId;

                    // تحميل الضيوف الكاملين من الخدمة (يشمل الاسم والموضوع والساعة)
                    var guests = await _episodeService.GetEpisodeGuestsAsync(_episodeId.Value);
                    foreach (var g in guests)
                        GuestList.Add(new GuestRow(g.EpisodeGuestId, g.GuestId, g.FullName, g.Topic, g.HostingTime));

                    // تحميل المراسلين
                    foreach (var c in ep.CorrespondentItems)
                        CorrespondentList.Add(new CorrespondentRow(c.Id, c.CorrespondentId, c.FullName, c.Topic, c.HostingTime));

                    // تحميل الموظفين
                    var allEmployees = (await _employeeService.GetAllActiveAsync()).ToList();
                    foreach (var e in ep.EmployeeItems)
                    {
                        var emp = allEmployees.FirstOrDefault(em => em.EmployeeId == e.EmployeeId);
                        EmployeeList.Add(new EmployeeRow(e.Id, e.EmployeeId, emp?.FullName ?? "غير معروف", emp?.StaffRoleName ?? "—"));
                    }
                    UpdateSectionCounts();
                    UpdateSaveButtonState();
                }
                else
                {
                    // ── للحلقات الجديدة: استعادة مسودة واستart الـ Auto-save ──
                    await TryRestoreDraftAsync();
                    StartAutoSave();
                    UpdateSectionCounts();
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An unexpected error occurred during processing");
                MessageService.Current.ShowError($"خطأ في تحميل البيانات: {ex.Message}");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        // ── إدارة الضيوف ──────────────────────────────────────────

        private void BtnAddGuest_Click(object sender, RoutedEventArgs e)
        {
            if (CbAllGuests.SelectedItem is not GuestDto guest)
            {
                MessageService.Current.ShowWarning("يرجى اختيار ضيف من القائمة أولاً."); return;
            }
            if (GuestList.Any(x => x.GuestId == guest.GuestId))
            {
                MessageService.Current.ShowWarning("هذا الضيف مضاف بالفعل."); return;
            }

            var topic = TxtGuestTopic.Text?.Trim();
            var hostingTime = TpGuestHostingTime.SelectedTime.HasValue
                ? TpGuestHostingTime.SelectedTime.Value.TimeOfDay
                : (TimeSpan?)null;

            GuestList.Add(new GuestRow(0, guest.GuestId, guest.FullName, topic, hostingTime));
            UpdateSectionCounts();

            // تصفير حقول الإضافة وتركيز تلقائي
            CbAllGuests.SelectedItem = null;
            TxtGuestTopic.Clear();
            TpGuestHostingTime.SelectedTime = null;
            Dispatcher.BeginInvoke(new Action(() => Keyboard.Focus(CbAllGuests)));
        }

        private void BtnRemoveGuest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is GuestRow row)
                GuestList.Remove(row);
            UpdateSectionCounts();
        }

        // ── إدارة المراسلين ────────────────────────────────────────

        private void BtnAddCorrespondent_Click(object sender, RoutedEventArgs e)
        {
            if (CbAllCorrespondents.SelectedItem is not CorrespondentDto corr)
            {
                MessageService.Current.ShowWarning("يرجى اختيار مراسل من القائمة أولاً."); return;
            }
            if (CorrespondentList.Any(x => x.CorrespondentId == corr.CorrespondentId))
            {
                MessageService.Current.ShowWarning("هذا المراسل مضاف بالفعل."); return;
            }

            var topic = TxtCorrespondentTopic.Text?.Trim();
            var hostingTime = TpCorrespondentHostingTime.SelectedTime.HasValue
                ? TpCorrespondentHostingTime.SelectedTime.Value.TimeOfDay
                : (TimeSpan?)null;

            CorrespondentList.Add(new CorrespondentRow(0, corr.CorrespondentId, corr.FullName, topic, hostingTime));
            UpdateSectionCounts();

            CbAllCorrespondents.SelectedItem = null;
            TxtCorrespondentTopic.Clear();
            TpCorrespondentHostingTime.SelectedTime = null;
            Dispatcher.BeginInvoke(new Action(() => Keyboard.Focus(CbAllCorrespondents)));
        }

        private void BtnRemoveCorrespondent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CorrespondentRow row)
                CorrespondentList.Remove(row);
            UpdateSectionCounts();
        }

        // ── إدارة طاقم العمل ──────────────────────────────────────

        private void BtnAddEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (CbAllEmployees.SelectedItem is not EmployeeDto emp)
            {
                MessageService.Current.ShowWarning("يرجى اختيار موظف من القائمة أولاً."); return;
            }
            if (EmployeeList.Any(x => x.EmployeeId == emp.EmployeeId))
            {
                MessageService.Current.ShowWarning("هذا الموظف مضاف بالفعل."); return;
            }

            EmployeeList.Add(new EmployeeRow(0, emp.EmployeeId, emp.FullName, emp.StaffRoleName ?? "—"));
            UpdateSectionCounts();
            CbAllEmployees.SelectedItem = null;
            Dispatcher.BeginInvoke(new Action(() => Keyboard.Focus(CbAllEmployees)));
        }

        private void BtnRemoveEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is EmployeeRow row)
                EmployeeList.Remove(row);
            UpdateSectionCounts();
        }

        // ── الحفظ والإلغاء ────────────────────────────────────────

        // ═══════════════════════════════════════════════════════
        // تحديث عدادات الأقسام وحالات الفراغ
        // ═══════════════════════════════════════════════════════
        private void UpdateSectionCounts()
        {
            GuestCountText.Text = GuestList.Count.ToString();
            CorrespondentCountText.Text = CorrespondentList.Count.ToString();
            StaffCountText.Text = EmployeeList.Count.ToString();

            TabGuestCountText.Text = GuestList.Count.ToString();
            TabGuestCountBadge.Visibility = GuestList.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            TabCorrespondentCountText.Text = CorrespondentList.Count.ToString();
            TabCorrespondentCountBadge.Visibility = CorrespondentList.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            TabStaffCountText.Text = EmployeeList.Count.ToString();
            TabStaffCountBadge.Visibility = EmployeeList.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            EmptyGuestState.Visibility = GuestList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            EmptyCorrespondentState.Visibility = CorrespondentList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            EmptyStaffState.Visibility = EmployeeList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ═══════════════════════════════════════════════════════
        // الحفظ التلقائي والمسودات
        // ═══════════════════════════════════════════════════════
        private void StartAutoSave()
        {
            if (_episodeId.HasValue) return; // لا نحفظ مسودات للتعديل
            _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _autoSaveTimer.Tick += async (_, _) => await SaveDraftAsync();
            _autoSaveTimer.Start();
        }

        private async Task SaveDraftAsync()
        {
            try
            {
                var draft = CollectDraftData();
                await _draftService.SaveDraftAsync(draft);
                PnlAutoSave.Visibility = Visibility.Visible;
                TxtAutoSaveStatus.Text = $"تم الحفظ تلقائياً — {DateTime.Now:hh:mm tt}";
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An unexpected error occurred during processing");
                MessageService.Current.ShowError($"خطأ في الحفظ التلقائي: {ex.Message}");
            }
        }

        private EpisodeDraft CollectDraftData()
        {
            var program = CbPrograms.SelectedItem as dynamic;
            return new EpisodeDraft
            {
                ProgramId = CbPrograms.SelectedValue as int?,
                ProgramName = program?.ProgramName,
                EpisodeName = TxtEpisodeName.Text?.Trim(),
                ScheduledDate = DpDate.SelectedDate,
                BroadcastTime = TpBroadcastTime.SelectedTime?.TimeOfDay,
                SpecialNotes = TxtSpecialNotes.Text?.Trim(),
                Guests = GuestList.Select(g => new DraftGuestItem
                {
                    GuestId = g.GuestId,
                    FullName = g.FullName,
                    Topic = g.Topic,
                    HostingTime = g.HostingTime
                }).ToList(),
                Correspondents = CorrespondentList.Select(c => new DraftCorrespondentItem
                {
                    CorrespondentId = c.CorrespondentId,
                    FullName = c.FullName,
                    Topic = c.Topic,
                    HostingTime = c.HostingTime
                }).ToList(),
                Employees = EmployeeList.Select(e => new DraftEmployeeItem
                {
                    EmployeeId = e.EmployeeId,
                    FullName = e.FullName,
                    StaffRoleName = e.StaffRoleName
                }).ToList()
            };
        }

        private async Task TryRestoreDraftAsync()
        {
            if (!_draftService.HasDraft()) return;

            var result = await MessageService.Current.ShowConfirmationAsync(
                "تم العثور على مسودة غير محفوظة لجلسة سابقة.\nهل تريد استعادتها؟",
                "استعادة المسودة");

            if (result)
            {
                var draft = await _draftService.LoadDraftAsync();
                if (draft != null)
                {
                    if (draft.ProgramId.HasValue)
                        CbPrograms.SelectedValue = draft.ProgramId.Value;
                    TxtEpisodeName.Text = draft.EpisodeName ?? "";
                    if (draft.ScheduledDate.HasValue)
                        DpDate.SelectedDate = draft.ScheduledDate.Value;
                    if (draft.BroadcastTime.HasValue)
                        TpBroadcastTime.SelectedTime = DateTime.Today.Add(draft.BroadcastTime.Value);
                    TxtSpecialNotes.Text = draft.SpecialNotes ?? "";

                    foreach (var g in draft.Guests)
                        GuestList.Add(new GuestRow(0, g.GuestId, g.FullName ?? "", g.Topic, g.HostingTime));
                    foreach (var c in draft.Correspondents)
                        CorrespondentList.Add(new CorrespondentRow(0, c.CorrespondentId, c.FullName ?? "", c.Topic, c.HostingTime));
                    foreach (var e in draft.Employees)
                        EmployeeList.Add(new EmployeeRow(0, e.EmployeeId, e.FullName ?? "", e.StaffRoleName ?? ""));

                    UpdateSectionCounts();
                    UpdateSaveButtonState();
                    PnlAutoSave.Visibility = Visibility.Visible;
                    TxtAutoSaveStatus.Text = $"تم استعادة مسودة من {draft.SavedAt:hh:mm tt}";
                }
            }
            else
            {
                _draftService.DeleteDraft();
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // ── التحقق من المدخلات الإلزامية ──
            if (CbPrograms.SelectedValue == null) { MessageService.Current.ShowWarning("يرجى اختيار البرنامج."); return; }
            if (string.IsNullOrWhiteSpace(TxtEpisodeName.Text)) { MessageService.Current.ShowWarning("يرجى إدخال عنوان الحلقة."); return; }
            if (DpDate.SelectedDate == null) { MessageService.Current.ShowWarning("يرجى تحديد تاريخ البث."); return; }

            // ── تعطيل زر الحفظ أثناء العملية لمنع النقر المكرر ──
            BtnSave.IsEnabled = false;

            try
            {
                var dto = new EpisodeDto(
                    _episodeId ?? 0,
                    (int)CbPrograms.SelectedValue,
                    GuestList.Select(g => new EpisodeGuestDto(g.EpisodeGuestId, g.GuestId, g.FullName, g.Topic, g.HostingTime, null)).ToList(),
                    CorrespondentList.Select(c => new EpisodeCorrespondentDto(c.Id, c.CorrespondentId, c.FullName, c.Topic, c.HostingTime)).ToList(),
                    EmployeeList.Select(emp => new EpisodeEmployeeDto(emp.Id, emp.EmployeeId)).ToList(),
                    TxtEpisodeName.Text.Trim(),
                    DpDate.SelectedDate,
                    TpBroadcastTime.SelectedTime?.TimeOfDay,
                    TxtSpecialNotes.Text?.Trim()
                );

                // ── تنفيذ عملية الحفظ (إضافة أو تعديل) ──
                Result result;
                if (_episodeId.HasValue)
                    result = await _episodeService.UpdateEpisodeAsync(dto, _session);
                else
                {
                    var createRes = await _episodeService.CreateEpisodeAsync(dto, _session);
                    result = createRes.IsSuccess ? Result.Success() : Result.Fail(createRes.ErrorMessage ?? "خطأ غير معروف");
                }

                // ── عرض رسالة النتيجة للمستخدم ──
                if (result.IsSuccess)
                {
                    _draftService.DeleteDraft();
                    DialogResult = true;
                }
                else
                {
                    MessageService.Current.ShowError(result.ErrorMessage ?? "فشل حفظ بيانات الحلقة.");
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An unexpected error occurred during processing");
                // ── معالجة أي استثناء غير متوقع ──
                MessageService.Current.ShowError($"حدث خطأ غير متوقع أثناء الحفظ: {ex.Message}");
            }
            finally
            {
                // ── إعادة تفعيل زر الحفظ ──
                BtnSave.IsEnabled = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _autoSaveTimer?.Stop();
            DialogResult = false;
        }

        // ═══════════════════════════════════════════════════════
        // كشف تعارض المواعيد
        // ═══════════════════════════════════════════════════════
        private void CbPrograms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedProgramId = CbPrograms.SelectedValue as int? ?? 0;
            _ = CheckForConflictsAsync();
        }

        private void DpDate_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedDateTime();
            _ = CheckForConflictsAsync();
        }

        private void TpBroadcastTime_SelectedTimeChanged(object? sender, RoutedEventArgs e)
        {
            UpdateSelectedDateTime();
            _ = CheckForConflictsAsync();
        }

        private void UpdateSelectedDateTime()
        {
            if (DpDate.SelectedDate.HasValue)
            {
                var date = DpDate.SelectedDate.Value;
                var time = TpBroadcastTime.SelectedTime.HasValue
                    ? TpBroadcastTime.SelectedTime.Value.TimeOfDay
                    : TimeSpan.Zero;
                _selectedDateTime = date.Date.Add(time);
            }
            else
                _selectedDateTime = null;
        }

        private async Task CheckForConflictsAsync()
        {
            if (_selectedProgramId <= 0 || !_selectedDateTime.HasValue)
            {
                ConflictPanel.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                _currentConflicts = await _episodeService.GetConflictingEpisodesAsync(
                    _selectedProgramId, _selectedDateTime.Value, _episodeId);

                if (_currentConflicts.Count == 0)
                {
                    ConflictPanel.Visibility = Visibility.Collapsed;
                    return;
                }

                ConflictPanel.Visibility = Visibility.Visible;
                bool hasHigh = _currentConflicts.Any(c => c.Level == ConflictLevel.High);

                if (hasHigh)
                {
                    ConflictPanel.SetCurrentValue(Border.BackgroundProperty, FindResource("ErrorLightBrush") ?? System.Windows.Media.Brushes.LightPink);
                    ConflictTitle.Text = "تعارض عالي — نفس البرنامج والوقت";
                    ConflictTitle.Foreground = (System.Windows.Media.Brush)FindResource("ErrorBrush");
                    ConflictMessage.Text = $"توجد {_currentConflicts.Count(c => c.Level == ConflictLevel.High)} حلقة في نفس البرنامج والوقت.";
                }
                else
                {
                    ConflictPanel.SetCurrentValue(Border.BackgroundProperty, FindResource("WarningLightBrush") ?? System.Windows.Media.Brushes.LightYellow);
                    ConflictTitle.Text = "تعارض محتمل";
                    ConflictTitle.Foreground = (System.Windows.Media.Brush)FindResource("WarningBrush");
                    ConflictMessage.Text = $"توجد {_currentConflicts.Count} حلقة في نفس الفترة الزمنية.";
                }

                BtnConflictDetails.Visibility = _currentConflicts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch
            {
                ConflictPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnConflictDetails_Click(object sender, RoutedEventArgs e)
        {
            if (_currentConflicts.Count == 0) return;

            var details = string.Join("\n", _currentConflicts.Select(c =>
                $"• {c.EpisodeName} ({c.ProgramName}) — {c.ScheduledTime:yyyy/MM/dd hh:mm tt} — {(c.Level == ConflictLevel.High ? "تعارض عالي" : "تعارض متوسط")}"));

            MessageService.Current.ShowInfo(
                $"التعارضات المكتشفة ({_currentConflicts.Count}):\n\n{details}",
                "تفاصيل تعارض المواعيد");
        }

        // ── نماذج البيانات الداخلية (ViewModels) ──────────────────

        public class GuestRow(int episodeGuestId, int guestId, string fullName, string? topic, TimeSpan? hostingTime)
        {
            public int EpisodeGuestId { get; set; } = episodeGuestId;
            public int GuestId { get; set; } = guestId;
            public string FullName { get; set; } = fullName;
            public string? Topic { get; set; } = topic;
            public TimeSpan? HostingTime { get; set; } = hostingTime;
            public string HostingTimeDisplay => HostingTime.HasValue ? HostingTime.Value.ToString(@"hh\:mm") : "—";
        }

        public class CorrespondentRow(int id, int correspondentId, string fullName, string? topic, TimeSpan? hostingTime)
        {
            public int Id { get; set; } = id;
            public int CorrespondentId { get; set; } = correspondentId;
            public string FullName { get; set; } = fullName;
            public string? Topic { get; set; } = topic;
            public TimeSpan? HostingTime { get; set; } = hostingTime;
            public string HostingTimeDisplay => HostingTime.HasValue ? HostingTime.Value.ToString(@"hh\:mm") : "—";
        }

        public class EmployeeRow(int id, int employeeId, string fullName, string staffRoleName)
        {
            public int Id { get; set; } = id;
            public int EmployeeId { get; set; } = employeeId;
            public string FullName { get; set; } = fullName;
            public string StaffRoleName { get; set; } = staffRoleName;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();    
        }
    }
}
