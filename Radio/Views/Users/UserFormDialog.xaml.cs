using DataAccess.DTOs;
using DataAccess.Services;
using Domain.Models;
using System.Windows;

namespace Radio.Views.Users
{
    /// <summary>
    /// Interaction logic for UserFormDialog.xaml
    /// </summary>
    public partial class UserFormDialog
    {
        private readonly IUserService _userService;
        private readonly UserSession _session;
        private readonly UserDto? _existing;

        public UserFormDialog(UserDto? existing, IUserService userService, UserSession session)
        {
            InitializeComponent();
            _existing = existing;
            _userService = userService;
            _session = session;

            _ = LoadRoles();

            if (_existing != null)
            {
                TxtTitle.Text = "تعديل مستخدم";
                TxtFullName.Text = _existing.FullName;
                TxtUsername.Text = _existing.Username;
                TxtUsername.IsEnabled = false; // لا يسمح بتغيير اسم المستخدم
                TxtPhone.Text = _existing.PhoneNumber;
                TxtEmail.Text = _existing.EmailAddress;
                TxtPwdHint.Visibility = Visibility.Visible;
                CboRoles.SelectedValue = _existing.RoleId;
            }
        }

        private async Task LoadRoles() => CboRoles.ItemsSource = await _userService.GetRolesAsync();

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtFullName.Text) || CboRoles.SelectedValue == null) return;

            var user = _existing ?? new UserDto();
            user.FullName = TxtFullName.Text;
            user.Username = TxtUsername.Text;
            user.RoleId = (int)CboRoles.SelectedValue;
            user.PhoneNumber = TxtPhone.Text;
            user.EmailAddress = TxtEmail.Text;

            try
            {
                if (_existing == null)
                {
                    if (string.IsNullOrWhiteSpace(TxtPassword.Password)) throw new Exception("كلمة المرور مطلوبة للمستخدم الجديد.");
                    await _userService.CreateUserAsync(user, TxtPassword.Password, _session);
                }
                else
                {
                    await _userService.UpdateUserAsync(user, TxtPassword.Password, _session);
                }
                DialogResult = true;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        /// <summary>
        /// استدعِ هذا عند فتح النافذة للتعديل
        /// </summary>
        public void SetEditMode()
        {
            // تغيير العنوان والأيقونة
            TxtTitle.Text = "تعديل بيانات المستخدم";
            TxtSubtitle.Text = "قم بتحديث البيانات المطلوبة ثم اضغط حفظ";

            // إظهار ملاحظة كلمة المرور
            PwdHintCard.Visibility = Visibility.Visible;

            // تغيير أيقونة الرأس
            // (يمكنك تغييرها برمجياً أو بإضافة أيقونة أخرى في XAML)
        }

        /// <summary>
        /// تأثير بسيط لإظهار خطأ في الحقل
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
