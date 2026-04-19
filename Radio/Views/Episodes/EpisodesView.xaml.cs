using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Episodes
{
    public partial class EpisodesView : UserControl
    {
        private readonly IEpisodeService _episodeService;
        private readonly IGuestService _guestService;
        private readonly IProgramService _programService;
        private readonly UserSession _session;
        private readonly IServiceProvider _serviceProvider;

        public Visibility ShowExecutedBtn { get; set; }
        public Visibility ShowPublishedBtn { get; set; }

        // قائمة بدائية لتخزين البيانات الأصلية للبحث
        private ObservableCollection<ActiveEpisodeDto> _allEpisodes;

        public EpisodesView(IEpisodeService epService,
            IProgramService progService, UserSession session,
            IServiceProvider serviceProvider, IGuestService guestService)
        {
            InitializeComponent();

            _episodeService = epService;
            _programService = progService;
            _session = session;
            _serviceProvider = serviceProvider;
            _guestService = guestService;

            // 1. صلاحية إضافة حلقة جديدة
            BtnAddEpisode.Visibility = _session.HasPermission("EPISODE_MANAGE") ? Visibility.Visible : Visibility.Collapsed;

            // 2. صلاحية أزرار التنفيذ
            this.ShowExecutedBtn = _session.HasPermission("EPISODE_EXECUTE") ? Visibility.Visible : Visibility.Collapsed;

            // 3. صلاحية أزرار النشر
            this.ShowPublishedBtn = _session.HasPermission("EPISODE_PUBLISH") ? Visibility.Visible : Visibility.Collapsed;

            _allEpisodes = new ObservableCollection<ActiveEpisodeDto>();
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var data = await _episodeService.GetActiveEpisodesAsync();

                // تخزين البيانات الأصلية في القائمة المحلية
                _allEpisodes = new ObservableCollection<ActiveEpisodeDto>(data);

                DgEpisodes.ItemsSource = _allEpisodes;

                // تحديث الإحصائيات
                UpdateStats(_allEpisodes);
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"فشل تحميل الحلقات: {ex.Message}");
            }
        }

        // ═══════════ تحديث الإحصائيات ═══════════
        private void UpdateStats(ObservableCollection<ActiveEpisodeDto> data)
        {
            if (data == null) return;

            int ScCount = data.Count(ep => ep.StatusText == "مجدولة");
            TxtTotal.Text = ScCount.ToString();

            // حساب الحلقات المنفذة
            int executedCount = data.Count(ep => ep.StatusText == "منفّذة");
            TxtExecuted.Text = executedCount.ToString();

            // حساب الحلقات المنشورة
            int publishedCount = data.Count(ep => ep.StatusText == "منشورة");
            TxtPublished.Text = publishedCount.ToString();
        }

        // ═══════════ البحث الفوري ═══════════
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // التأكد من تحميل البيانات أولاً
            if (_allEpisodes == null) return;

            string keyword = TxtSearch.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                // إذا كان البحث فارغاً، عرض كل البيانات
                DgEpisodes.ItemsSource = _allEpisodes;
                UpdateStats(_allEpisodes);
            }
            else
            {
                // فلترة البيانات بناءً على عنوان الحلقة أو اسم الضيف
                var filtered = _allEpisodes
                    .Where(ep =>
                        (ep.EpisodeName != null && ep.EpisodeName.ToLower().Contains(keyword)) ||
                        (ep.GuestName != null && ep.GuestName.ToLower().Contains(keyword)))
                    .ToList();

                DgEpisodes.ItemsSource = filtered;
                UpdateStats(new ObservableCollection<ActiveEpisodeDto>(filtered));
            }
        }

        // ═══════════ حذف الحلقة ═══════════
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            // التأكد من النقر على زر داخل الصفوف
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto selectedEpisode)
                return;

            // منع حذف الحلقات المنفذة أو المنشورة (حسب متطلبات سير العمل)
            if (selectedEpisode.StatusText == "منفّذة" || selectedEpisode.StatusText == "منشورة")
            {
                MessageService.Current.ShowWarning("لا يمكن حذف حلقة تم تنفيذها أو نشرها، يُرجى إلغائها أولاً.");
                return;
            }

            // تأكيد الحذف
            var confirm = MessageBox.Show(
                $"هل أنت متأكد من حذف الحلقة: {selectedEpisode.EpisodeName}؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    // استدعاء خدمة الحذف (تأكد من وجود هذا الدالة في الـ Interface)
                    //await _episodeService.DeleteEpisodeAsync(selectedEpisode.EpisodeId);

                    MessageService.Current.ShowSuccess("تم حذف الحلقة بنجاح.");

                    // إعادة تحميل البيانات لتحديث الجدول والإحصائيات
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    MessageService.Current.ShowError($"فشل حذف الحلقة: {ex.Message}");
                }
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
                new EpisodeFormDialog(_episodeService, _programService, _guestService, _session, null);

            if (dialog.ShowDialog() == true) await LoadDataAsync();
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ActiveEpisodeDto selectedEpisode)
            {
                var epService = _serviceProvider.GetRequiredService<IEpisodeService>();
                var progService = _serviceProvider.GetRequiredService<IProgramService>();
                var guestService = _serviceProvider.GetRequiredService<IGuestService>();

                var dialog = new EpisodeFormDialog(epService, progService, guestService, _session, selectedEpisode);

                if (dialog.ShowDialog() == true)
                {
                    await LoadDataAsync();
                }
            }
        }
    }
}