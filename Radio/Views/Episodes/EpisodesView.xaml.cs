using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Radio.Views.Common;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Episodes
{
    public partial class EpisodesView : UserControl
    {
        private readonly IEpisodeService _episodeService;
        private readonly IProgramService _programService;
        private readonly IGuestService _guestService;
        private readonly UserSession _session;
        private readonly IServiceProvider _serviceProvider;
        private List<ActiveEpisodeDto> _allEpisodes = [];

        public EpisodesView(
            IEpisodeService epService,
            IProgramService progService,
            UserSession session,
            IServiceProvider serviceProvider,
            IGuestService guestService)
        {
            InitializeComponent();

            _episodeService = epService;
            _programService = progService;
            _session = session;
            _serviceProvider = serviceProvider;
            _guestService = guestService;

            BtnAddEpisode.Visibility = _session.HasPermission(AppPermissions.EpisodeManage)
                ? Visibility.Visible
                : Visibility.Collapsed;

            Loaded += async (_, _) => await LoadDataAsync();
        }

        #region Data Loading

        private async Task LoadDataAsync()
        {
            try
            {
                _allEpisodes = (await _episodeService.GetActiveEpisodesAsync()).ToList();
                RebindAndUpdateStats();
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تحميل الحلقات.");
            }
        }

        #endregion

        #region Search

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || _allEpisodes.Count == 0)
                return;

            string keyword = textBox.Text.Trim();

            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allEpisodes
                : _allEpisodes.Where(ep =>
                    (ep.EpisodeName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (ep.ProgramName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (ep.GuestsDisplay?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    ep.GuestItems.Any(g =>
                        (g.Name?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (g.Topic?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)));

            DgEpisodes.ItemsSource = filtered.ToList();
        }

        #endregion

        #region Memory Cache Helpers

        private void RebindAndUpdateStats()
        {
            var keyword = TxtSearch.Text?.Trim();

            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allEpisodes
                : _allEpisodes.Where(ep =>
                    (ep.EpisodeName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (ep.ProgramName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (ep.GuestsDisplay?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    ep.GuestItems.Any(g =>
                        (g.Name?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (g.Topic?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))).ToList();

            DgEpisodes.ItemsSource = filtered.ToList();
            UpdateStatistics(filtered);
        }

        private static List<GuestDisplayItem> BuildGuestItems(List<GuestRow> guestRows)
        {
            return guestRows
                .Select(g => new GuestDisplayItem(g.GuestName, g.Topic, g.HostingTime))
                .ToList();
        }

        private void InsertSorted(EpisodeDto dto, string programName, int newId, List<GuestRow> guestRows)
        {
            var newDto = new ActiveEpisodeDto
            {
                EpisodeId = newId,
                ProgramId = dto.ProgramId,
                EpisodeName = dto.EpisodeName,
                ProgramName = programName,
                ScheduledExecutionTime = dto.ScheduledTime,
                StatusId = EpisodeStatus.Planned,
                StatusText = "مجدولة",
                SpecialNotes = dto.SpecialNotes,
                GuestItems = BuildGuestItems(guestRows),
                IsWebsitePublished = false
            };

            _allEpisodes.Add(newDto);
            _allEpisodes = _allEpisodes.OrderBy(e => e.ScheduledExecutionTime).ToList();
            RebindAndUpdateStats();
        }

        private void UpdateInCache(EpisodeDto dto, string programName, int episodeId, List<GuestRow> guestRows)
        {
            var idx = _allEpisodes.FindIndex(e => e.EpisodeId == episodeId);
            if (idx < 0) return;

            var existing = _allEpisodes[idx];

            _allEpisodes[idx] = existing with
            {
                ProgramId = dto.ProgramId,
                ProgramName = programName,
                EpisodeName = dto.EpisodeName,
                ScheduledExecutionTime = dto.ScheduledTime,
                SpecialNotes = dto.SpecialNotes,
                GuestItems = BuildGuestItems(guestRows)
            };

            _allEpisodes = _allEpisodes.OrderBy(e => e.ScheduledExecutionTime).ToList();
            RebindAndUpdateStats();
        }

        private void RemoveFromCache(int episodeId)
        {
            var idx = _allEpisodes.FindIndex(e => e.EpisodeId == episodeId);
            if (idx >= 0)
                _allEpisodes.RemoveAt(idx);

            RebindAndUpdateStats();
        }

        private void UpdateStatusInCache(int episodeId, byte newStatusId, string newStatusText,
            bool? isWebsitePublished = null, string? cancellationReason = null)
        {
            var idx = _allEpisodes.FindIndex(e => e.EpisodeId == episodeId);
            if (idx < 0) return;

            var existing = _allEpisodes[idx];

            var updated = existing with
            {
                StatusId = newStatusId,
                StatusText = newStatusText
            };

            if (isWebsitePublished.HasValue)
                updated = updated with { IsWebsitePublished = isWebsitePublished.Value };

            if (cancellationReason is not null)
                updated.CancellationReason = cancellationReason;

            _allEpisodes[idx] = updated;
            RebindAndUpdateStats();
        }

        #endregion

        #region CRUD Operations

        private void BtnAddEpisode_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EpisodeFormDialog(
                _episodeService, _programService, _guestService, _session);

            if (dialog.ShowDialog() == true && dialog.SubmittedDto is not null)
            {
                InsertSorted(dialog.SubmittedDto, dialog.SubmittedProgramName,
                    dialog.CreatedEpisodeId, dialog.GuestList.ToList());
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto selectedEpisode)
                return;

            var dialog = new EpisodeFormDialog(
                _episodeService, _programService, _guestService,
                _session, selectedEpisode);

            if (dialog.ShowDialog() == true && dialog.SubmittedDto is not null)
            {
                UpdateInCache(dialog.SubmittedDto, dialog.SubmittedProgramName,
                    selectedEpisode.EpisodeId, dialog.GuestList.ToList());
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto selectedEpisode)
                return;

            if (selectedEpisode.StatusId is EpisodeStatus.Executed or EpisodeStatus.Published or EpisodeStatus.WebsitePublished)
            {
                MessageService.Current.ShowWarning(
                    "لا يمكن حذف حلقة تم تنفيذها أو نشرها، يُرجى إلغائها أولاً.");
                return;
            }

            bool isConfirmed = await MessageService.Current.ShowConfirmationAsync(
                $"هل أنت متأكد من حذف الحلقة: {selectedEpisode.EpisodeName}؟",
                "تأكيد الحذف");

            if (!isConfirmed)
                return;

            try
            {
                var result = await _episodeService.DeleteEpisodeAsync(selectedEpisode.EpisodeId, _session);
                
                if (result.IsSuccess)
                {
                    RemoveFromCache(selectedEpisode.EpisodeId);
                    MessageService.Current.ShowSuccess($"تم حذف الحلقة «{selectedEpisode.EpisodeName}» بنجاح.");
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت عملية الحذف.");
                }
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حذف الحلقة.");
            }
        }

        #endregion

        #region Execution & Publishing

        private void BtnMarkExecuted_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto ep)
                return;

            if (ep.StatusId != EpisodeStatus.Planned)
                return;

            var execService = _serviceProvider.GetRequiredService<IExecutionService>();
            var dialog = new ExecutionLogDialog(ep.EpisodeId, execService, _session);

            if (dialog.ShowDialog() == true)
                UpdateStatusInCache(ep.EpisodeId, EpisodeStatus.Executed, "منفّذة");
        }

        private void BtnMarkPublished_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto ep)
                return;

            if (ep.StatusId != EpisodeStatus.Executed)
                return;

            var pubService = _serviceProvider.GetRequiredService<IPublishingService>();
            var dialog = new PublishingLogDialog(ep.EpisodeId, pubService, _session);

            if (dialog.ShowDialog() == true)
                UpdateStatusInCache(ep.EpisodeId, EpisodeStatus.Published, "منشورة رقمياً");
        }

        /// <summary>
        /// تبديل حالة النشر على الموقع الإلكتروني
        /// </summary>
        private async void BtnToggleWebsitePublish_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto ep)
                return;

            try
            {
                var isCurrentlyPublished = ep.StatusId == EpisodeStatus.WebsitePublished;
                var result = await _episodeService.ToggleWebsitePublishAsync(ep.EpisodeId, !isCurrentlyPublished, _session);

                if (result.IsSuccess)
                {
                    UpdateStatusInCache(ep.EpisodeId,
                        !isCurrentlyPublished ? EpisodeStatus.WebsitePublished : EpisodeStatus.Published,
                        !isCurrentlyPublished ? "منشورة على الموقع" : "منشورة رقمياً",
                        isWebsitePublished: !isCurrentlyPublished);

                    MessageService.Current.ShowSuccess(
                        !isCurrentlyPublished
                            ? "تم نشر الحلقة على الموقع بنجاح."
                            : "تم إلغاء نشر الحلقة من الموقع.");
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت العملية");
                }
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تغيير حالة النشر.");
            }
        }

        /// <summary>
        /// التراجع عن تنفيذ أو نشر حلقة بالخطأ (يتطلب سبب)
        /// </summary>
        private async void BtnRevert_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto ep)
                return;

            if (ep.StatusId is not (EpisodeStatus.Executed or EpisodeStatus.Published or EpisodeStatus.WebsitePublished))
                return;

            var reasonDialog = new ReasonInputDialog("تأكيد التراجع", "يرجى إدخال سبب التراجع عن الحالة:");
            reasonDialog.Owner = Window.GetWindow(this);
            if (reasonDialog.ShowDialog() != true)
                return;

            try
            {
                var result = await _episodeService.RevertEpisodeStatusAsync(ep.EpisodeId, reasonDialog.Reason!, _session);

                if (result.IsSuccess)
                {
                    var (newStatus, newText, isWebPub) = ep.StatusId switch
                    {
                        EpisodeStatus.Executed => (EpisodeStatus.Planned, "مجدولة", (bool?)null),
                        EpisodeStatus.Published => (EpisodeStatus.Executed, "منفّذة", (bool?)null),
                        EpisodeStatus.WebsitePublished => (EpisodeStatus.Published, "منشورة رقمياً", (bool?)false),
                        _ => ((byte)0, "", (bool?)null)
                    };

                    UpdateStatusInCache(ep.EpisodeId, newStatus, newText, isWebsitePublished: isWebPub);
                    MessageService.Current.ShowSuccess($"تم التراجع عن حالة الحلقة «{ep.EpisodeName}» بنجاح.");
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت عملية التراجع.");
                }
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء التراجع عن الحالة.");
            }
        }

        /// <summary>
        /// إلغاء حلقة مع إدخال سبب الإلغاء
        /// </summary>
        private async void BtnCancelEpisode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto ep)
                return;

            if (ep.StatusId is not (EpisodeStatus.Planned or EpisodeStatus.Executed))
                return;

            var reasonDialog = new ReasonInputDialog("تأكيد الإلغاء", "يرجى إدخال سبب إلغاء الحلقة:");
            reasonDialog.Owner = Window.GetWindow(this);
            if (reasonDialog.ShowDialog() != true)
                return;

            try
            {
                var result = await _episodeService.CancelEpisodeAsync(ep.EpisodeId, reasonDialog.Reason!, _session);

                if (result.IsSuccess)
                {
                    UpdateStatusInCache(ep.EpisodeId, EpisodeStatus.Cancelled, "ملغاة",
                        cancellationReason: reasonDialog.Reason);
                    MessageService.Current.ShowSuccess($"تم إلغاء الحلقة «{ep.EpisodeName}» بنجاح.");
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت عملية الإلغاء.");
                }
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء إلغاء الحلقة.");
            }
        }

        /// <summary>
        /// تعديل سبب إلغاء حلقة
        /// </summary>
        private async void BtnEditCancellationReason_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto ep)
                return;

            if (ep.StatusId != EpisodeStatus.Cancelled)
                return;

            var reasonDialog = new ReasonInputDialog("تعديل سبب الإلغاء", "عدّل سبب إلغاء الحلقة:");
            reasonDialog.Owner = Window.GetWindow(this);
            if (reasonDialog.ShowDialog() != true)
                return;

            try
            {
                var result = await _episodeService.UpdateCancellationReasonAsync(ep.EpisodeId, reasonDialog.Reason!, _session);

                if (result.IsSuccess)
                {
                    UpdateStatusInCache(ep.EpisodeId, ep.StatusId, ep.StatusText!,
                        cancellationReason: reasonDialog.Reason);
                    MessageService.Current.ShowSuccess("تم تعديل سبب الإلغاء بنجاح.");
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشل تعديل سبب الإلغاء.");
                }
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تعديل سبب الإلغاء.");
            }
        }

        #endregion

        #region Statistics

        private void UpdateStatistics(List<ActiveEpisodeDto> data)
        {
            if (data == null || data.Count == 0)
            {
                TxtTotal.Text = "0";
                TxtExecuted.Text = "0";
                TxtPublished.Text = "0";
                return;
            }

            TxtTotal.Text = data.Count.ToString();
            TxtExecuted.Text = data.Count(e => e.StatusId == EpisodeStatus.Executed).ToString();
            TxtPublished.Text = data.Count(e => e.StatusId == EpisodeStatus.Published ||
                                              e.StatusId == EpisodeStatus.WebsitePublished).ToString();
        }

        #endregion
    }
}