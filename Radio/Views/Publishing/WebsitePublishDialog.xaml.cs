using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Domain.Models;
using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Publishing
{
    public partial class WebsitePublishDialog : MetroWindow
    {
        private readonly IPublishingService _publishingService;
        private readonly UserSession _session;
        private readonly int _episodeId;

        public WebsitePublishDialog(IPublishingService publishingService,
                                     UserSession session,
                                     int episodeId)
        {
            InitializeComponent();
            _publishingService = publishingService;
            _session = session;
            _episodeId = episodeId;

            IsWindowDraggable = true;

            Loaded += (_, _) => InitializeForm();
        }

        private void InitializeForm()
        {
            try
            {
                // عيّن نوع الوسيط الافتراضي (فيديو)
                CmbMediaType.SelectedIndex = 0;

                // عرض الوقت الحالي
                TxtPublishTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"خطأ في تحضير النموذج: {ex.Message}");
            }
        }

        private async void BtnPublish_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // التحقق من العنوان
                if (string.IsNullOrWhiteSpace(TxtTitle.Text))
                {
                    MessageService.Current.ShowError("عنوان النشر مطلوب");
                    TxtTitle.Focus();
                    return;
                }

                // التحقق من نوع الوسيط
                if (CmbMediaType.SelectedItem is not ComboBoxItem mediaTypeItem)
                {
                    MessageService.Current.ShowError("اختر نوع الوسيط");
                    CmbMediaType.Focus();
                    return;
                }

                // استخدم Tag بدلاً من Content — Tag يحتوي على اسم enum (Video/Audio/Both)
                string mediaType = mediaTypeItem.Tag?.ToString() ?? "Video";

                // إنشاء DTO للنشر باستخدام نص enum
                var dto = new WebsitePublishingLogDto(
                    0,
                    _episodeId,
                    mediaType,
                    TxtTitle.Text.Trim(),
                    TxtNotes.Text.Trim(),
                    DateTime.UtcNow);

                // حفظ في قاعدة البيانات
                var result = await _publishingService.PublishToWebsiteAsync(dto, _session);

                if (result.IsSuccess)
                {
                    MessageService.Current.ShowSuccess("تم نشر الحلقة على الموقع بنجاح");
                    DialogResult = true;
                }
                else
                {
                    MessageService.Current.ShowError(result.ErrorMessage ?? "خطأ غير معروف");
                }
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"خطأ غير متوقع: {ex.Message}");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;
    }
}
