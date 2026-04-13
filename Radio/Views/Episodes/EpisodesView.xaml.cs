using BroadcastWorkflow.Services;
using DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using static Domain.Models.BroadcastWorkflowDBContext;

namespace Radio.Views.Episodes
{
    /// <summary>
    /// Interaction logic for EpisodesView.xaml
    /// </summary>
    public partial class EpisodesView : UserControl
    {
        private readonly IEpisodeService _episodeService;
        private readonly IProgramService _programService;
        private readonly UserSession _session;
        private readonly IServiceProvider _serviceProvider;
        public Visibility ShowExecutedBtn { get; set; }
        public Visibility ShowPublishedBtn { get; set; }

        // Logic for button visibility inside DataGrid based on Role & Status
        public EpisodesView(IEpisodeService epService, IProgramService progService, UserSession session, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _episodeService = epService;
            _programService = progService;
            _session = session;
            _serviceProvider = serviceProvider;

            // 1. صلاحية إضافة حلقة جديدة (للتنسيق والأدمن فقط)
            BtnAddEpisode.Visibility = _session.HasPermission("EPISODE_MANAGE") ? Visibility.Visible : Visibility.Collapsed;

            // 2. صلاحية أزرار التنفيذ (للإنتاج والأدمن فقط)
            this.ShowExecutedBtn = _session.HasPermission("EPISODE_EXECUTE") ? Visibility.Visible : Visibility.Collapsed;

            // 3. صلاحية أزرار النشر (للنشر الرقمي والأدمن فقط)
            this.ShowPublishedBtn = _session.HasPermission("EPISODE_PUBLISH") ? Visibility.Visible : Visibility.Collapsed;

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            DgEpisodes.ItemsSource = await _episodeService.GetActiveEpisodesAsync();
        }

        private async void BtnMarkExecuted_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeView ep && ep.StatusText == "Planned")
            {
                var execService = _serviceProvider.GetRequiredService<IExecutionService>();
                var dialog = new ExecutionLogDialog(ep.EpisodeId, execService, _session);
                if (dialog.ShowDialog() == true) await LoadDataAsync();
            }
        }

        private async void BtnMarkPublished_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeView ep && ep.StatusText == "Executed")
            {
                var pubService = _serviceProvider.GetRequiredService<IPublishingService>();
                var dialog = new PublishingLogDialog(ep.EpisodeId, pubService, _session);
                if (dialog.ShowDialog() == true) await LoadDataAsync();
            }
        }

        private async void BtnAddEpisode_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EpisodeFormDialog(_episodeService, _programService, _session);
            if (dialog.ShowDialog() == true) await LoadDataAsync();
        }

    }
}
