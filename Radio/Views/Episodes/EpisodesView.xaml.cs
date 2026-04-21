using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Episodes
{
    /// <summary>
    /// شاشة إدارة الحلقات — تعرض قائمة الحلقات النشطة مع إمكانية
    /// الإضافة والتعديل والحذف والبحث والتنفيذ والنشر.
    /// </summary>
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

            // ✅ استخدام AppPermissions بدلاً من نصوص ثابتة
            BtnAddEpisode.Visibility = _session.HasPermission(AppPermissions.EpisodeManage)
                ? Visibility.Visible
                : Visibility.Collapsed;

            Loaded += async (_, _) => await LoadDataAsync();
        }

        #region Data Loading

        /// <summary>
        /// تحميل جميع الحلقات النشطة وربطها بالـ DataGrid مع تحديث الإحصائيات.
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                _allEpisodes = (await _episodeService.GetActiveEpisodesAsync()).ToList();
                DgEpisodes.ItemsSource = _allEpisodes;
                UpdateStats(_allEpisodes);
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError("ليس لديك صلاحية لعرض بيانات الحلقات.");
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

        /// <summary>
        /// تحديث إحصائيات الحلقات (مجدولة / منفّذة / منشورة).
        /// </summary>
        private void UpdateStats(IEnumerable<ActiveEpisodeDto> data)
        {
            //TxtTotal.Text = data.Count(ep => ep.StatusText == "مجدولة").ToString();
            //TxtExecuted.Text = data.Count(ep => ep.StatusText == "منفّذة").ToString();
            //TxtPublished.Text = data.Count(ep => ep.StatusText == "منشورة").ToString();
        }

        #endregion

        #region Search

        /// <summary>
        /// البحث الفوري في قائمة الحلقات حسب عنوان الحلقة أو اسم الضيف.
        /// </summary>
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || _allEpisodes.Count == 0)
                return;

            string keyword = textBox.Text.Trim();

            // ✅ StringComparison بدلاً من ToLower()
            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allEpisodes
                : _allEpisodes.Where(ep =>
                    (ep.EpisodeName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (ep.GuestName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));

            var result = filtered.ToList();
            DgEpisodes.ItemsSource = result;
            UpdateStats(result);
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// فتح نافذة إضافة حلقة جديدة.
        /// </summary>
        private void BtnAddEpisode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new EpisodeFormDialog(
                    _episodeService, _programService, _guestService, _session);

                if (dialog.ShowDialog() == true)
                    _ = LoadDataAsync();
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء فتح نافذة إضافة الحلقة.");
            }
        }

        /// <summary>
        /// فتح نافذة تعديل حلقة موجودة.
        /// </summary>
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto selectedEpisode)
                return;

            try
            {
                var dialog = new EpisodeFormDialog(
                    _episodeService, _programService, _guestService,
                    _session, selectedEpisode);

                if (dialog.ShowDialog() == true)
                    _ = LoadDataAsync();
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء فتح نافذة تعديل الحلقة.");
            }
        }

        /// <summary>
        /// حذف حلقة بعد التحقق من حالتها وتأكيد المستخدم.
        /// </summary>
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto selectedEpisode)
                return;

            // ✅ منع حذف الحلقات المنفذة أو المنشورة
            if (selectedEpisode.StatusText is "منفّذة" or "منشورة")
            {
                MessageService.Current.ShowWarning(
                    "لا يمكن حذف حلقة تم تنفيذها أو نشرها، يُرجى إلغائها أولاً.");
                return;
            }

            // ✅ MessageService بدلاً من MessageBox
            bool isConfirmed = await MessageService.Current.ShowConfirmationAsync(
                $"هل أنت متأكد من حذف الحلقة: {selectedEpisode.EpisodeName}؟",
                "تأكيد الحذف");

            if (!isConfirmed)
                return;

            try
            {
                await _episodeService.DeleteEpisodeAsync(selectedEpisode.EpisodeId, _session);
                await LoadDataAsync();
                MessageService.Current.ShowSuccess($"تم حذف الحلقة «{selectedEpisode.EpisodeName}» بنجاح.");
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError("ليس لديك صلاحية لحذف الحلقات.");
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حذف الحلقة.");
            }
        }

        #endregion

        #region Execution & Publishing

        /// <summary>
        /// فتح نافذة تسجيل تنفيذ حلقة (للحلقات المجدولة فقط).
        /// </summary>
        private void BtnMarkExecuted_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto ep)
                return;

            if (ep.StatusText != "مجدولة")
                return;

            try
            {
                var execService = _serviceProvider.GetRequiredService<IExecutionService>();
                var dialog = new ExecutionLogDialog(ep.EpisodeId, execService, _session);

                if (dialog.ShowDialog() == true)
                    _ = LoadDataAsync();
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء فتح نافذة تسجيل التنفيذ.");
            }
        }

        /// <summary>
        /// فتح نافذة تسجيل نشر حلقة (للحلقات المنفّذة فقط).
        /// </summary>
        private void BtnMarkPublished_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not ActiveEpisodeDto ep)
                return;

            if (ep.StatusText != "منفّذة")
                return;

            try
            {
                var pubService = _serviceProvider.GetRequiredService<IPublishingService>();
                var dialog = new PublishingLogDialog(ep.EpisodeId, pubService, _session);

                if (dialog.ShowDialog() == true)
                    _ = LoadDataAsync();
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء فتح نافذة تسجيل النشر.");
            }
        }

        #endregion
    }
}