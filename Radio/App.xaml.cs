using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Domain.Models;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Radio.Forms;
using System.Windows;
using System.Windows.Threading;

namespace Radio
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        public IConfiguration Configuration { get; private set; } = null!;
        public static IHost AppHost { get; private set; } = null!;

        public App()
        {
            // ✅ النهج الحديث في .NET 10 (بدلاً من CreateDefaultBuilder)
            var builder = Host.CreateApplicationBuilder();

            // 1. Database Context Factory (EF Core 10)
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
            builder.Services.AddDbContextFactory<BroadcastWorkflowDBContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditInterceptor>();
                options.UseSqlServer(connectionString)
                       .AddInterceptors(interceptor);
            });

            // 2. Infrastructure
            builder.Services.AddSingleton<CurrentSessionProvider>();
            builder.Services.AddSingleton<AuditInterceptor>();

            // 3. Application Services
            builder.Services.AddTransient<IAuthService, AuthService>();
            builder.Services.AddTransient<IGuestService, GuestService>();
            builder.Services.AddTransient<ICorrespondentService, CorrespondentService>();
            builder.Services.AddTransient<IEpisodeService, EpisodeService>();
            builder.Services.AddTransient<IProgramService, ProgramService>();
            builder.Services.AddTransient<IExecutionService, ExecutionService>();
            builder.Services.AddTransient<IPublishingService, PublishingService>();
            builder.Services.AddTransient<IReportsService, ReportsService>();
            builder.Services.AddTransient<IUserService, UserService>();
            builder.Services.AddTransient<ICoverageService, CoverageService>();
            // ... باقي الخدمات ...

            // 4. UI
            builder.Services.AddTransient<LoginWindow>();
            builder.Services.AddTransient<MainWindow>();


            AppHost = builder.Build();
        }

        public void SwitchTheme(bool isDark)
        {
            // البحث عن كائن الثيم المدمج
            var theme = Current.Resources.MergedDictionaries.OfType<BundledTheme>().FirstOrDefault();
            if (theme != null)
            {
                // تغيير الوضع الأساسي
                theme.BaseTheme = isDark ? BaseTheme.Dark : BaseTheme.Light;

                // تغيير الألوان لتناسب المواصفات بدقة
                theme.PrimaryColor = isDark ? PrimaryColor.Indigo : PrimaryColor.Indigo;
                theme.SecondaryColor = isDark ? SecondaryColor.LightBlue : SecondaryColor.LightBlue;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // For now, we'll just show a placeholder or the first window when ready
            //var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            //mainWindow.Show();

            // 1. اصطياد أخطاء واجهة المستخدم (UI Thread Exceptions)
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // 2. اصطياد أخطاء المهام الخلفية (Background Task Exceptions)
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var loginWindow = AppHost.Services.GetRequiredService<LoginWindow>();
            loginWindow.Show();

            base.OnStartup(e);
        }

        /// <summary>
        /// يتم استدعاؤها عند حدوث خطأ مفاجئ في واجهة المستخدم لمنع انهيار البرنامج
        /// </summary>
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageService.Current.ShowError($"حدث خطأ في النظام: {e.Exception?.InnerException?.Message}");

            // إخبار النظام بأننا تعاملنا مع الخطأ، فلا تقم بإغلاق البرنامج
            e.Handled = true;
        }

        /// <summary>
        /// يتم استدعاؤها عند حدوث خطأ في مهام الـ Async التي لم يتم عمل await لها
        /// </summary>
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            MessageService.Current.ShowError($"خطأ خلفي: {e.Exception?.InnerException?.Message}");
            e.SetObserved();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // TODO: Log using ILogger
        }

    }

}
