using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Radio.Views.Publishing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Radio.Views.Episodes
{
    /// <summary>
    /// شاشة عرض سجلات النشر الشامل — تعرض جميع سجلات التنفيذ والنشر الرقمي
    /// ونشر الموقع الإلكتروني مع دعم البحث والفلترة حسب النوع
    /// من هنا يمكن فتح أي سجل للتعديل عبر النقر المزدوج أو زر التعديل
    /// </summary>
    public partial class PublishingRecordsView
    {
        private readonly IPublishingService _publishingService;
        private readonly IExecutionService _executionService;
        private readonly UserSession _session;
        private readonly IServiceProvider _serviceProvider;
        private List<PublishingRecordDto> _allRecords = [];

        /// <summary>
        /// هل اكتمل تحميل العناصر والبيانات؟
        /// يمنع استدعاء RebindRecords أثناء InitializeComponent
        /// حيث أن SelectedIndex="0" يطلق SelectionChanged قبل إنشاء DgRecords
        /// </summary>
        private bool _isDataLoaded;

        public PublishingRecordsView(
            IPublishingService publishingService,
            IExecutionService executionService,
            UserSession session,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _publishingService = publishingService;
            _executionService = executionService;
            _session = session;
            _serviceProvider = serviceProvider;

            Loaded += async (_, _) => await LoadDataAsync();
        }

        // ═══════════════════════════════════════════
        //  تحميل البيانات
        // ═══════════════════════════════════════════

        /// <summary>
        /// تحميل جميع السجلات من الخدمة وعرضها
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                // استرجاع جميع السجلات من جميع الحلقات
                _allRecords = await _publishingService.GetAllPublishingRecordsAsync();

                // الآن أصبحت البيانات جاهزة — نسمح للفلترة بالعمل
                _isDataLoaded = true;

                UpdateStats();
                RebindRecords();
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"خطأ في تحميل السجلات: {ex.Message}");
            }
        }

        private void UpdateStats()
        {
            TxtTotalRecords.Text = _allRecords.Count.ToString();
            TxtTodayRecords.Text = _allRecords.Count(r => r.RecordDate.Date == DateTime.Today).ToString();
            TxtPlatformCoverage.Text = _allRecords.Select(r => r.RecordType).Distinct().Count().ToString();
        }

        /// <summary>
        /// إعادة ربط البيانات مع تطبيق الفلترة والبحث
        /// </summary>
        private void RebindRecords()
        {
            // حماية: أثناء InitializeComponent يُطلق SelectionChanged قبل إنشاء العناصر
            if (!_isDataLoaded) return;

            var keyword = TxtSearch.Text?.Trim();
            var typeFilter = (CmbRecordType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";

            var filtered = _allRecords.AsEnumerable();

            // فلترة حسب النوع
            if (typeFilter != "All")
                filtered = filtered.Where(r => r.RecordType == typeFilter);

            // بحث نصي في الاسم والبرنامج والملخص والمنفّذ
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filtered = filtered.Where(r =>
                    (r.EpisodeName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.ProgramName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Summary?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.RecordedBy?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            DgRecords.ItemsSource = filtered.ToList();
        }

        // ═══════════════════════════════════════════
        //  أحداث البحث والفلترة
        // ═══════════════════════════════════════════

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => RebindRecords();

        private void CmbRecordType_SelectionChanged(object sender, SelectionChangedEventArgs e) => RebindRecords();

        /// <summary>
        /// زر إعادة تحميل السجلات — يُعيد جلب البيانات من قاعدة البيانات
        /// </summary>
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        // ═══════════════════════════════════════════
        //  تعديل السجلات — فتح نوافذ التعديل
        // ═══════════════════════════════════════════

        /// <summary>
        /// النقر المزدوج على سجل — يفتح نافذة التعديل المناسبة
        /// </summary>
        private void DgRecords_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // التحقق من أن النقر على عنصر فعلي وليس على مساحة فارغة
            if (e.OriginalSource is not DependencyObject depObj) return;

            // البحث عن العنصر الأب من نوع ListBoxItem
            var listBoxItem = FindParent<ListBoxItem>(depObj);
            if (listBoxItem is null) return;

            // الحصول على السجل المحدد
            if (listBoxItem.DataContext is PublishingRecordDto record)
            {
                OpenEditDialog(record);
            }
        }

        /// <summary>
        /// زر التعديل في كل بطاقة — يفتح نافذة التعديل المناسبة
        /// </summary>
        private void BtnEditRecord_Click(object sender, RoutedEventArgs e)
        {
            // الحصول على السجل من سياق البيانات للزر
            if (sender is not Button btn) return;
            if (btn.DataContext is not PublishingRecordDto record) return;

            OpenEditDialog(record);
        }

        /// <summary>
        /// فتح نافذة التعديل المناسبة حسب نوع السجل:
        /// - تنفيذ → ExecutionLogDialog
        /// - نشر رقمي → PublishingLogDialog
        /// - نشر موقع → WebsitePublishDialog
        /// بعد الإغلاق بنجاح، يُعاد تحميل البيانات
        /// </summary>
        private async void OpenEditDialog(PublishingRecordDto record)
        {
            var mainWindow = Window.GetWindow(this) as ModernMainWindow;
            if (mainWindow != null) await mainWindow.ShowOverlay();

            try
            {
                var ownerWindow = Window.GetWindow(this);

                switch (record.RecordType)
                {
                    case "Execution":
                        await OpenExecutionEditDialog(record, ownerWindow);
                        break;

                    case "SocialMedia":
                        await OpenSocialEditDialog(record, ownerWindow);
                        break;

                    case "Website":
                        await OpenWebsiteEditDialog(record, ownerWindow);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"خطأ في فتح نافذة التعديل: {ex.Message}");
            }
            finally
            {
                if (mainWindow != null) await mainWindow.HideOverlay();
            }
        }

        /// <summary>
        /// فتح نافذة تعديل سجل التنفيذ
        /// يحمل بيانات السجل من الخدمة ثم يفتح ExecutionLogDialog في وضع التعديل
        /// </summary>
        private async Task OpenExecutionEditDialog(PublishingRecordDto record, Window? ownerWindow)
        {
            // تحميل بيانات سجل التنفيذ الكاملة من الخدمة
            var executionLog = await _executionService.GetExecutionLogAsync(record.EpisodeId);
            if (executionLog is null)
            {
                MessageService.Current.ShowWarning("سجل التنفيذ غير موجود أو تم حذفه.");
                return;
            }

            // فتح النافذة في وضع التعديل
            var dialog = new ExecutionLogDialog(record.EpisodeId, _executionService, _session, executionLog)
            {
                Owner = ownerWindow
            };

            // إذا تم الحفظ بنجاح، نعيد تحميل البيانات
            if (dialog.ShowDialog() == true)
                await LoadDataAsync();
        }

        /// <summary>
        /// فتح نافذة تعديل سجل النشر الرقمي
        /// يحتاج لاسترجاع قائمة الضيوف + سجلات النشر للحلقة ثم يفتح PublishingLogDialog
        /// </summary>
        private async Task OpenSocialEditDialog(PublishingRecordDto record, Window? ownerWindow)
        {
            // استرجاع قائمة الضيوف (مطلوبة لـ PublishingLogDialog)
            var episodeService = _serviceProvider.GetRequiredService<IEpisodeService>();
            var guests = await episodeService.GetEpisodeGuestsAsync(record.EpisodeId);

            // استرجاع سجلات النشر الرقمي للحلقة
            var socialLogs = await _publishingService.GetEpisodeSocialLogsAsync(record.EpisodeId);

            if (!socialLogs.Any())
            {
                MessageService.Current.ShowWarning("سجلات النشر الرقمي غير موجودة أو تم حذفها.");
                return;
            }

            // فتح النافذة في وضع التعديل مع تمرير السجلات الموجودة
            var dialog = new PublishingLogDialog(_publishingService, _session, record.EpisodeId, guests, socialLogs)
            {
                Owner = ownerWindow
            };

            if (dialog.ShowDialog() == true)
                await LoadDataAsync();
        }

        /// <summary>
        /// فتح نافذة تعديل سجل نشر الموقع الإلكتروني
        /// يحمل بيانات السجل من الخدمة ثم يفتح WebsitePublishDialog في وضع التعديل
        /// </summary>
        private async Task OpenWebsiteEditDialog(PublishingRecordDto record, Window? ownerWindow)
        {
            // تحميل بيانات سجل نشر الموقع من الخدمة
            var websiteLog = await _publishingService.GetWebsitePublishingLogAsync(record.EpisodeId);
            if (websiteLog is null)
            {
                MessageService.Current.ShowWarning("سجل نشر الموقع غير موجود أو تم حذفه.");
                return;
            }

            // فتح النافذة في وضع التعديل
            var dialog = new WebsitePublishDialog(_publishingService, _session, record.EpisodeId, websiteLog)
            {
                Owner = ownerWindow
            };

            if (dialog.ShowDialog() == true)
                await LoadDataAsync();
        }

        // ═══════════════════════════════════════════
        //  أدوات مساعدة
        // ═══════════════════════════════════════════

        /// <summary>
        /// البحث عن العنصر الأب من نوع محدد في الشجرة البصرية
        /// يُستخدم لتحديد ListBoxItem عند النقر المزدوج
        /// </summary>
        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);

            while (parent is not null)
            {
                if (parent is T result)
                    return result;

                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }

            return null;
        }
    }
}
