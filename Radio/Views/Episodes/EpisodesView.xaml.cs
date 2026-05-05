using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Radio.Views.Common;
using System.Windows;
using System.Windows.Controls;
using Radio.Views.Publishing;

namespace Radio.Views.Episodes
{
    public partial class EpisodesView : UserControl
    {
        private readonly IEpisodeService _episodeService;
        private readonly IProgramService _programService;
        private readonly IGuestService _guestService;
        private readonly ICorrespondentService _correspondentService;
        private readonly IEmployeeService _employeeService;
        private readonly UserSession _session;
        private readonly IServiceProvider _serviceProvider;
        private List<ActiveEpisodeDto> _allEpisodes = [];

        public EpisodesView(
            IEpisodeService epService,
            IProgramService progService,
            UserSession session,
            IServiceProvider serviceProvider,
            IGuestService guestService,
            ICorrespondentService correspondentService,
            IEmployeeService employeeService)
        {
            InitializeComponent();

            _episodeService = epService;
            _programService = progService;
            _session = session;
            _serviceProvider = serviceProvider;
            _guestService = guestService;
            _correspondentService = correspondentService;
            _employeeService = employeeService;

            BtnAddEpisode.Visibility = _session.HasPermission(AppPermissions.EpisodeManage)
                ? Visibility.Visible
                : Visibility.Collapsed;

            Loaded += async (_, _) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _allEpisodes = (await _episodeService.GetActiveEpisodesAsync()).ToList();
                RebindAndUpdateStats();
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError("حدث خطأ أثناء تحميل الحلقات: " + ex.Message);
            }
        }

        private void RebindAndUpdateStats()
        {
            var keyword = TxtSearch.Text?.Trim();
            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allEpisodes
                : _allEpisodes.Where(ep =>
                    (ep.EpisodeName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (ep.ProgramName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

            DgEpisodes.ItemsSource = filtered;
            UpdateStatistics(filtered);
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => RebindAndUpdateStats();

        private async void BtnAddEpisode_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EpisodeFormControl(_episodeService, _programService, _guestService, _correspondentService, _employeeService, _session)
            {
                Owner = Window.GetWindow(this)
            };
            if (dialog.ShowDialog() == true) await LoadDataAsync();
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep)
            {
                var dialog = new EpisodeFormControl(_episodeService, _programService, _guestService, _correspondentService, _employeeService, _session, ep.EpisodeId)
                {
                    Owner = Window.GetWindow(this)
                };
                if (dialog.ShowDialog() == true) await LoadDataAsync();
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto selectedEpisode)
            {
                if (await MessageService.Current.ShowConfirmationAsync($"حذف {selectedEpisode.EpisodeName}؟", "تأكيد"))
                {
                    var res = await _episodeService.DeleteEpisodeAsync(selectedEpisode.EpisodeId, _session);
                    if (res.IsSuccess) await LoadDataAsync();
                }
            }
        }

        private async void BtnMarkExecuted_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep)
            {
                var execService = _serviceProvider.GetRequiredService<IExecutionService>();
                var dialog = new ExecutionLogDialog(ep.EpisodeId, execService, _session)
                {
                    Owner = Window.GetWindow(this)
                };
                if (dialog.ShowDialog() == true) await LoadDataAsync();
            }
        }

        private async void BtnMarkPublished_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep)
            {
                var pubService = _serviceProvider.GetRequiredService<IPublishingService>();
                var guests = await _episodeService.GetEpisodeGuestsAsync(ep.EpisodeId);
                var dialog = new PublishingLogDialog(pubService, _session, ep.EpisodeId, guests)
                {
                    Owner = Window.GetWindow(this)
                };
                if (dialog.ShowDialog() == true) await LoadDataAsync();
            }
        }

        private async void BtnToggleWebsitePublish_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep)
            {
                var publishingService = _serviceProvider.GetRequiredService<IPublishingService>();
                var dialog = new WebsitePublishDialog(publishingService, _session, ep.EpisodeId)
                {
                    Owner = Window.GetWindow(this)
                };
                if (dialog.ShowDialog() == true) await LoadDataAsync();
            }
        }

        private async void BtnRevert_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep)
            {
                var reasonDialog = new ReasonInputDialog("تراجع", "السبب:")
                {
                    Owner = Window.GetWindow(this)
                };
                if (reasonDialog.ShowDialog() == true)
                {
                    var res = await _episodeService.RevertEpisodeStatusAsync(ep.EpisodeId, reasonDialog.Reason!, _session);
                    if (res.IsSuccess) await LoadDataAsync();
                }
            }
        }

        private async void BtnCancelEpisode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep)
            {
                var reasonDialog = new ReasonInputDialog("إلغاء", "السبب:")
                {
                    Owner = Window.GetWindow(this)
                };
                if (reasonDialog.ShowDialog() == true)
                {
                    var res = await _episodeService.CancelEpisodeAsync(ep.EpisodeId, reasonDialog.Reason!, _session);
                    if (res.IsSuccess) await LoadDataAsync();
                }
            }
        }

        private async void BtnEditCancellationReason_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto ep)
            {
                var reasonDialog = new ReasonInputDialog("تعديل سبب الإلغاء", "السبب:", ep.CancellationReason)
                {
                    Owner = Window.GetWindow(this)
                };
                if (reasonDialog.ShowDialog() == true)
                {
                    var res = await _episodeService.UpdateCancellationReasonAsync(ep.EpisodeId, reasonDialog.Reason!, _session);
                    if (res.IsSuccess) await LoadDataAsync();
                }
            }
        }

        private void UpdateStatistics(List<ActiveEpisodeDto> data)
        {
            TxtTotal.Text = data.Count.ToString();
            TxtExecuted.Text = data.Count(e => e.StatusId == EpisodeStatus.Executed).ToString();
            TxtPublished.Text = data.Count(e => e.StatusId == EpisodeStatus.Published || e.StatusId == EpisodeStatus.WebsitePublished).ToString();
        }
    }
}
