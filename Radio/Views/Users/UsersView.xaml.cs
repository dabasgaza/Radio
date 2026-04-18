using DataAccess.Services;
using DataAccess.Services.Messaging;
using Domain.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Radio.Views.Users
{
    /// <summary>
    /// Interaction logic for UsersView.xaml
    /// </summary>
    public partial class UsersView : UserControl
    {
        private readonly IUserService _userService;
        private readonly UserSession _session;

        public UsersView(IUserService userService, UserSession session)
        {
            InitializeComponent();
            _userService = userService;
            _session = session;

            // فقط "المسؤول" يمكنه إضافة مستخدمين
            BtnAddUser.Visibility = _session.IsAdmin ? Visibility.Visible : Visibility.Collapsed;

            _ = LoadDataAsync();
        }

        /// <summary>
        /// تحميل قائمة المستخدمين من قاعدة البيانات
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                DgUsers.ItemsSource = users;
            }
            catch (Exception ex)
            {
                // استخدام خدمة الرسائل بدلاً من MessageBox
                MessageService.Current.ShowError("خطأ في تحميل قائمة المستخدمين: " + ex.Message);
            }
        }

        /// <summary>
        /// فتح نافذة إضافة مستخدم جديد
        /// </summary>
        private async void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserFormDialog(null, _userService, _session);
            if (dialog.ShowDialog() == true)
            {
                await LoadDataAsync();
                UpdateStats();
            }
        }

        /// <summary>
        /// فتح نافذة تعديل بيانات مستخدم موجود
        /// </summary>
        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is User user)
            {
                var dialog = new UserFormDialog(user, _userService, _session);
                if (dialog.ShowDialog() == true)
                {
                    await LoadDataAsync();
                    UpdateStats();
                }
            }
        }

        /// <summary>
        /// معالجة تغيير حالة المستخدم (تفعيل/تعطيل) من الـ ToggleButton
        /// </summary>
        private async void Status_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggle && toggle.DataContext is User user)
            {
                bool newStatus = toggle.IsChecked ?? false;

                try
                {
                    // محاولة تغيير الحالة عبر الخدمة
                    await _userService.ToggleUserStatusAsync(user.UserId, newStatus, _session);
                }
                catch (Exception ex)
                {
                    // في حال فشل العملية (مثلاً محاولة تعطيل حسابه الشخصي)
                    MessageService.Current.ShowWarning("تنبيه: " + ex.Message);

                    // إعادة الزر لحالته الأصلية لأن العملية فشلت
                    toggle.IsChecked = !newStatus;
                }
                finally
                {
                    // تحديث القائمة لضمان مزامنة البيانات
                    await LoadDataAsync();
                }
            }
        }

        // تحديث الإحصائيات بعد كل عملية
        private void UpdateStats()
        {
            var items = DgUsers.ItemsSource;
            if (items != null)
            {
                var list = items.Cast<dynamic>().ToList();
                TxtTotalUsers.Text = list.Count.ToString();
                TxtActiveUsers.Text = list.Count(u => u.IsActive == true).ToString();
                TxtInactiveUsers.Text = list.Count(u => u.IsActive == false).ToString();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {

        }
    }

}
