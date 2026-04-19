using DataAccess.Services;
using DataAccess.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace Radio.Forms
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
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
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text.Trim();
            string password = TxtPassword.Password;

            // التحقق المبدئي من الصيغة (UI Validation)
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageService.Current.ShowWarning("يرجى إدخال اسم المستخدم وكلمة المرور.");
                return;
            }

            SetLoading(true);

            try
            {
                // استدعاء الـ Service (النجاح يعني عدم رمي استثناء)
                var session = await _authService.LoginAsync(username, password);

                // إذا وصلنا هنا،意味着 النجاح
                MessageService.Current.ShowSuccess($"مرحباً بك، {session.Username}");

                var reportsService = _serviceProvider.GetRequiredService<IReportsService>();
                var mainWindow = new MainWindow(session, _serviceProvider, reportsService);
                mainWindow.Show();
                this.Close();
            }
            catch (UnauthorizedAccessException)
            {
                // خطأ في بيانات الدخول (رمزتها الخلفية كـ 401)
                MessageService.Current.ShowError("اسم المستخدم أو كلمة المرور غير صحيحة.");
            }
            catch (InvalidOperationException ex)
            {
                // خطأ في قواعد العمل (مثلاً: الحساب معطل)
                MessageService.Current.ShowWarning(ex.Message);
            }
            catch (Exception)
            {
                // خطأ نظام عام (مشكلة شبكة، قاعدة بيانات، إلخ)
                MessageService.Current.ShowError("حدث خطأ أثناء الاتصال بالخادم. يرجى المحاولة لاحقاً.");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void SetLoading(bool isLoading)
        {
            BtnLogin.IsEnabled = !isLoading;
            TxtUsername.IsEnabled = !isLoading;
            TxtPassword.IsEnabled = !isLoading;
            LoginProgress.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
