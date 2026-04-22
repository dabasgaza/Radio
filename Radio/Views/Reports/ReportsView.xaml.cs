using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Windows.Controls;

namespace Radio.Views.Reports
{
    /// <summary>
    /// لوحة التقارير — تعرض إحصائيات الحلقات وجدول اليوم وأداء البرامج.
    /// </summary>
    public partial class ReportsView : UserControl
    {
        private readonly IReportsService _reportsService;

        public ReportsView(IReportsService reportsService)
        {
            InitializeComponent();
            _reportsService = reportsService;

            // ✅ Loaded بدلاً من Fire-and-Forget
            Loaded += async (_, _) => await LoadDashboardDataAsync();
        }

        #region Data Loading

        /// <summary>
        /// تحميل بيانات لوحة التقارير: الإحصائيات + جدول اليوم + أداء البرامج.
        /// </summary>
        private async Task LoadDashboardDataAsync()
        {
            try
            {
                var stats = await _reportsService.GetEpisodeStatusStatsAsync();

                TxtPlannedCount.Text = stats.GetValueOrDefault("Planned", 0).ToString();
                TxtExecutedCount.Text = stats.GetValueOrDefault("Executed", 0).ToString();
                TxtPublishedCount.Text = stats.GetValueOrDefault("Published", 0).ToString();

                DgToday.ItemsSource = await _reportsService.GetTodayEpisodesAsync();

                DgProgramStats.ItemsSource = await _reportsService.GetMostActiveProgramsAsync();
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError("ليس لديك صلاحية لعرض التقارير.");
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تحميل لوحة التقارير.");
            }
        }

        #endregion
    }
}