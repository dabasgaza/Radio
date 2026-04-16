using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Reports
{
    /// <summary>
    /// Interaction logic for ReportsView.xaml
    /// </summary>
    /// 

    [SupportedOSPlatform("windows")] // 👈 أضف هذا السطر فوق الكلاس أو الميثود
    public partial class ReportsView : UserControl
    {
        private readonly IReportsService _reportsService;

        public ReportsView(IReportsService reportsService)
        {
            InitializeComponent();
            _reportsService = reportsService;
            _ = LoadDashboardDataAsync();
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                // 1. تحميل الإحصائيات العلوية
                var stats = await _reportsService.GetEpisodeStatusStatsAsync();

                // نستخدم المسميات البرمجية (Planned, Executed, Published) كمفاتيح
                TxtPlannedCount.Text = stats.GetValueOrDefault("Planned", 0).ToString();
                TxtExecutedCount.Text = stats.GetValueOrDefault("Executed", 0).ToString();
                TxtPublishedCount.Text = stats.GetValueOrDefault("Published", 0).ToString();

                // 2. تحميل جدول اليوم
                DgToday.ItemsSource = await _reportsService.GetTodayEpisodesAsync();

                // 3. تحميل أداء البرامج
                DgProgramStats.ItemsSource = await _reportsService.GetMostActiveProgramsAsync();
            }
            catch (Exception ex)
            {
                // استخدام نظام الرسائل المركزي الجديد
                MessageService.Current.ShowError($"فشل تحديث لوحة التقارير: {ex.Message}");
            }
        }

    }
}
