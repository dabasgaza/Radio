using DataAccess.DTOs;
using DataAccess.Validation;
using Domain.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Radio.Views.Episodes
{
    /// <summary>
    /// حالة نشر الضيف: انتظار / ناقص / جاهز
    /// </summary>
    public enum GuestPublishingStatus { Pending, Partial, Ready }

    /// <summary>
    /// ViewModel لكل ضيف — يحتفظ ببيانات النشر الخاصة به
    /// بدون فقدان البيانات عند التنقل بين الضيوف
    /// مع دعم التحقق المركزي عبر ValidationPipeline
    ///
    /// ⚠ تم نقل هذا الكلاس إلى ملف منفصل لتفادي خطأ CS0229
    ///   (Ambiguity) الذي يحدث عندما يكون الكلاس داخل ملف
    ///   PublishingLogDialog.xaml.cs الذي هو partial class
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
                        NormalizeUrl(p.Url)))  // تطبيع الرابط قبل التحقق
                    .ToList());

            var result = ValidationPipeline.ValidatePublishingLog(dto);

            ValidationErrors = result.IsSuccess
                ? []
                : result.ErrorMessage!.Split(Environment.NewLine).ToList();
        }

        /// <summary>
        /// إضافة بادئة https:// للرابط إذا لم يكن يبدأ ببروتوكول
        /// يُستخدم عند بناء DTO فقط — لا يُعدّل خاصية Url في الـ VM
        /// </summary>
        private static string? NormalizeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return url;

            return $"https://{url}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
