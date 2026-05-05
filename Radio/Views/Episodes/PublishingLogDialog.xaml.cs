using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using DataAccess.Validation;
using Domain.Models;
using MahApps.Metro.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Radio.Views.Episodes
{
    public partial class PublishingLogDialog : MetroWindow
    {
        private readonly IPublishingService _publishingService;
        private readonly UserSession _session;
        private readonly int _episodeId;
        private List<GuestPublishingVm> _guestVms = [];
        private List<PlatformSelectionVm> _platformTemplates = [];
        private bool _isUpdatingSelection;
        private bool _isDataLoaded;

        public PublishingLogDialog(
            IPublishingService publishingService,
            UserSession session,
            int episodeId,
            List<EpisodeGuestDto> guests)
        {
            InitializeComponent();

            // منع تشغيل معالجات الأحداث أثناء InitializeComponent
            // لأن CmbMediaType بـ SelectedIndex="2" يطلق SelectionChanged
            // قبل أن يتم إنشاء TxtStatusSummary في الشجرة البصرية
            _isUpdatingSelection = true;

            _publishingService = publishingService;
            _session = session;
            _episodeId = episodeId;

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
                _guestVms = (guests ?? []).Select(g => new GuestPublishingVm
                {
                    EpisodeGuestId = g.EpisodeGuestId,
                    FullName = g.FullName,
                    Topic = g.Topic,
                    Platforms = _platformTemplates.Select(pt => new PlatformSelectionVm
                    {
                        SocialMediaPlatformId = pt.SocialMediaPlatformId,
                        PlatformName = pt.PlatformName,
                        PlatformIcon = pt.PlatformIcon,
                        IsSelected = false,
                        Url = string.Empty
                    }).ToList()
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

                LstGuests.SelectedIndex = 0;
                UpdateStatusSummary();
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

            // تحميل منصات الضيف
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

                // بناء قائمة DTOs
                var guestLogs = readyGuests.Select(g =>
                {
                    var duration = BuildDuration(g.DurationMinutes, g.DurationSeconds);

                    return new SocialMediaPublishingLogDto(
                        0,
                        g.EpisodeGuestId,
                        g.ClipTitle,
                        duration,
                        g.MediaType,
                        g.Platforms
                            .Where(p => p.IsSelected && !string.IsNullOrWhiteSpace(p.Url))
                            .Select(p => new PlatformPublishDto(
                                p.SocialMediaPlatformId,
                                p.PlatformName,
                                p.Url))
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

                // حفظ عبر الخدمة (دفعة واحدة)
                BtnSaveAll.IsEnabled = false;

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

    // ═══════════════════════════════════════════════════════════
    //  ViewModels
    // ═══════════════════════════════════════════════════════════

    public enum GuestPublishingStatus { Pending, Partial, Ready }

    /// <summary>
    /// ViewModel لكل ضيف — يحتفظ ببيانات النشر الخاصة به
    /// بدون فقدان البيانات عند التنقل بين الضيوف
    /// مع دعم التحقق المركزي عبر ValidationPipeline
    /// </summary>
    public class GuestPublishingVm : INotifyPropertyChanged
    {
        public int EpisodeGuestId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Topic { get; set; }

        private string? _clipTitle;
        public string? ClipTitle
        {
            get => _clipTitle;
            set { _clipTitle = value; OnPropertyChanged(); }
        }

        private int? _durationMinutes;
        public int? DurationMinutes
        {
            get => _durationMinutes;
            set { _durationMinutes = value; OnPropertyChanged(); }
        }

        private int? _durationSeconds;
        public int? DurationSeconds
        {
            get => _durationSeconds;
            set { _durationSeconds = value; OnPropertyChanged(); }
        }

        public MediaType MediaType { get; set; } = MediaType.Both;

        public List<PlatformSelectionVm> Platforms { get; set; } = [];

        private GuestPublishingStatus _status = GuestPublishingStatus.Pending;
        public GuestPublishingStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        private List<string> _validationErrors = [];
        public List<string> ValidationErrors
        {
            get => _validationErrors;
            set { _validationErrors = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// إعادة حساب حالة الضيف بناءً على البيانات المدخلة
        /// </summary>
        public void RefreshStatus()
        {
            var hasTitle = !string.IsNullOrWhiteSpace(ClipTitle);
            var hasPlatform = Platforms.Any(p => p.IsSelected && !string.IsNullOrWhiteSpace(p.Url));

            Status = (hasTitle, hasPlatform) switch
            {
                (true, true) => GuestPublishingStatus.Ready,
                (false, false) => GuestPublishingStatus.Pending,
                _ => GuestPublishingStatus.Partial
            };
        }

        /// <summary>
        /// تشغيل ValidationPipeline على بيانات الضيف وتخزين الأخطاء
        /// </summary>
        public void Validate()
        {
            var dto = new SocialMediaPublishingLogDto(
                0,
                EpisodeGuestId,
                ClipTitle,
                null,
                MediaType,
                Platforms
                    .Where(p => p.IsSelected && !string.IsNullOrWhiteSpace(p.Url))
                    .Select(p => new PlatformPublishDto(
                        p.SocialMediaPlatformId,
                        p.PlatformName,
                        p.Url))
                    .ToList());

            var result = ValidationPipeline.ValidatePublishingLog(dto);

            ValidationErrors = result.IsSuccess
                ? []
                : result.ErrorMessage!.Split(Environment.NewLine).ToList();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// ViewModel لكل منصة — يدعم INotifyPropertyChanged للتحديث المباشر
    /// </summary>
    public class PlatformSelectionVm : INotifyPropertyChanged
    {
        public int SocialMediaPlatformId { get; set; }
        public string PlatformName { get; set; } = string.Empty;
        public string PlatformIcon { get; set; } = "ShareVariant";

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private string? _url;
        public string? Url
        {
            get => _url;
            set { _url = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
