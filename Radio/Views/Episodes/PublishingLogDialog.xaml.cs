using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using System.Text.RegularExpressions;
using System.Windows;

namespace Radio.Views.Episodes
{
    /// <summary>
    /// Interaction logic for PublishingLogDialog.xaml
    /// </summary>
    public partial class PublishingLogDialog
    {
        private readonly int _episodeId;
        private readonly IPublishingService _publishingService;
        private readonly UserSession _session;

        // ✅ RegexOptions.Compiled — محسّن مرة واحدة، أفضل أداء
        private static readonly Regex NumericOnlyRegex = new(@"^[0-9.]$", RegexOptions.Compiled);

        public PublishingLogDialog(int episodeId, IPublishingService publishingService, UserSession session)
        {
            InitializeComponent();
            _episodeId = episodeId;
            _publishingService = publishingService;
            _session = session;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // التحقق من وجود رابط واحد على الأقل (اختياري حسب سياسة العمل)
            if (string.IsNullOrWhiteSpace(TxtYouTube.Text) && string.IsNullOrWhiteSpace(TxtFacebook.Text))
            {
                var result = MessageBox.Show("لم تقم بإدخال روابط نشر رئيسية (يوتيوب أو فيسبوك). هل تود الاستمرار؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No) return;
            }

            // تجهيز كائن سجل النشر
            var log = new PublishingLogDto
            {
                EpisodeId = _episodeId,
                YouTubeUrl = TxtYouTube.Text.Trim(),
                FacebookUrl = TxtFacebook.Text.Trim(),
                TwitterUrl = TxtTwitter.Text.Trim(),
                SoundCloudUrl = TxtSoundCloud.Text.Trim(),
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                BtnSave.IsEnabled = false;

                // استدعاء خدمة النشر
                var result = await _publishingService.LogPublishingAsync(log, _session);

                if (result.IsSuccess)
                {
                    MessageService.Current.ShowSuccess("تم النشر بنجاح.");
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageService.Current.ShowWarning(result.ErrorMessage ?? "فشلت عملية النشر.");
                }
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError("حدث خطأ أثناء تسجيل النشر: " + ex.Message);
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
