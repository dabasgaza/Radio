// ═══════════════════════════════════════════════════════════════════════════
// EpisodesView.xaml.cs — المرحلة الثانية: أنماط العرض المتقدمة
// ═══════════════════════════════════════════════════════════════════════════
// التحسينات:
//   المرحلة 1: فلاتر Chips + بحث موسّع + عداد نتائج + اختصارات + إخفاء أزرار
//   المرحلة 2: ثلاثة أنماط عرض + ترتيب + تصفية حسب البرنامج
// ═══════════════════════════════════════════════════════════════════════════

using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Radio.Messaging;
using Radio.Views.Common;
using Radio.Views.Publishing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Radio.Views.Episodes
{
    public partial class EpisodesView : UserControl
    {
        private readonly IEpisodeService _episodeService;
        private bool _isUpdatingBatchBar;
        private readonly IProgramService _programService;
        private readonly IGuestService _guestService;
        private readonly ICorrespondentService _correspondentService;
        private readonly IEmployeeService _employeeService;
        private readonly UserSession _session;
        private readonly IServiceProvider _serviceProvider;
        private List<ActiveEpisodeDto> _allEpisodes = [];

        // ─── تفضيلات العرض المحفوظة ───
        private readonly EpisodeViewPreferences _prefs = EpisodeViewPreferences.Load();

        // ─── أصناف مساعدة ───
        public class FilterChipItem
        {
            public string Type { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
        }

        // ─── حالة الفلاتر والعرض ───
        private byte? _activeStatusFilter = null;
        private EpisodeViewMode _currentViewMode = EpisodeViewMode.Cards;
        private string? _activeProgramFilter = null;
        private DateTime? _dateFrom;
        private DateTime? _dateTo;
        private string? _activeDatePreset; // "Today", "ThisWeek", "ThisMonth", "Custom"

        // ─── خيارات الترتيب ───
        private static readonly string[] SortOptions = ["التاريخ (الأحدث)", "التاريخ (الأقدم)", "الحالة", "البرنامج", "اسم الحلقة"];

        public EpisodesView(
            IEpisodeService epService,
            IProgramService progService,
            UserSession session,
            IServiceProvider serviceProvider,
            IGuestService guestService,
            ICorrespondentService correspondentService,
            IEmployeeService employeeService)
        {
            InitializeComponent();

            _episodeService = epService;
            _programService = progService;
            _session = session;
            _serviceProvider = serviceProvider;
            _guestService = guestService;
            _correspondentService = correspondentService;
            _employeeService = employeeService;

            BtnAddEpisode.Visibility = _session.HasPermission(AppPermissions.EpisodeManage)
                ? Visibility.Visible
                : Visibility.Collapsed;

            KeyDown += EpisodesView_KeyDown;

            // ─── تهيئة خيارات الترتيب ───
            CmbSortBy.ItemsSource = SortOptions;

            Loaded += async (_, _) =>
            {
                RestorePreferences();
                await LoadDataAsync();
                ApplySearchFromPrefs();
            };
        }

        // ═══════════════════════════════════════════════════════════
        // اختصارات لوحة المفاتيح
        // ═══════════════════════════════════════════════════════════
        private void EpisodesView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.N)
            {
                e.Handled = true;
                BtnAddEpisode_Click(sender, e);
            }
            else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.F)
            {
                e.Handled = true;
                TxtSearch.Focus();
                TxtSearch.SelectAll();
            }
            else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.K)
            {
                e.Handled = true;
                ShowKeyboardShortcuts();
            }
        }

        private void BtnKeyboardHelp_Click(object sender, RoutedEventArgs e) => ShowKeyboardShortcuts();

        private void ShowKeyboardShortcuts()
        {
            MessageService.Current.ShowInfo(
                "اختصارات لوحة المفاتيح:\n\n" +
                "Ctrl+N — جدولة حلقة جديدة\n" +
                "Ctrl+F — البحث السريع\n" +
                "Ctrl+K — عرض هذا المساعدة\n" +
                "Escape — إغلاق النوافذ المنبثقة");
        }

        // ═══════════════════════════════════════════════════════════
        // تحميل البيانات
        // ═══════════════════════════════════════════════════════════
        private async Task LoadDataAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                EmptyStateOverlay.Visibility = Visibility.Collapsed;
                CardsView.Visibility = Visibility.Collapsed;
                TableView.Visibility = Visibility.Collapsed;
                CompactView.Visibility = Visibility.Collapsed;

                _allEpisodes = (await _episodeService.GetActiveEpisodesAsync()).ToList();
                PopulateProgramFilter();
                RebindAndUpdateStats();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An unexpected error occurred during processing");
                MessageService.Current.ShowError("حدث خطأ أثناء تحميل الحلقات: " + ex.Message);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // تعبئة تصفية البرامج
        // ═══════════════════════════════════════════════════════════
        private void PopulateProgramFilter()
        {
            var programs = _allEpisodes
                .Select(e => e.ProgramName)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            CmbProgramFilter.ItemsSource = programs;
        }

        // ═══════════════════════════════════════════════════════════
        // إعادة الربط وتحديث الإحصائيات
        // ═══════════════════════════════════════════════════════════
        private void RebindAndUpdateStats()
        {
            var keyword = TxtSearch.Text?.Trim();
            var filtered = _allEpisodes.AsEnumerable();

            // ─── فلتر الحالة ───
            if (_activeStatusFilter.HasValue)
            {
                byte sid = _activeStatusFilter.Value;
                filtered = filtered.Where(ep => ep.StatusId == sid);
            }

            // ─── فلتر البرنامج ───
            if (!string.IsNullOrWhiteSpace(_activeProgramFilter))
            {
                var prog = _activeProgramFilter;
                filtered = filtered.Where(ep => ep.ProgramName == prog);
            }

            // ─── فلتر نطاق التاريخ ───
            if (_dateFrom.HasValue)
            {
                var from = _dateFrom.Value.Date;
                filtered = filtered.Where(ep => ep.ScheduledExecutionTime.HasValue && ep.ScheduledExecutionTime.Value.Date >= from);
            }
            if (_dateTo.HasValue)
            {
                var to = _dateTo.Value.Date;
                filtered = filtered.Where(ep => ep.ScheduledExecutionTime.HasValue && ep.ScheduledExecutionTime.Value.Date <= to);
            }

            // ─── بحث موسّع ───
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filtered = filtered.Where(ep =>
                    (ep.EpisodeName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (ep.ProgramName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (ep.GuestsDisplay?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (ep.SpecialNotes?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    ep.GuestItems.Any(g => g.Name?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    ep.CorrespondentItems.Any(c => c.FullName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var sortIndex = CmbSortBy.SelectedIndex;
            filtered = sortIndex switch
            {
                0 => filtered.OrderByDescending(e => e.ScheduledExecutionTime.HasValue ? e.ScheduledExecutionTime.Value.Date : DateTime.MinValue)
                             .ThenBy(e => e.ScheduledExecutionTime),   // الأحدث: التاريخ تنازلياً ثم الوقت تصاعدياً
                1 => filtered.OrderBy(e => e.ScheduledExecutionTime),             // الأقدم: التاريخ والوقت تصاعدياً
                2 => filtered.OrderBy(e => e.StatusId)
                             .ThenByDescending(e => e.ScheduledExecutionTime.HasValue ? e.ScheduledExecutionTime.Value.Date : DateTime.MinValue)
                             .ThenBy(e => e.ScheduledExecutionTime), // الحالة: تجميع بالحالة ثم التاريخ تنازلي والوقت تصاعدي
                3 => filtered.OrderBy(e => e.ProgramName, StringComparer.OrdinalIgnoreCase),             // البرنامج
                4 => filtered.OrderBy(e => e.EpisodeName, StringComparer.OrdinalIgnoreCase),             // الاسم
                _ => filtered.OrderByDescending(e => e.ScheduledExecutionTime.HasValue ? e.ScheduledExecutionTime.Value.Date : DateTime.MinValue)
                             .ThenBy(e => e.ScheduledExecutionTime)
            };

            var resultList = filtered.ToList();

            // ─── ربط البيانات حسب نمط العرض ───
            CardsView.ItemsSource = resultList;
            TableView.ItemsSource = resultList;
            CompactView.ItemsSource = resultList;

            // ─── تحديث الإحصائيات والفلاتر النشطة ───
            UpdateChipCounts();
            UpdateActiveFilterChips();
            TxtResultsCount.Text = $"{resultList.Count} حلقة";

            if (resultList.Count == 0)
            {
                EmptyStateOverlay.Visibility = Visibility.Visible;
                CardsView.Visibility = Visibility.Collapsed;
                TableView.Visibility = Visibility.Collapsed;
                CompactView.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyStateOverlay.Visibility = Visibility.Collapsed;
                ApplyViewMode();
            }
        }

        private void UpdateActiveFilterChips()
        {
            var activeChips = new List<FilterChipItem>();

            if (!string.IsNullOrWhiteSpace(_activeProgramFilter))
            {
                activeChips.Add(new FilterChipItem { Type = "Program", Label = $"البرنامج: {_activeProgramFilter}" });
            }

            // فلتـر نطاق التاريخ
            if (!string.IsNullOrWhiteSpace(_activeDatePreset))
            {
                string label = _activeDatePreset switch
                {
                    "Today" => "اليوم",
                    "ThisWeek" => "هذا الأسبوع",
                    "ThisMonth" => "هذا الشهر",
                    "Custom" when _dateFrom.HasValue && _dateTo.HasValue => $"من {_dateFrom:yyyy/MM/dd} إلى {_dateTo:yyyy/MM/dd}",
                    "Custom" when _dateFrom.HasValue => $"من {_dateFrom:yyyy/MM/dd}",
                    "Custom" when _dateTo.HasValue => $"حتى {_dateTo:yyyy/MM/dd}",
                    _ => string.Empty
                };
                if (!string.IsNullOrWhiteSpace(label))
                    activeChips.Add(new FilterChipItem { Type = "Date", Label = $"التاريخ: {label}" });
            }

            // الترتيب (إذا لم يكن الافتراضي)
            if (CmbSortBy.SelectedIndex > 0)
            {
                activeChips.Add(new FilterChipItem { Type = "Sort", Label = $"الترتيب: {SortOptions[CmbSortBy.SelectedIndex]}" });
            }

            ItemsActiveChips.ItemsSource = activeChips;
            PnlActiveChips.Visibility = activeChips.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            // تحديث الـ Badge
            int totalActiveFilters = activeChips.Count;
            if (_activeStatusFilter.HasValue) totalActiveFilters++; // نعتبر حالة التبويب فلتر نشط أيضاً في الـ Badge

            if (totalActiveFilters > 0)
            {
                FilterBadge.Visibility = Visibility.Visible;
                TxtFilterCount.Text = totalActiveFilters.ToString();
            }
            else
            {
                FilterBadge.Visibility = Visibility.Collapsed;
            }
        }

        private void Chip_DeleteClick(object sender, RoutedEventArgs e)
        {
            if (sender is Chip chip && chip.DataContext is FilterChipItem item)
            {
                if (item.Type == "Program")
                {
                    CmbProgramFilter.SelectedItem = null;
                }
                else if (item.Type == "Sort")
                {
                    CmbSortBy.SelectedIndex = 0;
                }
                else if (item.Type == "Date")
                {
                    ClearDateFilter();
                }
                SavePreferences();
            }
        }

        private void BtnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Clear();
            FilterAll.IsChecked = true;
            _activeStatusFilter = null;
            CmbProgramFilter.SelectedItem = null;
            CmbSortBy.SelectedIndex = 0;
            ClearDateFilter();
            BtnToggleAdvancedFilters.IsChecked = false;
            RebindAndUpdateStats();
            SavePreferences();
        }

        // ═══════════════════════════════════════════════════════════
        // فلاتر نطاق التاريخ
        // ═══════════════════════════════════════════════════════════
        private void QuickDateFilter_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;

            if (sender == BtnToday)
            {
                _dateFrom = today;
                _dateTo = today;
                _activeDatePreset = "Today";
            }
            else if (sender == BtnThisWeek)
            {
                int diff = (int)today.DayOfWeek; // Sunday=0
                if (diff == 0) diff = 7; // Treat Sunday as end of week in Arabic culture
                _dateFrom = today.AddDays(-(diff - 1)); // Monday
                _dateTo = _dateFrom.Value.AddDays(6); // Sunday
                _activeDatePreset = "ThisWeek";
            }
            else if (sender == BtnThisMonth)
            {
                _dateFrom = new DateTime(today.Year, today.Month, 1);
                _dateTo = _dateFrom.Value.AddMonths(1).AddDays(-1);
                _activeDatePreset = "ThisMonth";
            }

            UpdateDateFilterUI();
            RebindAndUpdateStats();
            SavePreferences();
        }

        private void DpDateFrom_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
        {
            _dateFrom = DpDateFrom.SelectedDate;
            _activeDatePreset = "Custom";
            UpdateDateFilterUI();
            RebindAndUpdateStats();
            SavePreferences();
        }

        private void DpDateTo_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
        {
            _dateTo = DpDateTo.SelectedDate;
            _activeDatePreset = "Custom";
            UpdateDateFilterUI();
            RebindAndUpdateStats();
            SavePreferences();
        }

        private void BtnClearDateFilter_Click(object sender, RoutedEventArgs e)
        {
            ClearDateFilter();
            RebindAndUpdateStats();
            SavePreferences();
        }

        private void ClearDateFilter()
        {
            _dateFrom = null;
            _dateTo = null;
            _activeDatePreset = null;
            DpDateFrom.SelectedDate = null;
            DpDateTo.SelectedDate = null;
            BtnCustomRange.IsChecked = false;
            UpdateDateFilterUI();
        }

        private void UpdateDateFilterUI()
        {
            bool hasDateFilter = _dateFrom.HasValue || _dateTo.HasValue;
            BtnClearDateFilter.Visibility = hasDateFilter ? Visibility.Visible : Visibility.Collapsed;

            // Highlight active preset buttons
            BtnToday.Opacity = (_activeDatePreset == "Today") ? 1.0 : 0.6;
            BtnToday.FontWeight = (_activeDatePreset == "Today") ? FontWeights.Bold : FontWeights.Normal;
            BtnThisWeek.Opacity = (_activeDatePreset == "ThisWeek") ? 1.0 : 0.6;
            BtnThisWeek.FontWeight = (_activeDatePreset == "ThisWeek") ? FontWeights.Bold : FontWeights.Normal;
            BtnThisMonth.Opacity = (_activeDatePreset == "ThisMonth") ? 1.0 : 0.6;
            BtnThisMonth.FontWeight = (_activeDatePreset == "ThisMonth") ? FontWeights.Bold : FontWeights.Normal;
        }

        private void BtnCustomRange_Checked(object sender, RoutedEventArgs e)
        {
            SyncDateRangeUI();
        }

        private void BtnCustomRange_Unchecked(object sender, RoutedEventArgs e)
        {
            SyncDateRangeUI();
        }

        private void SyncDateRangeUI()
        {
            PnlDateRange.Visibility = BtnCustomRange.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        // ═══════════════════════════════════════════════════════════
        // العمليات المجمعة (Batch Operations)
        // ═══════════════════════════════════════════════════════════
        private void ItemCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingBatchBar) return;
            UpdateBatchActionBar();
        }

        private void UpdateBatchActionBar()
        {
            var selected = _allEpisodes.Where(e => e.IsSelected).ToList();
            int count = selected.Count;

            BatchActionBar.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            TxtSelectedCount.Text = $"{count} محددة";

            // الإجراءات المشتركة (intersection logic)
            bool canExecuted = selected.Count > 0 && selected.All(e => e.CanMarkExecuted);
            bool canPublished = selected.Count > 0 && selected.All(e => e.CanMarkPublished);
            bool canCancel = selected.Count > 0 && selected.All(e => e.CanCancel);
            bool canDelete = selected.Count > 0;

            BtnBatchExecuted.Visibility = canExecuted ? Visibility.Visible : Visibility.Collapsed;
            BtnBatchPublished.Visibility = canPublished ? Visibility.Visible : Visibility.Collapsed;
            BtnBatchCancel.Visibility = canCancel ? Visibility.Visible : Visibility.Collapsed;
            BtnBatchDelete.Visibility = canDelete ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SelectAllCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            try { _isUpdatingBatchBar = true; }
            finally { _isUpdatingBatchBar = false; }

            foreach (var ep in _allEpisodes)
                ep.IsSelected = true;
            UpdateBatchActionBar();
        }

        private void SelectAllCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            DeselectAll();
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            DeselectAll();
            UpdateBatchActionBar();
        }

        private void DeselectAll()
        {
            foreach (var ep in _allEpisodes)
                ep.IsSelected = false;
            // تحديث واجهة المستخدم بإعادة ربط القوائم
            var list = CardsView.ItemsSource as List<ActiveEpisodeDto>;
            if (list != null)
            {
                CardsView.ItemsSource = null;
                CardsView.ItemsSource = list;
                TableView.ItemsSource = null;
                TableView.ItemsSource = list;
                CompactView.ItemsSource = null;
                CompactView.ItemsSource = list;
            }
            UpdateBatchActionBar();
        }

        private async void BtnBatchExecuted_Click(object sender, RoutedEventArgs e)
        {
            var selected = _allEpisodes.Where(ep => ep.IsSelected && ep.CanMarkExecuted).ToList();
            if (selected.Count == 0) return;

            var confirm = await MessageService.Current.ShowConfirmationAsync(
                $"تنفيذ {selected.Count} حلقة؟", "تأكيد العملية المجمعة");
            if (!confirm) return;

            int success = 0, fail = 0;
            foreach (var ep in selected)
            {
                var mainWindow = Window.GetWindow(this) as ModernMainWindow;
                if (mainWindow != null) await mainWindow.ShowOverlay();
                try
                {
                    var execService = _serviceProvider.GetRequiredService<IExecutionService>();
                    var dialog = new ExecutionLogDialog(ep.EpisodeId, execService, _session) { Owner = mainWindow };
                    if (dialog.ShowDialog() == true) success++; else fail++;
                }
                finally { if (mainWindow != null) await mainWindow.HideOverlay(); }
            }

            await LoadDataAsync();
            MessageService.Current.ShowSuccess(Messages.BatchActioned("تنفيذ", success, fail));
        }

        private async void BtnBatchPublished_Click(object sender, RoutedEventArgs e)
        {
            var selected = _allEpisodes.Where(ep => ep.IsSelected && ep.CanMarkPublished).ToList();
            if (selected.Count == 0) return;

            var confirm = await MessageService.Current.ShowConfirmationAsync(
                $"نشر {selected.Count} حلقة رقمياً؟", "تأكيد النشر الجماعي");
            if (!confirm) return;

            int success = 0, fail = 0;
            foreach (var ep in selected)
            {
                var mainWindow = Window.GetWindow(this) as ModernMainWindow;
                if (mainWindow != null) await mainWindow.ShowOverlay();
                try
                {
                    var pubService = _serviceProvider.GetRequiredService<IPublishingService>();
                    var guests = await _episodeService.GetEpisodeGuestsAsync(ep.EpisodeId);
                    var dialog = new PublishingLogDialog(pubService, _session, ep.EpisodeId, guests) { Owner = mainWindow };
                    if (dialog.ShowDialog() == true) success++; else fail++;
                }
                finally { if (mainWindow != null) await mainWindow.HideOverlay(); }
            }

            await LoadDataAsync();
            MessageService.Current.ShowSuccess(Messages.BatchActioned("نشر", success, fail));
        }

        private async void BtnBatchCancel_Click(object sender, RoutedEventArgs e)
        {
            var selected = _allEpisodes.Where(ep => ep.IsSelected && ep.CanCancel).ToList();
            if (selected.Count == 0) return;

            var reasonDialog = new ReasonInputDialog("إلغاء جماعي", "سبب الإلغاء (سيُطبّق على الجميع):");
            if (reasonDialog.ShowDialog() != true) return;

            int success = 0, fail = 0;
            foreach (var ep in selected)
            {
                var res = await _episodeService.CancelEpisodeAsync(ep.EpisodeId, reasonDialog.Reason!, _session);
                if (res.IsSuccess) success++; else fail++;
            }

            await LoadDataAsync();
            MessageService.Current.ShowSuccess(Messages.BatchActioned("إلغاء", success, fail));
        }

        private async void BtnBatchDelete_Click(object sender, RoutedEventArgs e)
        {
            var selected = _allEpisodes.Where(ep => ep.IsSelected).ToList();
            if (selected.Count == 0) return;

            var names = string.Join("، ", selected.Take(3).Select(ep => $"«{ep.EpisodeName}»"));
            if (selected.Count > 3) names += $" و{selected.Count - 3} أخرى";

            var confirm = await MessageService.Current.ShowConfirmationAsync(
                $"حذف {selected.Count} حلقة؟\n{names}\n\nهذا الإجراء لا يمكن التراجع عنه.", "تأكيد الحذف الجماعي");
            if (!confirm) return;

            int success = 0, fail = 0;
            foreach (var ep in selected)
            {
                var res = await _episodeService.DeleteEpisodeAsync(ep.EpisodeId, _session);
                if (res.IsSuccess) success++; else fail++;
            }

            await LoadDataAsync();
            MessageService.Current.ShowSuccess(Messages.BatchActioned("حذف", success, fail));
        }

        // ═══════════════════════════════════════════════════════════
        // فلاتر الحالة (Chips)
        // ═══════════════════════════════════════════════════════════
        private void StatusFilter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                _activeStatusFilter = rb.Name switch
                {
                    nameof(FilterPlanned) => EpisodeStatus.Planned,
                    nameof(FilterExecuted) => EpisodeStatus.Executed,
                    nameof(FilterPublished) => EpisodeStatus.Published,
                    nameof(FilterWebPublished) => EpisodeStatus.WebsitePublished,
                    nameof(FilterCancelled) => EpisodeStatus.Cancelled,
                    _ => null
                };
                RebindAndUpdateStats();
                SavePreferences();
            }
        }



        private void UpdateChipCounts()
        {
            ChipPlanned.Text = $"مجدولة ({_allEpisodes.Count(e => e.StatusId == EpisodeStatus.Planned)})";
            ChipExecuted.Text = $"منفّذة ({_allEpisodes.Count(e => e.StatusId == EpisodeStatus.Executed)})";
            ChipPublished.Text = $"منشورة ({_allEpisodes.Count(e => e.StatusId == EpisodeStatus.Published)})";
            ChipWebPublished.Text = $"منشورة على الموقع ({_allEpisodes.Count(e => e.StatusId == EpisodeStatus.WebsitePublished)})";
            ChipCancelled.Text = $"ملغاة ({_allEpisodes.Count(e => e.StatusId == EpisodeStatus.Cancelled)})";
        }

        // ═══════════════════════════════════════════════════════════
        // تصفية حسب البرنامج
        // ═══════════════════════════════════════════════════════════
        private void CmbProgramFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _activeProgramFilter = CmbProgramFilter.SelectedItem as string;
            RebindAndUpdateStats();
            SavePreferences();
        }

        // ═══════════════════════════════════════════════════════════
        // ترتيب
        // ═══════════════════════════════════════════════════════════
        private void CmbSortBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allEpisodes.Count > 0)
            {
                RebindAndUpdateStats();
                SavePreferences();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // تبديل أنماط العرض
        // ═══════════════════════════════════════════════════════════
        private void ViewMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                _currentViewMode = rb.Name switch
                {
                    nameof(ViewTable) => EpisodeViewMode.Table,
                    nameof(ViewCompact) => EpisodeViewMode.Compact,
                    _ => EpisodeViewMode.Cards
                };
                ApplyViewMode();
                SavePreferences();
            }
        }

        private void ApplyViewMode()
        {
            CardsView.Visibility = _currentViewMode == EpisodeViewMode.Cards ? Visibility.Visible : Visibility.Collapsed;
            TableView.Visibility = _currentViewMode == EpisodeViewMode.Table ? Visibility.Visible : Visibility.Collapsed;
            CompactView.Visibility = _currentViewMode == EpisodeViewMode.Compact ? Visibility.Visible : Visibility.Collapsed;
        }

        // ═══════════════════════════════════════════════════════════
        // حفظ واستعادة تفضيلات العرض
        // ═══════════════════════════════════════════════════════════
        private void RestorePreferences()
        {
            // نمط العرض
            _currentViewMode = _prefs.ViewMode switch
            {
                "Table" => EpisodeViewMode.Table,
                "Compact" => EpisodeViewMode.Compact,
                _ => EpisodeViewMode.Cards
            };
            ViewCards.IsChecked = _currentViewMode == EpisodeViewMode.Cards;
            ViewTable.IsChecked = _currentViewMode == EpisodeViewMode.Table;
            ViewCompact.IsChecked = _currentViewMode == EpisodeViewMode.Compact;

            // الترتيب
            if (_prefs.SortIndex >= 0 && _prefs.SortIndex < SortOptions.Length)
                CmbSortBy.SelectedIndex = _prefs.SortIndex;
            else
                CmbSortBy.SelectedIndex = 0;

            // فلتر الحالة
            _activeStatusFilter = _prefs.StatusFilter switch
            {
                "Planned" => EpisodeStatus.Planned,
                "Executed" => EpisodeStatus.Executed,
                "Published" => EpisodeStatus.Published,
                "WebsitePublished" => EpisodeStatus.WebsitePublished,
                "Cancelled" => EpisodeStatus.Cancelled,
                _ => null
            };
            FilterAll.IsChecked = !_activeStatusFilter.HasValue;
            if (_activeStatusFilter.HasValue)
            {
                var name = _activeStatusFilter.Value switch
                {
                    0 => nameof(FilterPlanned),
                    1 => nameof(FilterExecuted),
                    2 => nameof(FilterPublished),
                    3 => nameof(FilterWebPublished),
                    _ => nameof(FilterCancelled)
                };
                var rb = FindName(name) as RadioButton;
                if (rb != null) rb.IsChecked = true;
            }

            // فلتر البرنامج
            _activeProgramFilter = _prefs.ProgramFilter;

            // فلتر التاريخ
            _dateFrom = _prefs.DateFrom;
            _dateTo = _prefs.DateTo;
            _activeDatePreset = _prefs.DatePreset;
            if (_dateFrom.HasValue) DpDateFrom.SelectedDate = _dateFrom;
            if (_dateTo.HasValue) DpDateTo.SelectedDate = _dateTo;
            UpdateDateFilterUI();
        }

        private void ApplySearchFromPrefs()
        {
            if (!string.IsNullOrWhiteSpace(_prefs.SearchText))
            {
                TxtSearch.Text = _prefs.SearchText;
            }
        }

        private void SavePreferences()
        {
            _prefs.ViewMode = _currentViewMode.ToString();
            _prefs.SortIndex = CmbSortBy.SelectedIndex;
            _prefs.StatusFilter = _activeStatusFilter switch
            {
                EpisodeStatus.Planned => "Planned",
                EpisodeStatus.Executed => "Executed",
                EpisodeStatus.Published => "Published",
                EpisodeStatus.WebsitePublished => "WebsitePublished",
                EpisodeStatus.Cancelled => "Cancelled",
                _ => null
            };
            _prefs.ProgramFilter = _activeProgramFilter;
            _prefs.DateFrom = _dateFrom;
            _prefs.DateTo = _dateTo;
            _prefs.DatePreset = _activeDatePreset;
            _prefs.SearchText = TxtSearch.Text?.Trim() ?? string.Empty;
            _prefs.Save();
        }

        // ═══════════════════════════════════════════════════════════
        // البحث
        // ═══════════════════════════════════════════════════════════
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => RebindAndUpdateStats();

        // ═══════════════════════════════════════════════════════════
        // إجراءات سير العمل
        // ═══════════════════════════════════════════════════════════
        private async void BtnAddEpisode_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as ModernMainWindow;
            if (mainWindow != null) await mainWindow.ShowOverlay();
            try
            {
                var dialog = new EpisodeFormControl(_episodeService, _programService, _guestService, _correspondentService, _employeeService, _session)
                { Owner = mainWindow };
                if (dialog.ShowDialog() == true)
                {
                    await LoadDataAsync();
                    MessageService.Current.ShowSuccess("تم جدولة الحلقة بنجاح.");
                }
            }
            finally { if (mainWindow != null) await mainWindow.HideOverlay(); }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep)
            {
                var mainWindow = Window.GetWindow(this) as ModernMainWindow;
                if (mainWindow != null) await mainWindow.ShowOverlay();
                try
                {
                    var dialog = new EpisodeFormControl(_episodeService, _programService, _guestService, _correspondentService, _employeeService, _session, ep.EpisodeId)
                    { Owner = mainWindow };
                    if (dialog.ShowDialog() == true)
                    {
                        await LoadDataAsync();
                        MessageService.Current.ShowSuccess(Messages.Updated("بيانات الحلقة", ep.EpisodeName));
                    }
                }
                finally { if (mainWindow != null) await mainWindow.HideOverlay(); }
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is ActiveEpisodeDto selectedEpisode)
            {
                if (await MessageService.Current.ShowConfirmationAsync($"حذف {selectedEpisode.EpisodeName}؟", "تأكيد"))
                {
                    var res = await _episodeService.DeleteEpisodeAsync(selectedEpisode.EpisodeId, _session);

                    if (res.IsSuccess)
                    {
                        await LoadDataAsync();
                        MessageService.Current.ShowSuccess(Messages.Deleted("الحلقة", selectedEpisode.EpisodeName));
                    }
                    else
                    {
                        MessageService.Current.ShowWarning(res.ErrorMessage ?? "فشل الحذف.");
                    }
                }
            }
        }

        private async void BtnMarkExecuted_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep)
            {
                var mainWindow = Window.GetWindow(this) as ModernMainWindow;
                if (mainWindow != null) await mainWindow.ShowOverlay();
                try
                {
                    var execService = _serviceProvider.GetRequiredService<IExecutionService>();
                    var dialog = new ExecutionLogDialog(ep.EpisodeId, execService, _session) { Owner = mainWindow };
                    if (dialog.ShowDialog() == true)
                    {
                        MessageService.Current.ShowSuccess(Messages.ActionedWithName("تسجيل تنفيذ", "الحلقة", ep.EpisodeName));
                        await LoadDataAsync();
                    }
                }
                finally { if (mainWindow != null) await mainWindow.HideOverlay(); }
            }
        }

        private async void BtnMarkPublished_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep)
            {
                var mainWindow = Window.GetWindow(this) as ModernMainWindow;
                if (mainWindow != null) await mainWindow.ShowOverlay();
                try
                {
                    var pubService = _serviceProvider.GetRequiredService<IPublishingService>();
                    var guests = await _episodeService.GetEpisodeGuestsAsync(ep.EpisodeId);
                    var dialog = new PublishingLogDialog(pubService, _session, ep.EpisodeId, guests) { Owner = mainWindow };
                    if (dialog.ShowDialog() == true)
                    {
                        MessageService.Current.ShowSuccess(Messages.ActionedWithName("تسجيل النشر الرقمي لـ", "الحلقة", ep.EpisodeName));
                        await LoadDataAsync();
                    }
                }
                finally { if (mainWindow != null) await mainWindow.HideOverlay(); }
            }
        }

        private async void BtnToggleWebsitePublish_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep)
            {
                var mainWindow = Window.GetWindow(this) as ModernMainWindow;
                if (mainWindow != null) await mainWindow.ShowOverlay();
                try
                {
                    var publishingService = _serviceProvider.GetRequiredService<IPublishingService>();
                    var dialog = new WebsitePublishDialog(publishingService, _session, ep.EpisodeId) { Owner = mainWindow };
                    if (dialog.ShowDialog() == true)
                    {
                        MessageService.Current.ShowSuccess(Messages.ActionedWithName("نشر", "الحلقة على الموقع", ep.EpisodeName));
                        await LoadDataAsync();
                    }
                }
                finally { if (mainWindow != null) await mainWindow.HideOverlay(); }
            }
        }

        private async void BtnViewRecords_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep)
            {
                var mainWindow = Window.GetWindow(this) as ModernMainWindow;
                if (mainWindow != null) await mainWindow.ShowOverlay();
                try
                {
                    var publishingService = _serviceProvider.GetRequiredService<IPublishingService>();
                    var executionService = _serviceProvider.GetRequiredService<IExecutionService>();
                    var dialog = new EpisodeRecordsView(
                        publishingService, executionService, _session, _serviceProvider,
                        ep.EpisodeId, ep.EpisodeName ?? string.Empty)
                    { Owner = mainWindow };
                    dialog.ShowDialog();
                }
                finally { if (mainWindow != null) await mainWindow.HideOverlay(); }
            }
        }

        private async void BtnRevert_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep)
            {
                var reasonDialog = new ReasonInputDialog("تراجع", "السبب:");
                if (reasonDialog.ShowDialog() == true)
                {
                    var res = await _episodeService.RevertEpisodeStatusAsync(ep.EpisodeId, reasonDialog.Reason!, _session);
                    if (res.IsSuccess)
                    {
                        await LoadDataAsync();
                        MessageService.Current.ShowSuccess(Messages.Reverted("الحلقة", ep.EpisodeName));
                    }
                    else MessageService.Current.ShowWarning(res.ErrorMessage ?? "فشل التراجع.");
                }
            }
        }

        private async void BtnCancelEpisode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep)
            {
                var reasonDialog = new ReasonInputDialog("إلغاء", "السبب:");
                if (reasonDialog.ShowDialog() == true)
                {
                    var res = await _episodeService.CancelEpisodeAsync(ep.EpisodeId, reasonDialog.Reason!, _session);
                    if (res.IsSuccess)
                    {
                        await LoadDataAsync();
                        MessageService.Current.ShowSuccess(Messages.Cancelled("الحلقة", ep.EpisodeName));
                    }
                    else MessageService.Current.ShowWarning(res.ErrorMessage ?? "فشل الإلغاء.");
                }
            }
        }

        private async void BtnEditCancellationReason_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep)
            {
                var reasonDialog = new ReasonInputDialog("تعديل سبب الإلغاء", "السبب:", ep.CancellationReason);
                if (reasonDialog.ShowDialog() == true)
                {
                    var res = await _episodeService.UpdateCancellationReasonAsync(ep.EpisodeId, reasonDialog.Reason!, _session);
                    if (res.IsSuccess)
                    {
                        await LoadDataAsync();
                        MessageService.Current.ShowSuccess(Messages.Updated("سبب إلغاء الحلقة", ep.EpisodeName));
                    }
                    else MessageService.Current.ShowWarning(res.ErrorMessage ?? "فشل تحديث السبب.");
                }
            }
        }


    }
}
