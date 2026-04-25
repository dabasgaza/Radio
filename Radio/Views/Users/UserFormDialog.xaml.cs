using DataAccess.DTOs;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using DataAccess.Validation;
using System.ComponentModel.DataAnnotations;
using System.Windows;

namespace Radio.Views.Users
{
    /// <summary>
    /// نافذة إضافة/تعديل مستخدم — تتولى جمع البيانات والتحقق منها ثم إرسالها لـ UserService.
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

            IsWindowDraggable = true;

            // ✅ عنوان ديناميكي حسب نوع العملية
            Title = _existing is not null ? "تعديل بيانات المستخدم" : "إضافة مستخدم جديد";

            // ✅ تعبئة الحقول في حالة التعديل
            if (_existing is not null)
            {
                TxtFullName.Text = _existing.FullName;
                TxtUsername.Text = _existing.Username;
                TxtUsername.IsEnabled = false;
                TxtPhone.Text = _existing.PhoneNumber;
                TxtEmail.Text = _existing.EmailAddress;
                TxtPwdHint.Visibility = Visibility.Visible;
                CboRoles.SelectedValue = _existing.RoleId;
            }

            // ✅ Loaded بدلاً من Fire-and-Forget
            Loaded += async (_, _) => await LoadRolesAsync();
        }

        #region Data Loading

        /// <summary>
        /// تحميل قائمة الأدوار لملء الـ ComboBox.
        /// </summary>
        private async Task LoadRolesAsync()
        {
            try
            {
                CboRoles.ItemsSource = await _userService.GetRolesAsync();
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError("ليس لديك صلاحية لعرض قائمة الأدوار.");
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء تحميل الأدوار.");
            }
        }

        #endregion

        #region Save

        /// <summary>
        /// حفظ المستخدم (إضافة أو تعديل) بعد التحقق من صحة المدخلات.
        /// </summary>
        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var dto = _existing ?? new UserDto();
            dto.FullName = TxtFullName.Text.Trim();
            dto.Username = TxtUsername.Text.Trim();
            dto.RoleId = CboRoles.SelectedValue is int roleId ? roleId : 0;
            dto.PhoneNumber = TxtPhone.Text.Trim();
            dto.EmailAddress = TxtEmail.Text.Trim();

            try
            {
                // ✅ التحقق عبر ValidationPipeline
                ValidationPipeline.ValidateUser(dto, TxtPassword.Password);

                BtnSave.IsEnabled = false;

                if (_existing is null)
                {
                    await _userService.CreateUserAsync(dto, TxtPassword.Password, _session);
                    MessageService.Current.ShowSuccess("تمت إضافة المستخدم بنجاح.");
                }
                else
                {
                    await _userService.UpdateUserAsync(dto, TxtPassword.Password, _session);
                    MessageService.Current.ShowSuccess("تم تعديل بيانات المستخدم بنجاح.");
                }

                DialogResult = true;
            }
            catch (ValidationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError(
                    _existing is null
                        ? "ليس لديك صلاحية لإضافة مستخدم جديد."
                        : "ليس لديك صلاحية لتعديل بيانات المستخدمين.");
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ غير متوقع أثناء حفظ بيانات المستخدم.");
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }

        #endregion

        #region UI Events

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                DragMove();
        }

        #endregion

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}