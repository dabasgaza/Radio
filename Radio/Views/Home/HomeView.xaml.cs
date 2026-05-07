using DataAccess.Services;
using System.Windows.Controls;

namespace Radio.Views.Home
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        private readonly IReportsService _reportsService;

        public HomeView(IReportsService reportsService)
        {
            _reportsService = reportsService;
            InitializeComponent();
            Loaded += HomeView_Loaded;
        }

        public HomeView() // Design-time constructor
        {
            InitializeComponent();
        }

        private async void HomeView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var todayEpisodes = await _reportsService.GetTodayEpisodesAsync();
                var activePrograms = await _reportsService.GetMostActiveProgramsAsync();
                
                // Bind data
                DgTodayEpisodes.ItemsSource = todayEpisodes;
                IcActivePrograms.ItemsSource = activePrograms;
                
                // Summary Counts
                TxtTodayCount.Text = todayEpisodes.Count.ToString();
                TxtGuestCount.Text = todayEpisodes.Sum(ep => string.IsNullOrEmpty(ep.GuestsDisplay) || ep.GuestsDisplay == "لا يوجد ضيف" ? 0 : ep.GuestsDisplay.Split('،').Length).ToString();
            }
            catch (Exception)
            {
                // Silently fail for dashboard stats in this context
            }
        }
    }
}
