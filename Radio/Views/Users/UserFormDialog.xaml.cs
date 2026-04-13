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
        private readonly User? _existing;

        public UserFormDialog(User? existing, IUserService userService, UserSession session)
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

            var user = _existing ?? new User();
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

    }
}
