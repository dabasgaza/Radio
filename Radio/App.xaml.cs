using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Domain.Models;
using MaterialDesignColors;
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
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                ContentRootPath = AppContext.BaseDirectory
            });

            // Configure Serilog with Seq
            var seqUrl = builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341";
            var apiKey = builder.Configuration["Seq:ApiKey"] ?? "";
            var slowQueryThreshold = builder.Configuration.GetValue<int>("Seq:SlowQueryThresholdMs", 100);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console()
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
                options.UseSqlServer(connectionString)
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

        protected override void OnStartup(StartupEventArgs e)
        {
            var customCulture = new System.Globalization.CultureInfo("en-US");
            customCulture.DateTimeFormat.ShortDatePattern = "dd-MM-yyyy";
            customCulture.DateTimeFormat.DateSeparator = "-";

            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = customCulture;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    System.Windows.Markup.XmlLanguage.GetLanguage("en-US")));

            base.OnStartup(e);

            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await AppHost.StopAsync();
            base.OnExit(e);
        }
    }
}
