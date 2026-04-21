using DataAccess.Services;
using DataAccess.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace Radio.Forms
{
    /// <summary>
    /// نافذة تسجيل الدخول — تتحقق من بيانات المستخدم وتفتح النافذة الرئيسية عند النجاح.
    /// </summary>
    public partial class LoginWindow
    {
        private readonly IAuthService _authService;
        private readonly IServiceProvider _serviceProvider;

        public LoginWindow(IAuthService authService, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _authService = authService;
            _serviceProvider = serviceProvider;

            // ✅ الضغط على Enter في حقل كلمة المرور = تسجيل دخول
            TxtPassword.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                    BtnLogin_Click(s, e);
            };

            TxtUsername.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                    TxtPassword.Focus();
            };
        }

        /// <summary>
        /// محاولة تسجيل الدخول بالبيانات المدخلة.
        /// </summary>
        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text.Trim();
            string password = TxtPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageService.Current.ShowWarning("يرجى إدخال اسم المستخدم وكلمة المرور.");
                return;
            }

            SetLoading(true);

            try
            {
                var session = await _authService.LoginAsync(username, password);

                // ✅ فحص أمان — إذا الـ Service يُرجع null بدلاً من رمي استثناء
                if (session is null)
                {
                    MessageService.Current.ShowError("اسم المستخدم أو كلمة المرور غير صحيحة.");
                    TxtPassword.Focus();
                    TxtPassword.SelectAll();
                    return;
                }

                MessageService.Current.ShowSuccess($"مرحباً بك، {session.FullName}");

                var reportsService = _serviceProvider.GetRequiredService<IReportsService>();
                var mainWindow = new MainWindow(session, _serviceProvider, reportsService);
                mainWindow.Show();
                Close();
            }
            catch (UnauthorizedAccessException)
            {
                MessageService.Current.ShowError("اسم المستخدم أو كلمة المرور غير صحيحة.");
                TxtPassword.Focus();
                TxtPassword.SelectAll();
            }
            catch (InvalidOperationException ex)
            {
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                MessageService.Current.ShowError("حدث خطأ أثناء الاتصال بالخادم. يرجى المحاولة لاحقاً.");
            }
            finally
            {
                SetLoading(false);
            }
        }

        /// <summary>
        /// تبديل حالة التحميل — تعطيل الحقول والأزرار أثناء العملية.
        /// </summary>
        private void SetLoading(bool isLoading)
        {
            BtnLogin.IsEnabled = !isLoading;
            TxtUsername.IsEnabled = !isLoading;
            TxtPassword.IsEnabled = !isLoading;
            LoginProgress.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}