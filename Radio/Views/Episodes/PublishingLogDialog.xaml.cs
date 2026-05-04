using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Episodes
{
    public partial class PublishingLogDialog : UserControl
    {
        private readonly IPublishingService _publishingService;
        private readonly UserSession _session;
        private readonly int _episodeId;
        private List<EpisodeGuestDto> _guests = new();
        private List<PlatformSelectionVm> _platformVms = new();

        public PublishingLogDialog(IPublishingService publishingService,
                                    UserSession session, 
                                    int episodeId,
                                    List<EpisodeGuestDto> guests)
        {
            InitializeComponent();
            _publishingService = publishingService;
            _session = session;
            _episodeId = episodeId;
            _guests = guests ?? new List<EpisodeGuestDto>();
            Loaded += async (_, _) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                // تحميل الضيوف
                LstGuests.ItemsSource = _guests;

                // تحميل المنصات
                var platforms = await _publishingService.GetAllPlatformsAsync();
                _platformVms = platforms.Select(p => new PlatformSelectionVm
                {
                    SocialMediaPlatformId = p.SocialMediaPlatformId,
                    PlatformName = p.Name,
                    IsSelected = false,
                    Url = string.Empty
                }).ToList();
                
                IcPlatforms.ItemsSource = _platformVms;

                // إذا لم يكن هناك ضيوف، أظهر رسالة تحذير
                if (!_guests.Any())
                {
                    MessageService.Current.ShowWarning("لا توجد ضيوف في هذه الحلقة", "تنبيه");
                    return;
                }

                // اختر أول ضيف
                LstGuests.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"خطأ في تحميل البيانات: {ex.Message}", "خطأ");
            }
        }

        private void LstGuests_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // إعادة تعيين حقول النموذج عند تغيير الضيف
            if (LstGuests.SelectedItem is EpisodeGuestDto)
            {
                TxtClipTitle.Text = string.Empty;
                TxtDuration.Text = string.Empty;

                // إعادة تعيين جميع المنصات
                foreach (var vm in _platformVms)
                {
                    vm.IsSelected = false;
                    vm.Url = string.Empty;
                }
                IcPlatforms.ItemsSource = null;
                IcPlatforms.ItemsSource = _platformVms;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // التحقق من اختيار ضيف
                if (LstGuests.SelectedItem is not EpisodeGuestDto guest)
                {
                    MessageService.Current.ShowWarning("اختر ضيفاً أولاً", "تحقق من البيانات");
                    return;
                }

                // الحصول على المنصات المحددة
                var selectedPlatforms = _platformVms
                    .Where(p => p.IsSelected && !string.IsNullOrWhiteSpace(p.Url))
                    .Select(p => new PlatformPublishDto(p.SocialMediaPlatformId, p.PlatformName, p.Url))
                    .ToList();

                // التحقق من وجود منصات محددة
                if (!selectedPlatforms.Any())
                {
                    MessageService.Current.ShowWarning("اختر منصة واحدة على الأقل وأدخل رابطاً", "تحقق من البيانات");
                    return;
                }

                // تحويل مدة المقطع
                TimeSpan? duration = null;
                if (!string.IsNullOrWhiteSpace(TxtDuration.Text))
                {
                    if (TimeSpan.TryParseExact(TxtDuration.Text, @"mm\:ss", CultureInfo.InvariantCulture, out var parsedDuration))
                        duration = parsedDuration;
                    else
                    {
                        MessageService.Current.ShowError("صيغة المدة غير صحيحة. استخدم MM:SS", "خطأ");
                        return;
                    }
                }

                // إنشاء DTO
                var dto = new SocialMediaPublishingLogDto(
                    0, 
                    guest.EpisodeGuestId,
                    TxtClipTitle.Text.Trim(),
                    duration,
                    selectedPlatforms);

                // حفظ في قاعدة البيانات
                var result = await _publishingService.SavePublishingLogAsync(dto, _session);

                if (result.IsSuccess)
                {
                    MessageService.Current.ShowSuccess("تم حفظ بيانات النشر بنجاح", "نجاح");
                    DialogHost.Close("RootDialog", true);
                }
                else
                {
                    MessageService.Current.ShowError(result.ErrorMessage ?? "خطأ غير معروف", "خطأ في الحفظ");
                }
            }
            catch (Exception ex)
            {
                MessageService.Current.ShowError($"خطأ غير متوقع: {ex.Message}", "خطأ");
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => DialogHost.Close("RootDialog", false);
    }

    /// <summary>
    /// ViewModel مساعد لاختيار المنصات
    /// يدعم INotifyPropertyChanged للتحديث المباشر على الـ UI
    /// </summary>
        public class PlatformSelectionVm : INotifyPropertyChanged
        {
            public int SocialMediaPlatformId { get; set; }
            public string PlatformName { get; set; } = string.Empty;

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