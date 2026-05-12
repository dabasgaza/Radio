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
using Microsoft.Extensions.DependencyInjection;
using Radio.Views.Common;
using Radio.Views.Publishing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace Radio.Views.Episodes
{
    public partial class EpisodesView : UserControl
    {
        private readonly IEpisodeService _episodeService;
        private readonly IProgramService _programService;
        private readonly IGuestService _guestService;
        private readonly ICorrespondentService _correspondentService;
        private readonly IEmployeeService _employeeService;
        private readonly UserSession _session;
        private readonly IServiceProvider _serviceProvider;
        private List<ActiveEpisodeDto> _allEpisodes = [];

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

            Loaded += async (_, _) => await LoadDataAsync();
            KeyDown += EpisodesView_KeyDown;

            // ─── تهيئة خيارات الترتيب ───
            CmbSortBy.ItemsSource = SortOptions;
            CmbSortBy.SelectedIndex = 0;
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

            // ─── ترتيب ───
            var sortIndex = CmbSortBy.SelectedIndex;
            filtered = sortIndex switch
            {
                0 => filtered.OrderByDescending(e => e.ScheduledExecutionTime),   // الأحدث
                1 => filtered.OrderBy(e => e.ScheduledExecutionTime),             // الأقدم
                2 => filtered.OrderBy(e => e.StatusId).ThenByDescending(e => e.ScheduledExecutionTime), // الحالة
                3 => filtered.OrderBy(e => e.ProgramName, StringComparer.OrdinalIgnoreCase),             // البرنامج
                4 => filtered.OrderBy(e => e.EpisodeName, StringComparer.OrdinalIgnoreCase),             // الاسم
                _ => filtered.OrderByDescending(e => e.ScheduledExecutionTime)
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
            }
        }

        private void BtnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Clear();
            FilterAll.IsChecked = true;
            _activeStatusFilter = null;
            CmbProgramFilter.SelectedItem = null;
            CmbSortBy.SelectedIndex = 0;
            BtnToggleAdvancedFilters.IsChecked = false;
            RebindAndUpdateStats();
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
        }

        // ═══════════════════════════════════════════════════════════
        // ترتيب
        // ═══════════════════════════════════════════════════════════
        private void CmbSortBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allEpisodes.Count > 0)
                RebindAndUpdateStats();
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
            }
        }

        private void ApplyViewMode()
        {
            CardsView.Visibility = _currentViewMode == EpisodeViewMode.Cards ? Visibility.Visible : Visibility.Collapsed;
            TableView.Visibility = _currentViewMode == EpisodeViewMode.Table ? Visibility.Visible : Visibility.Collapsed;
            CompactView.Visibility = _currentViewMode == EpisodeViewMode.Compact ? Visibility.Visible : Visibility.Collapsed;
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
                        MessageService.Current.ShowSuccess("تم تحديث بيانات الحلقة بنجاح.");
                    }
                }
                finally { if (mainWindow != null) await mainWindow.HideOverlay(); }
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto selectedEpisode)
            {
                if (await MessageService.Current.ShowConfirmationAsync($"حذف {selectedEpisode.EpisodeName}؟", "تأكيد"))
                {
                    var res = await _episodeService.DeleteEpisodeAsync(selectedEpisode.EpisodeId, _session);
                    if (res.IsSuccess)
                    {
                        await LoadDataAsync();
                        MessageService.Current.ShowSuccess($"تم حذف الحلقة «{selectedEpisode.EpisodeName}» بنجاح.");
                    }
                    else MessageService.Current.ShowWarning(res.ErrorMessage ?? "فشل الحذف.");
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
                        MessageService.Current.ShowSuccess("تم تسجيل تنفيذ الحلقة بنجاح.");
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
                        MessageService.Current.ShowSuccess("تم تسجيل النشر الرقمي بنجاح.");
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
                        MessageService.Current.ShowSuccess("تم نشر الحلقة على الموقع بنجاح.");
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
                        MessageService.Current.ShowSuccess("تم التراجع عن الحالة بنجاح.");
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
                        MessageService.Current.ShowSuccess("تم إلغاء الحلقة بنجاح.");
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
                        MessageService.Current.ShowSuccess("تم تحديث سبب الإلغاء بنجاح.");
                    }
                    else MessageService.Current.ShowWarning(res.ErrorMessage ?? "فشل تحديث السبب.");
                }
            }
        }


    }
}
