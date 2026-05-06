using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using DataAccess.Validation;
using Domain.Models;
using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Radio.Messaging;


namespace Radio.Views.Episodes
{
    /// <summary>
    /// نافذة نشر/تعديل مقاطع الضيوف على السوشيال ميديا — تدعم وضعين:
    ///   1. إنشاء جديد: تُنشئ سجلات نشر وتحوّل الحلقة إلى "منشورة رقمياً"
    ///   2. تعديل موجود: تُحدّث بيانات السجلات دون تغيير حالة الحلقة
    /// </summary>
    public partial class PublishingLogDialog : MetroWindow
    {
        private readonly IPublishingService _publishingService;
        private readonly UserSession _session;
        private readonly int _episodeId;
        private List<GuestPublishingVm> _guestVms = [];
        private List<PlatformSelectionVm> _platformTemplates = [];
        private bool _isUpdatingSelection;
        private bool _isDataLoaded;

        /// <summary>
        /// سجلات النشر الموجودة — null يعني وضع إنشاء جديد
        /// في وضع التعديل، تحتوي على بيانات النشر السابقة لكل ضيف
        /// </summary>
        private readonly List<SocialMediaPublishingLogDto>? _existingLogs;

        /// <summary>
        /// هل نحن في وضع التعديل؟
        /// </summary>
        private bool IsEditMode => _existingLogs is not null;

        /// <summary>
        /// وضع إنشاء جديد — يُنشئ سجلات نشر ويحوّل الحلقة إلى "منشورة رقمياً"
        /// </summary>
        public PublishingLogDialog(
            IPublishingService publishingService,
            UserSession session,
            int episodeId,
            List<EpisodeGuestDto> guests)
            : this(publishingService, session, episodeId, guests, null)
        {
        }

        /// <summary>
        /// وضع التعديل — يُحدّث سجلات النشر الموجودة دون تغيير حالة الحلقة
        /// إذا مُرّر existingLogs = null يعمل كوضع إنشاء جديد
        /// </summary>
        public PublishingLogDialog(
            IPublishingService publishingService,
            UserSession session,
            int episodeId,
            List<EpisodeGuestDto> guests,
            List<SocialMediaPublishingLogDto>? existingLogs)
        {
            InitializeComponent();

            // منع تشغيل معالجات الأحداث أثناء InitializeComponent
            // لأن CmbMediaType بـ SelectedIndex="2" يطلق SelectionChanged
            // قبل أن يتم إنشاء TxtStatusSummary في الشجرة البصرية
            _isUpdatingSelection = true;

            _publishingService = publishingService;
            _session = session;
            _episodeId = episodeId;
            _existingLogs = existingLogs;

            IsWindowDraggable = true;
            Loaded += async (_, _) => await LoadAsync(guests);
        }

        // ═══════════════════════════════════════════
        //  التحميل الأولي
        // ═══════════════════════════════════════════

        private async Task LoadAsync(List<EpisodeGuestDto> guests)
        {
            try
            {
                // تحميل المنصات المتاحة
                var platforms = await _publishingService.GetAllPlatformsAsync();
                _platformTemplates = platforms.Select(p => new PlatformSelectionVm
                {
                    SocialMediaPlatformId = p.SocialMediaPlatformId,
                    PlatformName = p.Name,
                    PlatformIcon = p.Icon ?? "ShareVariant",
                    IsSelected = false,
                    Url = string.Empty
                }).ToList();

                // إنشاء ViewModel لكل ضيف مع نسخة مستقلة من المنصات
                // في وضع التعديل: ندمج البيانات المحفوظة مسبقاً مع قالب المنصات
                _guestVms = (guests ?? []).Select(g =>
                {
                    // البحث عن سجل موجود لهذا الضيف (في وضع التعديل)
                    var existingLog = _existingLogs?
                        .FirstOrDefault(l => l.EpisodeGuestId == g.EpisodeGuestId);

                    return new GuestPublishingVm
                    {
                        EpisodeGuestId = g.EpisodeGuestId,
                        FullName = g.FullName,
                        Topic = g.Topic,

                        // تعبئة البيانات من السجل الموجود إن وُجد
                        ClipTitle = existingLog?.ClipTitle,
                        DurationMinutes = existingLog?.Duration?.Minutes,
                        DurationSeconds = existingLog?.Duration?.Seconds,
                        MediaType = existingLog?.MediaType ?? MediaType.Both,

                        // دمج المنصات: المنصات المحددة سابقاً مع روابطها + المنصات غير المحددة
                        Platforms = _platformTemplates.Select(pt =>
                        {
                            var existingPlatform = existingLog?.Platforms
                                .FirstOrDefault(p => p.PlatformId == pt.SocialMediaPlatformId);

                            return new PlatformSelectionVm
                            {
                                SocialMediaPlatformId = pt.SocialMediaPlatformId,
                                PlatformName = pt.PlatformName,
                                PlatformIcon = pt.PlatformIcon,
                                IsSelected = existingPlatform is not null,
                                Url = existingPlatform is not null
                                    ? StripProtocol(existingPlatform.Url)
                                    : string.Empty
                            };
                        }).ToList()
                    };
                }).ToList();

                LstGuests.ItemsSource = _guestVms;

                if (!_guestVms.Any())
                {
                    MessageService.Current.ShowWarning("لا توجد ضيوف في هذه الحلقة.");
                    return;
                }

                // الآن أصبحت البيانات جاهزة — نسمح بالتفاعل
                _isDataLoaded = true;
                _isUpdatingSelection = false;

                // تحديث حالة كل ضيف بناءً على البيانات المحفوظة
                foreach (var guest in _guestVms)
                {
                    guest.RefreshStatus();
                    guest.Validate();
                }

                LstGuests.SelectedIndex = 0;
                UpdateStatusSummary();

                // تحويل المظهر إلى وضع التعديل إذا لزم
                if (IsEditMode)
                {
                    Title = "تعديل بيانات النشر الرقمي";
                    BtnSaveAll.Content = "حفظ التعديلات";
                }
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"خطأ في تحميل البيانات: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════
        //  التنقل بين الضيوف (بدون فقدان البيانات)
        // ═══════════════════════════════════════════

        private void LstGuests_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelection || !_isDataLoaded) return;

            // ⚠ نقطة حرجة: عند إطلاق SelectionChanged يكون SelectedItem قد تغيّر فعلاً
            // للضيف الجديد، لكن حقول النموذج لا تزال تعرض بيانات الضيف القديم.
            // لذلك نأخذ الضيف السابق من RemovedItems وليس من SelectedItem.
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is GuestPublishingVm previousGuest)
            {
                SaveGuestData(previousGuest);
            }

            // عرض بيانات الضيف الجديد
            LoadGuestData();
            UpdateStatusSummary();
        }

        /// <summary>
        /// حفظ بيانات النموذج الحالي في الـ ViewModel الخاص بالضيف المحدد حالياً
        /// يُستخدم عندما نريد حفظ بيانات الضيف المعروض حالياً (FormField_Changed / BtnSaveAll)
        /// </summary>
        private void SaveCurrentGuestData()
        {
            if (!_isDataLoaded) return;
            if (LstGuests.SelectedItem is not GuestPublishingVm guest) return;
            SaveGuestData(guest);
        }

        /// <summary>
        /// حفظ بيانات النموذج في ضيف محدد — يقرأ القيم من حقول الإدخال ويكتبها في الـ ViewModel
        /// ⚠ هذا هو المنطق المركزي: لا نقرأ من SelectedItem بل من المعامل guest مباشرة
        /// </summary>
        private void SaveGuestData(GuestPublishingVm guest)
        {
            if (guest is null) return;

            guest.ClipTitle = TxtClipTitle.Text?.Trim();

            // تحويل المدّة
            guest.DurationMinutes = int.TryParse(TxtMinutes.Text, out var min) ? min : (int?)null;
            guest.DurationSeconds = int.TryParse(TxtSeconds.Text, out var sec) ? sec : (int?)null;

            // نوع الوسائط
            guest.MediaType = GetSelectedMediaType();

            // المنصات محفوظة تلقائياً عبر TwoWay Binding
            // ⚠ لا نطبق NormalizeUrl هنا! لأن تعديل Url سيُنعكس
            // على الـ TextBox عبر TwoWay Binding فيظهر "https://" في الحقل.
            // التطبيع يتم فقط عند بناء DTO للحفظ (BtnSaveAll_Click)
            // أو عند التحقق (GuestPublishingVm.Validate).

            // تحديث حالة الضيف والتحقق
            guest.RefreshStatus();
            guest.Validate();
        }

        /// <summary>
        /// تحميل بيانات الضيف المحدد في النموذج
        /// </summary>
        private void LoadGuestData()
        {
            if (LstGuests.SelectedItem is not GuestPublishingVm guest)
            {
                PnlGuestForm.IsEnabled = false;
                return;
            }

            PnlGuestForm.IsEnabled = true;

            _isUpdatingSelection = true;

            TxtClipTitle.Text = guest.ClipTitle ?? string.Empty;
            TxtMinutes.Text = guest.DurationMinutes?.ToString() ?? string.Empty;
            TxtSeconds.Text = guest.DurationSeconds?.ToString() ?? string.Empty;

            // تعيين نوع الوسائط
            CmbMediaType.SelectedIndex = guest.MediaType switch
            {
                MediaType.Audio => 0,
                MediaType.Video => 1,
                _ => 2 // Both
            };

            // تحميل منصات الضيف (مع إزالة البروتوكول من الروابط للعرض)
            foreach (var platform in guest.Platforms)
            {
                platform.Url = StripProtocol(platform.Url);
            }
            IcPlatforms.ItemsSource = guest.Platforms;

            // عرض أخطاء التحقق إن وجدت
            ShowValidationErrors(guest);

            _isUpdatingSelection = false;
        }

        // ═══════════════════════════════════════════
        //  التحديث الفوري لحالة الضيف
        // ═══════════════════════════════════════════

        private void FormField_Changed(object sender, EventArgs e)
        {
            if (_isUpdatingSelection || !_isDataLoaded) return;

            SaveCurrentGuestData();
            UpdateStatusSummary();

            // عرض أخطاء التحقق للضيف الحالي
            if (LstGuests.SelectedItem is GuestPublishingVm guest)
                ShowValidationErrors(guest);

            // إعادة عرض الحالة في القائمة
            LstGuests.Items.Refresh();
        }

        private void UpdateStatusSummary()
        {
            // حماية إضافية: العنصر قد لا يكون موجوداً بعد في الشجرة البصرية
            if (TxtStatusSummary is null) return;

            var ready = _guestVms.Count(g => g.Status == GuestPublishingStatus.Ready);
            var partial = _guestVms.Count(g => g.Status == GuestPublishingStatus.Partial);
            var pending = _guestVms.Count(g => g.Status == GuestPublishingStatus.Pending);

            var parts = new List<string>();
            if (ready > 0) parts.Add($"{ready} جاهز");
            if (partial > 0) parts.Add($"{partial} ناقص");
            if (pending > 0) parts.Add($"{pending} انتظار");

            TxtStatusSummary.Text = parts.Any()
                ? string.Join(" | ", parts)
                : "لا يوجد ضيوف";
        }

        // ═══════════════════════════════════════════
        //  التحقق من إدخال المدة (أرقام فقط)
        // ═══════════════════════════════════════════

        private void TxtDuration_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // السماح بالأرقام فقط
            e.Handled = !e.Text.All(char.IsDigit);
        }

        // ═══════════════════════════════════════════
        //  معالجة حقل الرابط (بادئة https://)
        // ═══════════════════════════════════════════

        /// <summary>
        /// منع المستخدم من كتابة البروتوكول يدوياً في حقل الرابط
        /// لأن البادئة https:// معروضة كنص ثابت بجانب الحقل
        /// </summary>
        private void Url_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            // حساب النص الناتج بعد الإدخال
            var newText = textBox.Text.Substring(0, textBox.SelectionStart)
                + e.Text
                + textBox.Text.Substring(textBox.SelectionStart + textBox.SelectionLength);

            // إذا بدأ بـ http، نمنع الإدخال (المستخدم يحاول كتابة البروتوكول)
            if (newText.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                newText.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// معالجة اللصق في حقل الرابط:
        /// - إذا لصق المستخدم رابطاً كاملاً (https://...) نزيل البروتوكول تلقائياً
        /// - نعترض Ctrl+V ونضع النص المنظف بدلاً منه
        /// </summary>
        private void Url_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.V || !(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                return;

            if (sender is not TextBox textBox) return;

            // قراءة النص من الحافظة
            if (Clipboard.ContainsText())
            {
                var pasted = Clipboard.GetText().Trim();
                var stripped = StripProtocol(pasted);

                if (!string.IsNullOrEmpty(stripped))
                {
                    // وضع النص المنظف (بدون البروتوكول) في الحقل
                    var caretPos = textBox.SelectionStart;
                    var before = textBox.Text.Substring(0, caretPos);
                    var after = textBox.Text.Substring(caretPos + textBox.SelectionLength);
                    textBox.Text = before + stripped + after;
                    textBox.CaretIndex = before.Length + stripped.Length;

                    // رفع حدث التغيير يدوياً
                    FormField_Changed(sender, EventArgs.Empty);
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// إزالة بروتوكول http:// أو https:// من الرابط
        /// لعرضه في حقل الإدخال بدون البروتوكول (لأنه معروض كبادئة ثابتة)
        /// </summary>
        private static string? StripProtocol(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;

            if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return url["https://".Length..];

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                return url["http://".Length..];

            return url;
        }

        /// <summary>
        /// دمج بادئة https:// مع إدخال المستخدم للحصول على الرابط الكامل
        /// يُستخدم عند الحفظ
        /// </summary>
        private static string? NormalizeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;

            // إذا كان الرابط يبدأ فعلاً ببروتوكول، أعده كما هو
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return url;

            // إضافة https:// كبادئة
            return $"https://{url}";
        }

        // ═══════════════════════════════════════════
        //  عرض أخطاء التحقق
        // ═══════════════════════════════════════════

        private void ShowValidationErrors(GuestPublishingVm guest)
        {
            if (PnlValidationErrors is null) return;

            if (guest.ValidationErrors.Any())
            {
                PnlValidationErrors.Visibility = Visibility.Visible;
                LblValidationHeader.Text = $"أخطاء في بيانات {guest.FullName}:";
                LstValidationMessages.ItemsSource = guest.ValidationErrors.ToList();
            }
            else
            {
                PnlValidationErrors.Visibility = Visibility.Collapsed;
            }
        }

        // ═══════════════════════════════════════════
        //  حفظ الكل
        // ═══════════════════════════════════════════

        private async void BtnSaveAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // حفظ بيانات الضيف الحالي أولاً
                SaveCurrentGuestData();

                // جمع بيانات الضيوف الجاهزين فقط
                var readyGuests = _guestVms
                    .Where(g => g.Status == GuestPublishingStatus.Ready)
                    .ToList();

                if (!readyGuests.Any())
                {
                    MessageService.Current.ShowWarning(
                        "لا توجد بيانات نشر مكتملة للحفظ.\n" +
                        "يرجى تعبئة عنوان المقطع واختيار منصة واحدة على الأقل مع إدخال الرابط لضيف واحد.");
                    return;
                }

                // بناء قائمة DTOs (الروابط مدمج معها https:// عبر NormalizeUrl)
                var guestLogs = readyGuests.Select(g =>
                {
                    var duration = BuildDuration(g.DurationMinutes, g.DurationSeconds);

                    // في وضع التعديل: نستخدم معرّف السجل الموجود
                    var logId = 0;
                    if (IsEditMode)
                    {
                        var existingLog = _existingLogs!
                            .FirstOrDefault(l => l.EpisodeGuestId == g.EpisodeGuestId);
                        logId = existingLog?.LogId ?? 0;
                    }

                    return new SocialMediaPublishingLogDto(
                        logId,
                        g.EpisodeGuestId,
                        g.ClipTitle,
                        duration,
                        g.MediaType,
                        g.Platforms
                            .Where(p => p.IsSelected && !string.IsNullOrWhiteSpace(p.Url))
                            .Select(p => new PlatformPublishDto(
                                p.SocialMediaPlatformId,
                                p.PlatformName,
                                NormalizeUrl(p.Url)))
                            .ToList());
                }).ToList();

                // أسماء الضيوف الجاهزين لرسائل خطأ أوضح
                var readyGuestNames = readyGuests.Select(g => g.FullName).ToList();

                // التحقق المركزي عبر ValidationPipeline مع أسماء الضيوف
                var validation = ValidationPipeline.ValidatePublishingBatch(guestLogs, readyGuestNames);
                if (!validation.IsSuccess)
                {
                    MessageService.Current.ShowWarning(validation.ErrorMessage!);
                    return;
                }

                // حفظ عبر الخدمة
                BtnSaveAll.IsEnabled = false;

                if (IsEditMode)
                {
                    // ═══ وضع التعديل: تحديث كل سجل على حدة ═══
                    var successCount = 0;
                    foreach (var logDto in guestLogs)
                    {
                        var result = await _publishingService.UpdateSocialPublishingLogAsync(logDto, _session);
                        if (result.IsSuccess)
                            successCount++;
                    }

                    MessageService.Current.ShowSuccess(
                        $"تم تعديل بيانات نشر {successCount} ضيف بنجاح.");
                    DialogResult = true;
                }
                else
                {
                    // ═══ وضع الإنشاء: حفظ دفعة واحدة ═══
                    var result = await _publishingService.LogSocialPublishingAsync(_episodeId, guestLogs, _session);

                    if (result.IsSuccess)
                    {
                        MessageService.Current.ShowSuccess(
                            $"تم نشر مقاطع {readyGuests.Count} ضيف بنجاح.");
                        DialogResult = true;
                    }
                    else
                    {
                        MessageService.Current.ShowError(result.ErrorMessage ?? "خطأ في حفظ بيانات النشر.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"خطأ غير متوقع: {ex.Message}");
            }
            finally
            {
                BtnSaveAll.IsEnabled = true;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        // ═══════════════════════════════════════════
        //  أدوات مساعدة
        // ═══════════════════════════════════════════

        private MediaType GetSelectedMediaType()
        {
            var selected = CmbMediaType.SelectedItem as ComboBoxItem;
            var tag = selected?.Tag?.ToString();
            return tag switch
            {
                "Audio" => MediaType.Audio,
                "Video" => MediaType.Video,
                _ => MediaType.Both
            };
        }

        private static TimeSpan? BuildDuration(int? minutes, int? seconds)
        {
            if (minutes is null && seconds is null) return null;

            var min = Math.Clamp(minutes ?? 0, 0, 999);
            var sec = Math.Clamp(seconds ?? 0, 0, 59);

            return TimeSpan.FromSeconds(min * 60 + sec);
        }
    }
}
