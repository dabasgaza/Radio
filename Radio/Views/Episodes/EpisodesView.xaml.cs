using BroadcastWorkflow.Services;
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
    /// Interaction logic for EpisodesView.xaml
    /// </summary>
    public partial class EpisodesView : UserControl
    {
        private readonly IEpisodeService _episodeService;
        private readonly IGuestService _guestService;
        private readonly IProgramService _programService;
        private readonly UserSession _session;
        private readonly IServiceProvider _serviceProvider;
        public Visibility ShowExecutedBtn { get; set; }
        public Visibility ShowPublishedBtn { get; set; }

        // Logic for button visibility inside DataGrid based on Role & Status
        public EpisodesView(IEpisodeService epService,  
            IProgramService progService, UserSession session,
            IServiceProvider serviceProvider, IGuestService guestService)
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
            _guestService = guestService;
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // إظهار مؤشر تحميل إذا أردت (Busy Indicator)
                var data = await _episodeService.GetActiveEpisodesAsync();
                DgEpisodes.ItemsSource = data;
            }
            catch (Exception ex)
            {
                // ✅ السحر هنا: عند اصطياد الخطأ وعرضه، يصبح "Observed" ويختفي الخطأ الخلفي
                MessageService.Current.ShowError($"فشل تحميل الحلقات: {ex.Message}");
            }
        }

        private async void BtnMarkExecuted_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep && ep.StatusText == "مجدولة")
            {
                var execService = _serviceProvider.GetRequiredService<IExecutionService>();
                var dialog = new ExecutionLogDialog(ep.EpisodeId, execService, _session);

                if (dialog.ShowDialog() == true)
                    await LoadDataAsync();
            }

        }

        private async void BtnMarkPublished_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep && ep.StatusText == "منفّذة")
            {
                var pubService = _serviceProvider.GetRequiredService<IPublishingService>();
                var dialog = new PublishingLogDialog(ep.EpisodeId, pubService, _session);
                if (dialog.ShowDialog() == true) await LoadDataAsync();
            }
        }

        private async void BtnAddEpisode_Click(object sender, RoutedEventArgs e)
        {
            var dialog = 
                new EpisodeFormDialog(_episodeService, _programService, _guestService, _session,null);

            if (dialog.ShowDialog() == true) await LoadDataAsync();
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto selectedEpisode)
            {
                var epService = _serviceProvider.GetRequiredService<IEpisodeService>();
                var progService = _serviceProvider.GetRequiredService<IProgramService>();
                var guestService = _serviceProvider.GetRequiredService<IGuestService>();

                // نمرر selectedEpisode كبارامتر أخير (وهو الـ _existingEpisode)
                var dialog = new EpisodeFormDialog(epService, progService, guestService, _session, selectedEpisode);

                if (dialog.ShowDialog() == true)
                {
                    await LoadDataAsync(); // تحديث الجدول بعد التعديل
                }
            }

        }
    }
}
