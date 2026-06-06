using DataAccess.Common;
using DataAccess.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Radio.Views.Home
{
    public partial class HomeView : UserControl
    {
        private readonly IReportsService _reportsService;
        private readonly UserSession _session;
        private readonly DispatcherTimer _clockTimer;

        public HomeView(IReportsService reportsService, CurrentSessionProvider sessionProvider)
        {
            _reportsService = reportsService;
            _session = sessionProvider.CurrentSession!;
            InitializeComponent();

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += UpdateClock;
            _clockTimer.Start();

            UpdateGreeting();

            Loaded += HomeView_Loaded;
            Unloaded += (_, _) => _clockTimer.Stop();
        }

        public HomeView()
        {
            InitializeComponent();
            _reportsService = null!;
            _session = null!;
            _clockTimer = null!;
        }

        private void UpdateClock(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            TxtClock.Text = now.ToString("hh:mm");
            var arCulture = (CultureInfo)new CultureInfo("ar-SA").Clone();
            arCulture.DateTimeFormat.Calendar = new GregorianCalendar();
            TxtDate.Text = now.ToString("dddd، d MMMM yyyy", arCulture);
        }

        private void UpdateGreeting()
        {
            var hour = DateTime.Now.Hour;
            string greeting = hour switch
            {
                < 12 => "صباح الخير",
                < 18 => "مساء الخير",
                _ => "مساء الخير"
            };
            TxtGreeting.Text = $"{greeting}، {_session.FullName}";
        }

        private async void HomeView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var todayEpisodes = await _reportsService.GetTodayEpisodesAsync();
                var activePrograms = await _reportsService.GetMostActiveProgramsAsync();

                DgTodayEpisodes.ItemsSource = todayEpisodes;
                IcActivePrograms.ItemsSource = activePrograms;

                TxtTodayCount.Text = todayEpisodes.Count.ToString();
                TxtGuestCount.Text = todayEpisodes
                    .Sum(ep => string.IsNullOrEmpty(ep.GuestsDisplay) || ep.GuestsDisplay == "لا يوجد ضيف"
                        ? 0 : ep.GuestsDisplay.Split('،').Length)
                    .ToString();
                TxtEpisodeCountBadge.Text = todayEpisodes.Count.ToString();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to load dashboard data");
            }
        }
    }
}
