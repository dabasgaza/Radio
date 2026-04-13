using BroadcastWorkflow.Services;
using System.Windows;

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

            // UI Validation
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("يرجى إدخال اسم المستخدم وكلمة المرور.");
                return;
            }

            SetLoading(true);

            try
            {
                var session = await _authService.LoginAsync(username, password);

                if (session != null)
                {
                    // Login Success: Open MainWindow and pass the session
                    var mainWindow = new MainWindow(session, _serviceProvider);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    ShowError("اسم المستخدم أو كلمة المرور غير صحيحة.");
                }
            }
            catch (Exception)
            {
                ShowError("حدث خطأ أثناء الاتصال بالخادم. يرجى المحاولة لاحقاً.");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void ShowError(string message)
        {
            LblError.Text = message;
            LblError.Visibility = Visibility.Visible;
        }

        private void SetLoading(bool isLoading)
        {
            BtnLogin.IsEnabled = !isLoading;
            TxtUsername.IsEnabled = !isLoading;
            TxtPassword.IsEnabled = !isLoading;
            LoginProgress.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            LblError.Visibility = Visibility.Collapsed;
        }
    }
}
