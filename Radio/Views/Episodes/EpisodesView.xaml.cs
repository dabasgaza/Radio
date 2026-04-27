using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using static Domain.Models.BroadcastWorkflowDBContext;

namespace Radio.Views.Episodes
{
    /// <summary>
    /// شاشة إدارة الحلقات — تعرض قائمة الحلقات النشطة مع إمكانية
    /// الإضافة والتعديل والحذف والبحث والتنفيذ والنشر.
    /// </summary>
    public partial class EpisodesView : UserControl
    {
        private readonly IEpisodeService _episodeService;
        private readonly IProgramService _programService;
        private readonly IGuestService _guestService;
        private readonly UserSession _session;
        private readonly IServiceProvider _serviceProvider;
        private List<ActiveEpisodeDto> _allEpisodes = [];

        public EpisodesView(
            IEpisodeService epService,
            IProgramService progService,
            UserSession session,
            IServiceProvider serviceProvider,
            IGuestService guestService)
        {
            InitializeComponent();

            _episodeService = epService;
            _programService = progService;
            _session = session;
            _serviceProvider = serviceProvider;
            _guestService = guestService;

            // ✅ AppPermissions بدلاً من نصوص ثابتة
            BtnAddEpisode.Visibility = _session.HasPermission(AppPermissions.EpisodeManage)
                ? Visibility.Visible
                : Visibility.Collapsed;

            Loaded += async (_, _) => await LoadDataAsync();
        }

        #region Data Loading

        /// <summary>
        /// تحميل جميع الحلقات النشطة وربطها بالـ DataGrid مع تحديث الإحصائيات.
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                _allEpisodes = (await _episodeService.GetActiveEpisodesAsync()).ToList();
                DgEpisodes.ItemsSource = _allEpisodes;
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError("ليس لديك صلاحية لعرض بيانات الحلقات.");
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تحميل الحلقات.");
            }
        }

        #endregion

        #region Search

        /// <summary>
        /// البحث الفوري في قائمة الحلقات حسب عنوان الحلقة أو اسم الضيف.
        /// </summary>
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || _allEpisodes.Count == 0)
                return;

            string keyword = textBox.Text.Trim();

            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allEpisodes
                : _allEpisodes.Where(ep =>
                    (ep.EpisodeName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (ep.GuestsDisplay?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));

            DgEpisodes.ItemsSource = filtered.ToList();
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// فتح نافذة إضافة حلقة جديدة.
        /// </summary>
        private async void BtnAddEpisode_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EpisodeFormDialog(
                _episodeService, _programService, _guestService, _session);

            if (dialog.ShowDialog() == true)
                await LoadDataAsync();   // ✅ await بدلاً من fire-and-forget
        }

        /// <summary>
        /// فتح نافذة تعديل حلقة موجودة.
        /// </summary>
        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto selectedEpisode)
                return;

            var dialog = new EpisodeFormDialog(
                _episodeService, _programService, _guestService,
                _session, selectedEpisode);

            if (dialog.ShowDialog() == true)
                await LoadDataAsync();   // ✅ await بدلاً من fire-and-forget
        }

        /// <summary>
        /// حذف حلقة بعد التحقق من حالتها وتأكيد المستخدم.
        /// </summary>
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto selectedEpisode)
                return;

            // ✅ منع حذف الحلقات المنفذة أو المنشورة
            if (selectedEpisode.StatusText is "منفّذة" or "منشورة")
            {
                MessageService.Current.ShowWarning(
                    "لا يمكن حذف حلقة تم تنفيذها أو نشرها، يُرجى إلغائها أولاً.");
                return;
            }

            bool isConfirmed = await MessageService.Current.ShowConfirmationAsync(
                $"هل أنت متأكد من حذف الحلقة: {selectedEpisode.EpisodeName}؟",
                "تأكيد الحذف");

            if (!isConfirmed)
                return;

            try
            {
                await _episodeService.DeleteEpisodeAsync(selectedEpisode.EpisodeId, _session);
                await LoadDataAsync();
                MessageService.Current.ShowSuccess($"تم حذف الحلقة «{selectedEpisode.EpisodeName}» بنجاح.");
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError("ليس لديك صلاحية لحذف الحلقات.");
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حذف الحلقة.");
            }
        }

        #endregion

        #region Execution & Publishing

        /// <summary>
        /// فتح نافذة تسجيل تنفيذ حلقة (للحلقات المجدولة فقط).
        /// </summary>
        private async void BtnMarkExecuted_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto ep)
                return;

            if (ep.StatusText != "مجدولة")
                return;

            var execService = _serviceProvider.GetRequiredService<IExecutionService>();
            var dialog = new ExecutionLogDialog(ep.EpisodeId, execService, _session);

            if (dialog.ShowDialog() == true)
                await LoadDataAsync();   // ✅ await بدلاً من fire-and-forget
        }

        /// <summary>
        /// فتح نافذة تسجيل نشر حلقة (للحلقات المنفّذة فقط).
        /// </summary>
        private async void BtnMarkPublished_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto ep)
                return;

            if (ep.StatusText != "منفّذة")
                return;

            var pubService = _serviceProvider.GetRequiredService<IPublishingService>();
            var dialog = new PublishingLogDialog(ep.EpisodeId, pubService, _session);

            if (dialog.ShowDialog() == true)
                await LoadDataAsync();   // ✅ await بدلاً من fire-and-forget
        }

        #endregion


        private void UpdateStatistics(List<ActiveEpisodeView> data)
        {
            if (data == null || !data.Any())
            {
                TxtTotal.Text = "0";
                TxtExecuted.Text = "0";
                TxtPublished.Text = "0";
                return;
            }

            // عدد الحلقات الكلي
            TxtTotal.Text = data.Count.ToString();

            // عدد المنشورة
            var publishedCount = data.Count(e => e.StatusText == "تم النشر");
            TxtPublished.Text = $"{publishedCount}";

            // البث القادم: أقرب حلقة مخططة بعد الآن
            var now = DateTime.Now;
            var nextEpisode = data
                .Where(e => e.ScheduledExecutionTime > now)
                .OrderBy(e => e.ScheduledExecutionTime)
                .FirstOrDefault();

            //if (nextEpisode != null)
            //{
            //    var dayName = nextEpisode.ScheduledExecutionTime.ToString("dddd", new System.Globalization.CultureInfo("ar-SA"));
            //    var time = nextEpisode.ScheduledExecutionTime.ToString("hh:mm tt", new System.Globalization.CultureInfo("ar-SA"));
            //    TxtExecuted.Text = $"البث القادم: {nextEpisode.ProgramName} — {dayName} {time}";
            //}
            //else
            //{
            //    TxtExecuted.Text = "البث القادم: لا يوجد";
            //}
        }

        private void BtnViewDetails_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}