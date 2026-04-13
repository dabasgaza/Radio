using DataAccess.Services;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Reports
{
    /// <summary>
    /// Interaction logic for ReportsView.xaml
    /// </summary>
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
                var stats = await _reportsService.GetEpisodeStatusStatsAsync();
                TxtPlannedCount.Text = stats["Planned"].ToString();
                TxtExecutedCount.Text = stats["Executed"].ToString();
                TxtPublishedCount.Text = stats["Published"].ToString();

                // ستعمل الآن لأنها تجلب DTO بدلاً من محاولة الوصول لـ DbSet غير موجود
                DgToday.ItemsSource = await _reportsService.GetTodayEpisodesAsync();
                DgTopGuests.ItemsSource = await _reportsService.GetTopGuestsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل التقارير: " + ex.Message);
            }
        }


    }
}
