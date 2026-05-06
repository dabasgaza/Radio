using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Radio.Views.Publishing;
using System.Windows;

namespace Radio.Views.Episodes
{
    /// <summary>
    /// شاشة عرض سجلات الحلقة — تُظهر سجل التنفيذ والنشر الرقمي ونشر الموقع
    /// مع إمكانية تعديل أي سجل مباشرة عبر فتح النافذة المناسبة في وضع التعديل
    /// </summary>
    public partial class EpisodeRecordsView
    {
        private readonly IPublishingService _publishingService;
        private readonly IExecutionService _executionService;
        private readonly UserSession _session;
        private readonly IServiceProvider _serviceProvider;
        private readonly int _episodeId;
        private readonly string _episodeName;

        // ═══ بيانات السجلات المحفوظة — تُستخدم عند فتح نافذة التعديل ═══
        private ExecutionLogDto? _executionLog;
        private List<SocialMediaPublishingLogDto> _socialLogs = [];
        private WebsitePublishingLogDto? _websiteLog;

        public EpisodeRecordsView(
            IPublishingService publishingService,
            IExecutionService executionService,
            UserSession session,
            IServiceProvider serviceProvider,
            int episodeId,
            string episodeName)
        {
            InitializeComponent();

            _publishingService = publishingService;
            _executionService = executionService;
            _session = session;
            _serviceProvider = serviceProvider;
            _episodeId = episodeId;
            _episodeName = episodeName;

            IsWindowDraggable = true;

            // عرض اسم الحلقة في الهيدر
            TxtHeaderSubtitle.Text = episodeName;

            Loaded += async (_, _) => await LoadRecordsAsync();
        }

        // ═══════════════════════════════════════════
        //  تحميل السجلات من الخدمات
        // ═══════════════════════════════════════════

        /// <summary>
        /// تحميل جميع السجلات المرتبطة بالحلقة وعرضها
        /// </summary>
        private async Task LoadRecordsAsync()
        {
            try
            {
                // 1. تحميل سجل التنفيذ
                _executionLog = await _executionService.GetExecutionLogAsync(_episodeId);

                // 2. تحميل سجلات النشر الرقمي
                _socialLogs = await _publishingService.GetEpisodeSocialLogsAsync(_episodeId);

                // 3. تحميل سجل نشر الموقع
                _websiteLog = await _publishingService.GetWebsitePublishingLogAsync(_episodeId);

                // عرض البيانات في الأقسام
                DisplayExecutionRecord();
                DisplaySocialRecord();
                DisplayWebsiteRecord();

                // إخفاء رسالة "لا توجد سجلات" إذا وُجد أي سجل
                PnlNoRecords.Visibility = (_executionLog is null && !_socialLogs.Any() && _websiteLog is null)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"خطأ في تحميل السجلات: {ex.Message}");
            }
        }

        /// <summary>
        /// عرض بيانات سجل التنفيذ في القسم المخصص
        /// </summary>
        private void DisplayExecutionRecord()
        {
            if (_executionLog is null)
            {
                PnlExecution.Visibility = Visibility.Collapsed;
                return;
            }

            PnlExecution.Visibility = Visibility.Visible;

            var parts = new List<string>();
            if (_executionLog.DurationMinutes > 0)
                parts.Add($"المدة: {_executionLog.DurationMinutes} دقيقة");
            if (!string.IsNullOrWhiteSpace(_executionLog.ExecutionNotes))
                parts.Add($"الملاحظات: {_executionLog.ExecutionNotes}");
            if (!string.IsNullOrWhiteSpace(_executionLog.IssuesEncountered))
                parts.Add($"المشاكل: {_executionLog.IssuesEncountered}");

            TxtExecDetails.Text = parts.Any()
                ? string.Join(" • ", parts)
                : "تم التسجيل بدون تفاصيل إضافية";
        }

        /// <summary>
        /// عرض ملخص سجلات النشر الرقمي
        /// </summary>
        private void DisplaySocialRecord()
        {
            if (!_socialLogs.Any())
            {
                PnlSocial.Visibility = Visibility.Collapsed;
                return;
            }

            PnlSocial.Visibility = Visibility.Visible;

            // تجميع ملخص لكل ضيف والمنصات المنشورة له
            var summaries = _socialLogs.Select(log =>
            {
                var platformNames = log.Platforms
                    .Where(p => !string.IsNullOrWhiteSpace(p.Url))
                    .Select(p => p.PlatformName)
                    .ToList();

                var clipInfo = !string.IsNullOrWhiteSpace(log.ClipTitle) ? log.ClipTitle : "بدون عنوان";
                return $"{clipInfo} → {string.Join("، ", platformNames)}";
            });

            TxtSocialDetails.Text = string.Join("\n", summaries);
        }

        /// <summary>
        /// عرض بيانات سجل نشر الموقع الإلكتروني
        /// </summary>
        private void DisplayWebsiteRecord()
        {
            if (_websiteLog is null)
            {
                PnlWebsite.Visibility = Visibility.Collapsed;
                return;
            }

            PnlWebsite.Visibility = Visibility.Visible;

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(_websiteLog.Title))
                parts.Add($"العنوان: {_websiteLog.Title}");
            if (!string.IsNullOrWhiteSpace(_websiteLog.MediaType))
                parts.Add($"النوع: {_websiteLog.MediaType}");
            if (!string.IsNullOrWhiteSpace(_websiteLog.Notes))
                parts.Add($"ملاحظات: {_websiteLog.Notes}");

            TxtWebDetails.Text = parts.Any()
                ? string.Join(" • ", parts)
                : "تم النشر بدون تفاصيل إضافية";
        }

        // ═══════════════════════════════════════════
        //  أزرار التعديل — فتح النوافذ في وضع التعديل
        // ═══════════════════════════════════════════

        /// <summary>
        /// تعديل سجل التنفيذ — يفتح ExecutionLogDialog في وضع التعديل
        /// </summary>
        private async void BtnEditExecution_Click(object sender, RoutedEventArgs e)
        {
            if (_executionLog is null) return;

            var dialog = new ExecutionLogDialog(_episodeId, _executionService, _session, _executionLog)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
                await LoadRecordsAsync();  // إعادة تحميل البيانات بعد التعديل
        }

        /// <summary>
        /// تعديل النشر الرقمي — يفتح PublishingLogDialog في وضع التعديل
        /// يحتاج لاسترجاع قائمة الضيوف أولاً
        /// </summary>
        private async void BtnEditSocial_Click(object sender, RoutedEventArgs e)
        {
            if (!_socialLogs.Any()) return;

            try
            {
                // استرجاع قائمة الضيوف (مطلوبة لـ PublishingLogDialog)
                var episodeService = _serviceProvider.GetRequiredService<IEpisodeService>();
                var guests = await episodeService.GetEpisodeGuestsAsync(_episodeId);

                var dialog = new PublishingLogDialog(_publishingService, _session, _episodeId, guests, _socialLogs)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                    await LoadRecordsAsync();
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"خطأ: {ex.Message}");
            }
        }

        /// <summary>
        /// تعديل نشر الموقع — يفتح WebsitePublishDialog في وضع التعديل
        /// </summary>
        private async void BtnEditWebsite_Click(object sender, RoutedEventArgs e)
        {
            if (_websiteLog is null) return;

            var dialog = new WebsitePublishDialog(_publishingService, _session, _episodeId, _websiteLog)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
                await LoadRecordsAsync();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        private void ColorZone_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove(); 
        }
    }
}
