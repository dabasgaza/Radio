using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services;
using MaterialDesignThemes.Wpf;

namespace Radio.Views.Episodes
{
    public partial class EpisodeFormControl : UserControl
    {
        private readonly IEpisodeService _episodeService;
        private readonly IProgramService _programService;
        private readonly IGuestService _guestService;
        private readonly ICorrespondentService _correspondentService;
        private readonly IUserService _userService;
        private readonly UserSession _session;
        private readonly int? _episodeId;

        // ── قوائم Observable مرتبطة بالجداول ──
        public ObservableCollection<GuestRow> GuestList { get; } = [];
        public ObservableCollection<CorrespondentRow> CorrespondentList { get; } = [];
        public ObservableCollection<EmployeeRow> EmployeeList { get; } = [];

        public EpisodeFormControl(
            IEpisodeService episodeService,
            IProgramService programService,
            IGuestService guestService,
            ICorrespondentService correspondentService,
            IUserService userService,
            UserSession session,
            int? episodeId = null)
        {
            InitializeComponent();
            _episodeService = episodeService;
            _programService = programService;
            _guestService = guestService;
            _correspondentService = correspondentService;
            _userService = userService;
            _session = session;
            _episodeId = episodeId;

            DgGuests.ItemsSource = GuestList;
            DgCorrespondents.ItemsSource = CorrespondentList;
            DgEmployees.ItemsSource = EmployeeList;

            Loaded += async (_, _) => await InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                CbPrograms.ItemsSource = await _programService.GetAllActiveAsync();
                CbAllGuests.ItemsSource = await _guestService.GetAllActiveAsync();
                CbAllCorrespondents.ItemsSource = await _correspondentService.GetAllActiveAsync();
                CbAllEmployees.ItemsSource = await _userService.GetAllUsersAsync();

                if (_episodeId.HasValue)
                {
                    var ep = await _episodeService.GetActiveEpisodeByIdAsync(_episodeId.Value);
                    if (ep == null) return;

                    TxtEpisodeName.Text = ep.EpisodeName;
                    DpDate.SelectedDate = ep.ScheduledExecutionTime;
                    TxtSpecialNotes.Text = ep.SpecialNotes;
                    CbPrograms.SelectedValue = ep.ProgramId;

                    // تحميل الضيوف الكاملين من الخدمة (يشمل الاسم والموضوع والساعة)
                    var guests = await _episodeService.GetEpisodeGuestsAsync(_episodeId.Value);
                    foreach (var g in guests)
                        GuestList.Add(new GuestRow(g.EpisodeGuestId, g.GuestId, g.FullName, g.Topic, g.HostingTime));

                    // تحميل المراسلين
                    foreach (var c in ep.CorrespondentItems)
                        CorrespondentList.Add(new CorrespondentRow(c.Id, c.CorrespondentId, c.FullName, c.Topic, c.HostingTime));

                    // تحميل الموظفين
                    var allUsers = (await _userService.GetAllUsersAsync()).ToList();
                    foreach (var e in ep.EmployeeItems)
                    {
                        var user = allUsers.FirstOrDefault(u => u.UserId == e.EmployeeId);
                        EmployeeList.Add(new EmployeeRow(e.EmployeeId, user?.FullName ?? "غير معروف", user?.RoleName ?? "—"));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── إدارة الضيوف ──────────────────────────────────────────

        private void BtnAddGuest_Click(object sender, RoutedEventArgs e)
        {
            if (CbAllGuests.SelectedItem is not GuestDto guest)
            {
                MessageBox.Show("يرجى اختيار ضيف من القائمة أولاً."); return;
            }
            if (GuestList.Any(x => x.GuestId == guest.GuestId))
            {
                MessageBox.Show("هذا الضيف مضاف بالفعل."); return;
            }

            var topic = TxtGuestTopic.Text?.Trim();
            var hostingTime = TpGuestHostingTime.SelectedTime.HasValue
                ? TpGuestHostingTime.SelectedTime.Value.TimeOfDay
                : (TimeSpan?)null;

            GuestList.Add(new GuestRow(0, guest.GuestId, guest.FullName, topic, hostingTime));

            // تصفير حقول الإضافة
            CbAllGuests.SelectedItem = null;
            TxtGuestTopic.Clear();
            TpGuestHostingTime.SelectedTime = null;
        }

        private void BtnRemoveGuest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is GuestRow row)
                GuestList.Remove(row);
        }

        // ── إدارة المراسلين ────────────────────────────────────────

        private void BtnAddCorrespondent_Click(object sender, RoutedEventArgs e)
        {
            if (CbAllCorrespondents.SelectedItem is not CorrespondentDto corr)
            {
                MessageBox.Show("يرجى اختيار مراسل من القائمة أولاً."); return;
            }
            if (CorrespondentList.Any(x => x.CorrespondentId == corr.CorrespondentId))
            {
                MessageBox.Show("هذا المراسل مضاف بالفعل."); return;
            }

            var topic = TxtCorrespondentTopic.Text?.Trim();
            var hostingTime = TpCorrespondentHostingTime.SelectedTime.HasValue
                ? TpCorrespondentHostingTime.SelectedTime.Value.TimeOfDay
                : (TimeSpan?)null;

            CorrespondentList.Add(new CorrespondentRow(0, corr.CorrespondentId, corr.FullName, topic, hostingTime));

            CbAllCorrespondents.SelectedItem = null;
            TxtCorrespondentTopic.Clear();
            TpCorrespondentHostingTime.SelectedTime = null;
        }

        private void BtnRemoveCorrespondent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CorrespondentRow row)
                CorrespondentList.Remove(row);
        }

        // ── إدارة طاقم العمل ──────────────────────────────────────

        private void BtnAddEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (CbAllEmployees.SelectedItem is not UserDto user)
            {
                MessageBox.Show("يرجى اختيار موظف من القائمة أولاً."); return;
            }
            if (EmployeeList.Any(x => x.EmployeeId == user.UserId))
            {
                MessageBox.Show("هذا الموظف مضاف بالفعل."); return;
            }

            EmployeeList.Add(new EmployeeRow(user.UserId, user.FullName, user.RoleName ?? "—"));
            CbAllEmployees.SelectedItem = null;
        }

        private void BtnRemoveEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is EmployeeRow row)
                EmployeeList.Remove(row);
        }

        // ── الحفظ والإلغاء ────────────────────────────────────────

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (CbPrograms.SelectedValue == null) { MessageBox.Show("يرجى اختيار البرنامج."); return; }
            if (string.IsNullOrWhiteSpace(TxtEpisodeName.Text)) { MessageBox.Show("يرجى إدخال عنوان الحلقة."); return; }
            if (DpDate.SelectedDate == null) { MessageBox.Show("يرجى تحديد تاريخ البث."); return; }

            var dto = new EpisodeDto(
                _episodeId ?? 0,
                (int)CbPrograms.SelectedValue,
                GuestList.Select(g => new EpisodeGuestDto(g.EpisodeGuestId, g.GuestId, g.FullName, g.Topic, g.HostingTime, null)).ToList(),
                CorrespondentList.Select(c => new EpisodeCorrespondentDto(c.Id, c.CorrespondentId, c.FullName, c.Topic, c.HostingTime)).ToList(),
                EmployeeList.Select(e => new EpisodeEmployeeDto(0, e.EmployeeId)).ToList(),
                TxtEpisodeName.Text.Trim(),
                DpDate.SelectedDate,
                TxtSpecialNotes.Text?.Trim()
            );

            Result result;
            if (_episodeId.HasValue)
                result = await _episodeService.UpdateEpisodeAsync(dto, _session);
            else
            {
                var createRes = await _episodeService.CreateEpisodeAsync(dto, _session);
                result = createRes.IsSuccess ? Result.Success() : Result.Fail(createRes.ErrorMessage ?? "خطأ غير معروف");
            }

            if (result.IsSuccess) DialogHost.Close("RootDialog", true);
            else MessageBox.Show(result.ErrorMessage ?? "فشل الحفظ.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogHost.Close("RootDialog", false);

        // ── نماذج البيانات الداخلية (ViewModels) ──────────────────

        public class GuestRow(int episodeGuestId, int guestId, string fullName, string? topic, TimeSpan? hostingTime)
        {
            public int EpisodeGuestId { get; set; } = episodeGuestId;
            public int GuestId { get; set; } = guestId;
            public string FullName { get; set; } = fullName;
            public string? Topic { get; set; } = topic;
            public TimeSpan? HostingTime { get; set; } = hostingTime;
            public string HostingTimeDisplay => HostingTime.HasValue ? HostingTime.Value.ToString(@"hh\:mm") : "—";
        }

        public class CorrespondentRow(int id, int correspondentId, string fullName, string? topic, TimeSpan? hostingTime)
        {
            public int Id { get; set; } = id;
            public int CorrespondentId { get; set; } = correspondentId;
            public string FullName { get; set; } = fullName;
            public string? Topic { get; set; } = topic;
            public TimeSpan? HostingTime { get; set; } = hostingTime;
            public string HostingTimeDisplay => HostingTime.HasValue ? HostingTime.Value.ToString(@"hh\:mm") : "—";
        }

        public class EmployeeRow(int employeeId, string fullName, string staffRoleName)
        {
            public int EmployeeId { get; set; } = employeeId;
            public string FullName { get; set; } = fullName;
            public string StaffRoleName { get; set; } = staffRoleName;
        }
    }
}
