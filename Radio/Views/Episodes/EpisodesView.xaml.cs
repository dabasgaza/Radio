// ═══════════════════════════════════════════════════════════════════════════
// EpisodesView.xaml.cs — المرحلة الأولى: الأساسيات السريعة
// ═══════════════════════════════════════════════════════════════════════════
// التحسينات المُنفَّذة:
//   1. فلاتر سريعة حسب الحالة (StatusFilter)
//   2. بحث موسّع يشمل الضيوف والمراسلين والملاحظات
//   3. عداد نتائج البحث والتصفية
//   4. اختصارات لوحة المفاتيح (Ctrl+N, Ctrl+F, Ctrl+K)
//   5. إخفاء الأزرار المعطلة عبر Visibility Converter (في XAML)
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

        // ─── حقل الفلتر النشط ───
        private byte? _activeStatusFilter = null; // null = الكل

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

            // ─── تسجيل اختصارات لوحة المفاتيح ───
            KeyDown += EpisodesView_KeyDown;
        }

        // ═══════════════════════════════════════════════════════════
        // اختصارات لوحة المفاتيح
        // ═══════════════════════════════════════════════════════════
        private void EpisodesView_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+N → جدولة حلقة جديدة
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.N)
            {
                e.Handled = true;
                BtnAddEpisode_Click(sender, e);
            }
            // Ctrl+F → التركيز على حقل البحث
            else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.F)
            {
                e.Handled = true;
                TxtSearch.Focus();
                TxtSearch.SelectAll();
            }
            // Ctrl+K → عرض اختصارات لوحة المفاتيح
            else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.K)
            {
                e.Handled = true;
                ShowKeyboardShortcuts();
            }
        }

        private void BtnKeyboardHelp_Click(object sender, RoutedEventArgs e)
        {
            ShowKeyboardShortcuts();
        }

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
                _allEpisodes = (await _episodeService.GetActiveEpisodesAsync()).ToList();
                RebindAndUpdateStats();
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError("حدث خطأ أثناء تحميل الحلقات: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // إعادة الربط وتحديث الإحصائيات (مع دعم الفلاتر والبحث الموسّع)
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

            // ─── بحث موسّع (عناوين + برامج + ضيوف + مراسلين + ملاحظات) ───
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

            var resultList = filtered.ToList();
            DgEpisodes.ItemsSource = resultList;

            // ─── تحديث الإحصائيات ───
            UpdateStatistics(_allEpisodes); // الإحصائيات دائماً على الكل

            // ─── تحديث عدادات Chips ───
            UpdateChipCounts();

            // ─── تحديث عداد النتائج ───
            TxtResultCount.Text = $"عرض {resultList.Count} من {_allEpisodes.Count} حلقة";
        }

        // ═══════════════════════════════════════════════════════════
        // فلاتر الحالة السريعة (Chips)
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
                    _ => null // الكل
                };
                RebindAndUpdateStats();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // تحديث عدادات Chips
        // ═══════════════════════════════════════════════════════════
        private void UpdateChipCounts()
        {
            ChipPlanned.Text = $"مجدولة ({_allEpisodes.Count(e => e.StatusId == EpisodeStatus.Planned)})";
            ChipExecuted.Text = $"منفّذة ({_allEpisodes.Count(e => e.StatusId == EpisodeStatus.Executed)})";
            ChipPublished.Text = $"منشورة رقمياً ({_allEpisodes.Count(e => e.StatusId == EpisodeStatus.Published)})";
            ChipWebPublished.Text = $"منشورة على الموقع ({_allEpisodes.Count(e => e.StatusId == EpisodeStatus.WebsitePublished)})";
            ChipCancelled.Text = $"ملغاة ({_allEpisodes.Count(e => e.StatusId == EpisodeStatus.Cancelled)})";
        }

        // ═══════════════════════════════════════════════════════════
        // البحث
        // ═══════════════════════════════════════════════════════════
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => RebindAndUpdateStats();

        // ═══════════════════════════════════════════════════════════
        // إجراءات سير العمل (تبقى كما هي)
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
                        await LoadDataAsync();
                        MessageService.Current.ShowSuccess("تم تسجيل تنفيذ الحلقة بنجاح.");
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
                        await LoadDataAsync();
                        MessageService.Current.ShowSuccess("تم تسجيل النشر الرقمي بنجاح.");
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
                        await LoadDataAsync();
                        MessageService.Current.ShowSuccess("تم نشر الحلقة على الموقع بنجاح.");
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
                    var dialog = new EpisodeRecordsView(publishingService, executionService, _session, _serviceProvider, ep.EpisodeId, ep.EpisodeName ?? string.Empty)
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

        // ═══════════════════════════════════════════════════════════
        // تحديث الإحصائيات
        // ═══════════════════════════════════════════════════════════
        private void UpdateStatistics(List<ActiveEpisodeDto> data)
        {
            TxtTotal.Text = data.Count.ToString();
            TxtExecuted.Text = data.Count(e => e.StatusId == EpisodeStatus.Executed).ToString();
            TxtPublished.Text = data.Count(e => e.StatusId == EpisodeStatus.Published || e.StatusId == EpisodeStatus.WebsitePublished).ToString();
        }
    }
}
