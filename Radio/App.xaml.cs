using BroadcastWorkflow.Services;
using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Radio.Forms;
using System.IO;
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

        public App()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 1. Database Context Factory
            string connectionString = Configuration.GetConnectionString("DefaultConnection")!;
            services.AddDbContextFactory<BroadcastWorkflowDBContext>((serviceProvider, options) =>
            {
                var interceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
                options.UseSqlServer(connectionString)
                       .AddInterceptors(interceptor); // 👈 ربط المعترض
            });

            // 2. Services (To be implemented in Phase 3)
            services.AddTransient<IAuditService, AuditService>();
            services.AddTransient<IAuthService, AuthService>();
            services.AddTransient<IGuestService, GuestService>();
            services.AddTransient<ICorrespondentService, CorrespondentService>();
            services.AddTransient<IEpisodeService, EpisodeService>();
            services.AddTransient<IProgramService, ProgramService>();
            services.AddTransient<IExecutionService, ExecutionService>();
            services.AddTransient<IPublishingService, PublishingService>();
            services.AddTransient<IReportsService, ReportsService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<ICoverageService, CoverageService>();

            // تسجيل مزود الجلسة ليكون متاحاً في كل مكان
            services.AddSingleton<CurrentSessionProvider>();
            services.AddSingleton<AuditInterceptor>();


            // 3. Windows (To be implemented in Phase 4+)
            services.AddTransient<MainWindow>();
            services.AddTransient<LoginWindow>();
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

            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
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


    }

}
