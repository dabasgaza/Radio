using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Domain.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Radio.Forms;
using Radio.Messaging;
using Radio.Services;
using Serilog;
using System.Windows;
using System.Windows.Threading;

namespace Radio
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        public static IHost AppHost { get; private set; } = null!;

        public App()
        {
            var customCulture = new System.Globalization.CultureInfo("en-GB");
            customCulture.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";
            customCulture.DateTimeFormat.DateSeparator = "-";

            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = customCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = customCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = customCulture;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    System.Windows.Markup.XmlLanguage.GetLanguage("en-GB")));

            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                ContentRootPath = AppContext.BaseDirectory
            });

            // Configure Serilog with Seq
            var seqUrl = builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341";
            var apiKey = builder.Configuration["Seq:ApiKey"] ?? string.Empty;
            var slowQueryThreshold = builder.Configuration.GetValue<int>("Seq:SlowQueryThresholdMs", 100);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/radio.log", rollingInterval: RollingInterval.Day)
                .WriteTo.Seq(seqUrl, apiKey: string.IsNullOrEmpty(apiKey) ? null : apiKey)
                .CreateLogger();

            builder.Services.AddSerilog();

            // 1. Database Context Factory
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
            builder.Services.AddSingleton(sp => new DbQueryPerformanceInterceptor(slowQueryThreshold));
            builder.Services.AddDbContextFactory<BroadcastWorkflowDBContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditInterceptor>();
                var perfInterceptor = sp.GetRequiredService<DbQueryPerformanceInterceptor>();
                options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure())
                       .AddInterceptors(interceptor, perfInterceptor);
            });

            // 2. Infrastructure
            builder.Services.AddSingleton<CurrentSessionProvider>();
            builder.Services.AddSingleton<AuditInterceptor>();
            builder.Services.AddSingleton<IMessageService, WpfMessageService>();

            // Application Insights
            var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
            if (!string.IsNullOrEmpty(appInsightsConnectionString))
            {
                telemetryConfiguration.ConnectionString = appInsightsConnectionString;
            }
            builder.Services.AddSingleton(telemetryConfiguration);
            builder.Services.AddSingleton<TelemetryClient>();

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
            builder.Services.AddSingleton<ICachedLookupService, CachedLookupService>();
            builder.Services.AddTransient<IEmployeeService, EmployeeService>();
            builder.Services.AddTransient<IPlatformService, PlatformService>();
            builder.Services.AddTransient<IPermissionService, PermissionService>();
            builder.Services.AddTransient<IDatabaseManagementService, DatabaseManagementService>();
            builder.Services.AddTransient<IAuditLogService, AuditLogService>();
            builder.Services.AddTransient<ISystemDiagnosticsService, SystemDiagnosticsService>();
            builder.Services.AddHostedService<DatabaseBackupScheduler>();

            // 4. Caching
            builder.Services.AddMemoryCache();

            // 5. UI
            builder.Services.AddSingleton<NavigationService>();
            builder.Services.AddTransient<LoginWindow>();
            builder.Services.AddTransient<ModernMainWindow>();

            AppHost = builder.Build();
            ServiceProvider = AppHost.Services;

            // ✅ تهيئة خدمة الرسائل المركزية للاستخدام في كافة أنحاء النظام
            MessageService.Initialize(ServiceProvider.GetRequiredService<IMessageService>());
        }

        public void SwitchTheme(bool isDark)
        {
            var theme = Current.Resources.MergedDictionaries.OfType<BundledTheme>().FirstOrDefault();
            if (theme != null)
            {
                theme.BaseTheme = isDark ? BaseTheme.Dark : BaseTheme.Light;
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            FontScaleService.Initialize();

            try
            {
                var dbFactory = ServiceProvider.GetRequiredService<IDbContextFactory<BroadcastWorkflowDBContext>>();

                using (var context = await dbFactory.CreateDbContextAsync())
                {
                    await context.Database.MigrateAsync();
                }

                await DataAccess.Seeding.DbSeeder.SeedAsync(dbFactory);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل تهيئة قاعدة البيانات.");
                MessageBox.Show(
                    $"تعذر إنشاء قاعدة البيانات أو تهيئتها. يرجى التحقق من اتصال SQL Server.\n\n{ex.Message}",
                    "خطأ في قاعدة البيانات",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }

            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await AppHost.StopAsync();
            base.OnExit(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // تسجيل الخطأ كحالة حرجة في السجل مع استثناء التفاصيل كاملة
            Log.Fatal(e.Exception, "حدث خطأ غير متوقع لم يتم معالجته في التطبيق.");

            // عرض رسالة ودية للمستخدم باللغة العربية لمواصلة العمل دون انهيار التطبيق مفاجئاً
            MessageBox.Show(
                $"نعتذر عن هذا الخطأ غير المتوقع في النظام.\n\nتفاصيل الخطأ: {e.Exception.Message}\n\nيمكنك الاستمرار في استخدام التطبيق، وإذا استمرت المشكلة، يرجى التواصل مع الدعم الفني.",
                "خطأ غير متوقع في النظام",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            // منع إغلاق التطبيق المفاجئ
            e.Handled = true;
        }
    }
}
