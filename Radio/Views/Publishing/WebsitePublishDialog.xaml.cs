using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Domain.Models;
using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;
using Radio.Messaging;


namespace Radio.Views.Publishing
{
    /// <summary>
    /// نافذة نشر/تعديل نشر حلقة على الموقع الإلكتروني — تدعم وضعين:
    ///   1. إنشاء جديد: تُنشئ سجل نشر وتحوّل الحلقة إلى "منشورة على الموقع"
    ///   2. تعديل موجود: تُحدّث بيانات السجل دون تغيير حالة الحلقة
    /// </summary>
    public partial class WebsitePublishDialog : MetroWindow
    {
        private readonly IPublishingService _publishingService;
        private readonly UserSession _session;
        private readonly int _episodeId;

        /// <summary>
        /// سجل النشر الموجود — null يعني وضع إنشاء جديد
        /// </summary>
        private readonly WebsitePublishingLogDto? _existingLog;

        /// <summary>
        /// هل نحن في وضع التعديل؟
        /// </summary>
        private bool IsEditMode => _existingLog is not null;

        /// <summary>
        /// وضع إنشاء جديد — يُنشئ سجل نشر ويحوّل الحلقة إلى "منشورة على الموقع"
        /// </summary>
        public WebsitePublishDialog(IPublishingService publishingService,
                                     UserSession session,
                                     int episodeId)
            : this(publishingService, session, episodeId, null)
        {
        }

        /// <summary>
        /// وضع التعديل — يُحدّث سجل نشر موجود دون تغيير حالة الحلقة
        /// إذا مُرّر existingLog = null يعمل كوضع إنشاء جديد
        /// </summary>
        public WebsitePublishDialog(IPublishingService publishingService,
                                     UserSession session,
                                     int episodeId,
                                     WebsitePublishingLogDto? existingLog)
        {
            InitializeComponent();
            _publishingService = publishingService;
            _session = session;
            _episodeId = episodeId;
            _existingLog = existingLog;

            IsWindowDraggable = true;
            Loaded += (_, _) => InitializeForm();
        }

        private void InitializeForm()
        {
            try
            {
                if (IsEditMode && _existingLog is not null)
                {
                    // ═══ وضع التعديل: تعبئة الحقول من السجل الموجود ═══
                    PopulateFields(_existingLog);
                    SwitchToEditMode();
                }
                else
                {
                    // ═══ وضع الإنشاء: تعيين القيم الافتراضية ═══
                    CmbMediaType.SelectedIndex = 0;
                    TxtPublishTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                }
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"خطأ في تحضير النموذج: {ex.Message}");
            }
        }

        /// <summary>
        /// تعبئة حقول النموذج من بيانات السجل الموجود
        /// </summary>
        private void PopulateFields(WebsitePublishingLogDto log)
        {
            TxtTitle.Text = log.Title ?? string.Empty;
            TxtNotes.Text = log.Notes ?? string.Empty;
            TxtPublishTime.Text = log.PublishedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

            // تعيين نوع الوسائط من النص المخزن
            var mediaType = log.MediaType ?? "Video";
            for (int i = 0; i < CmbMediaType.Items.Count; i++)
            {
                if (CmbMediaType.Items[i] is ComboBoxItem item && item.Tag?.ToString() == mediaType)
                {
                    CmbMediaType.SelectedIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        /// تحويل مظهر النافذة إلى وضع التعديل:
        /// - تغيير العنوان والزر
        /// - تغيير نص الرسالة التوضيحية
        /// </summary>
        private void SwitchToEditMode()
        {
            Title = "تعديل نشر الموقع الإلكتروني";
            BtnPublish.Content = "حفظ التعديلات";
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

                if (IsEditMode)
                {
                    // ═══ وضع التعديل: تحديث السجل الموجود دون تغيير حالة الحلقة ═══
                    var dto = new WebsitePublishingLogDto(
                        _existingLog!.Id,
                        _episodeId,
                        mediaType,
                        TxtTitle.Text.Trim(),
                        TxtNotes.Text.Trim(),
                        _existingLog.PublishedAt);  // الحفاظ على تاريخ النشر الأصلي

                    var result = await _publishingService.UpdateWebsitePublishingLogAsync(dto, _session);

                    if (result.IsSuccess)
                    {
                        MessageService.Current.ShowSuccess("تم تعديل نشر الموقع بنجاح");
                        DialogResult = true;
                    }
                    else
                    {
                        MessageService.Current.ShowError(result.ErrorMessage ?? "فشل تعديل السجل");
                    }
                }
                else
                {
                    // ═══ وضع الإنشاء: نشر جديد وتحويل الحلقة إلى "منشورة على الموقع" ═══
                    var dto = new WebsitePublishingLogDto(
                        0,
                        _episodeId,
                        mediaType,
                        TxtTitle.Text.Trim(),
                        TxtNotes.Text.Trim(),
                        DateTime.UtcNow);

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
