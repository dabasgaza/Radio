using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Security;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Domain.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Data.SqlClient;
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

            // ✨ التقاط أخطاء async void التي تحدث على مؤشرات خارج UI
            // بدون هذا المعالج، أي استثناء غير معالج في async void على مؤشر Thread Pool
            // سيرفع TaskScheduler.UnobservedTaskException ويُنهي التطبيق بصمت
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // ✨ التقاط أخطاء مؤشرات العمل غير التابعة لـ UI (non-UI threads)
            // يلتقط استثناءات من Thread.Start, Thread Pool, BackgroundWorker
            // التي لا يلتقطها DispatcherUnhandledException
            AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;

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
            // ✨ قراءة نص الاتصال الآمن — يدعم التشفير بـ DPAPI ومتغيرات البيئة
            var connectionString = SecureConfigurationProvider.GetSecureConnectionString(builder.Configuration);
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
            builder.Services.AddTransient<IEpisodeQueryService, EpisodeService>();
            builder.Services.AddTransient<IEpisodeCommandService, EpisodeService>();
            builder.Services.AddTransient<IProgramService, ProgramService>();
            builder.Services.AddTransient<IExecutionService, ExecutionService>();
            builder.Services.AddTransient<IPublishingService, PublishingService>();
            builder.Services.AddTransient<IPublishingQueryService, PublishingService>();
            builder.Services.AddTransient<IPublishingCommandService, PublishingService>();
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
            builder.Services.AddSingleton<DialogHelper>();
            builder.Services.AddTransient<LoginWindow>();
            builder.Services.AddTransient<ModernMainWindow>();

            AppHost = builder.Build();
            ServiceProvider = AppHost.Services;

            // ✅ تهيئة خدمة الرسائل المركزية للاستخدام في كافة أنحاء النظام
            MessageService.Initialize(ServiceProvider.GetRequiredService<IMessageService>());
        }

        public void EnsureLightTheme()
        {
            var theme = Current.Resources.MergedDictionaries.OfType<BundledTheme>().FirstOrDefault();
            if (theme != null)
            {
                theme.BaseTheme = BaseTheme.Light;
            }
        }

        /// <summary>
        /// ✨ تبديل الثيم بين الفاتح والداكن.
        /// يُحدث BundledTheme ويُبدّل فراشي الألوان السطحية ديناميكياً.
        /// </summary>
        public static void ToggleTheme()
        {
            var theme = Current.Resources.MergedDictionaries.OfType<BundledTheme>().FirstOrDefault();
            if (theme == null) return;

            var isDark = theme.BaseTheme == BaseTheme.Dark;
            theme.BaseTheme = isDark ? BaseTheme.Light : BaseTheme.Dark;

            // تحديث فراشي السطح والخلفية حسب الثيم
            var isNowDark = !isDark;
            Current.Resources["SurfaceBrush"] = new System.Windows.Media.SolidColorBrush(
                isNowDark ? System.Windows.Media.Color.FromRgb(0x1E, 0x29, 0x3B) : System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
            Current.Resources["AppBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(
                isNowDark ? System.Windows.Media.Color.FromRgb(0x0F, 0x17, 0x2A) : System.Windows.Media.Color.FromRgb(0xF8, 0xFA, 0xFC));
            Current.Resources["SurfaceVariantBrush"] = new System.Windows.Media.SolidColorBrush(
                isNowDark ? System.Windows.Media.Color.FromRgb(0x33, 0x41, 0x55) : System.Windows.Media.Color.FromRgb(0xF1, 0xF5, 0xF9));
            Current.Resources["DividerBrush"] = new System.Windows.Media.SolidColorBrush(
                isNowDark ? System.Windows.Media.Color.FromRgb(0x33, 0x41, 0x55) : System.Windows.Media.Color.FromRgb(0xE2, 0xE8, 0xF0));
            Current.Resources["CardBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(
                isNowDark ? System.Windows.Media.Color.FromRgb(0x1E, 0x29, 0x3B) : System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
        }

        /// <summary>
        /// ✨ الثيم الحالي — هل هو داكن؟
        /// </summary>
        public static bool IsDarkTheme
        {
            get
            {
                var theme = Current.Resources.MergedDictionaries.OfType<BundledTheme>().FirstOrDefault();
                return theme?.BaseTheme == BaseTheme.Dark;
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

                // ✨ عرض نافذة الخطأ المخصصة بدلاً من MessageBox التقليدية
                // في هذه المرحلة (قبل فتح النافذة الرئيسية)، نظام الإشعارات Toast غير متاح
                // لأن NotificationHost لم يتم تسجيله بعد، لذلك نستخدم SystemErrorWindow
                var errorWindow = new SystemErrorWindow(ex);
                errorWindow.ShowDialog();

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

            // ✨ تصنيف الخطأ: هل يمكن المتابعة أم يجب إغلاق التطبيق؟
            var isRecoverable = IsRecoverableException(e.Exception);

            if (isRecoverable)
            {
                // ✅ أخطاء قابلة للاستعادة — عرض إشعار Toast للمستخدم بدلاً من نافذة خطأ
                try
                {
                    var userMessage = GetUserFriendlyMessage(e.Exception);
                    MessageService.Current.ShowError(userMessage, "خطأ في العملية");
                }
                catch
                {
                    // إذا فشل العرض عبر MessageService، نحاول MessageBox
                    try { MessageBox.Show(GetUserFriendlyMessage(e.Exception), "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
                    catch { /* تجاهل — لا نهيّج المستخدم أكثر */ }
                }

                // إعادة تهيئة Logger إن لزم
                ReinitializeLogger();
            }
            else
            {
                // ✅ أخطاء حرجة غير قابلة للاستعادة — عرض نافذة الخطأ المخصصة
                try
                {
                    // إجبار Serilog على كتابة السجلات للقرص فوراً
                    Log.CloseAndFlush();

                    var errorWindow = new SystemErrorWindow(e.Exception);
                    errorWindow.ShowDialog();

                    // إعادة تهيئة Logger في حال قرر المستخدم "متابعة العمل" كي يستمر التسجيل
                    ReinitializeLogger();
                }
                catch (Exception fallbackEx)
                {
                    // ✨ إذا فشلت نافذة الخطأ نفسها، نستخدم MessageBox كحل أخير
                    try
                    {
                        Log.Error(fallbackEx, "فشل عرض نافذة الخطأ المخصصة");
                        Log.CloseAndFlush();
                        MessageBox.Show(
                            $"حدث خطأ حرج في التطبيق وتعذر عرض نافذة الخطأ المخصصة.\n\n{e.Exception?.Message}",
                            "خطأ حرج",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    catch { /* تجاهل — لا شيء يمكن فعله */ }
                }
            }

            // منع إغلاق التطبيق المفاجئ
            e.Handled = true;
        }

        /// <summary>
        /// ✨ معالج أخطاء async void على مؤشرات خارج UI (Thread Pool).
        /// بدون هذا المعالج، أي استثناء غير معالج في async void على Thread Pool
        /// يُنهي التطبيق بصمت في .NET 10. هذا يلتقط الخطأ، يسجله، ويمنع الانهيار.
        /// </summary>
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            // تسجيل الخطأ مع السياق الكامل
            Log.Error(e.Exception, "خطأ غير معالج في عملية غير متزامنة (UnobservedTaskException): {Message}",
                e.Exception?.InnerException?.Message ?? e.Exception?.Message ?? "غير معروف");

            // ✨ عرض رسالة تحذير عبر نظام الإشعارات المركزي بدلاً من MessageBox التقليدية
            // NotificationManager يتحقق من Dispatcher داخلياً
            try
            {
                var innerMessage = e.Exception?.InnerException?.Message ?? e.Exception?.Message ?? "خطأ غير معروف";
                MessageService.Current.ShowWarning(
                    $"حدث خطأ أثناء تنفيذ عملية في الخلفية:\n\n{innerMessage}",
                    "خطأ في العملية");
            }
            catch
            {
                // إذا فشل العرض، سجّله فقط — لا تُنهِ التطبيق
                Log.Warning("فشل إظهار رسالة خطأ UnobservedTaskException للمستخدم");
            }

            // ✨ منع انهيار التطبيق — تعليم الاستثناء كمعالج
            e.SetObserved();
        }

        /// <summary>
        /// ✨ معالج أخطاء مؤشرات العمل غير التابعة لـ UI (non-UI threads).
        /// يلتقط استثناءات من Thread.Start, Thread Pool, BackgroundWorker
        /// التي لا يلتقطها DispatcherUnhandledException.
        /// ملاحظة: لا يمكن منع إنهاء التطبيق في هذه الحالة، لكن يمكن تسجيل الخطأ
        /// وإظهار رسالة للمستخدم قبل الإغلاق.
        /// </summary>
        private void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;

            // تسجيل الخطأ كحالة حرجة
            Log.Fatal(exception, "خطأ حرج غير معالج على مؤشر عمل خارجي (AppDomain.UnhandledException). هل سيُنهي التطبيق: {IsTerminating}",
                e.IsTerminating);

            // محاولة إظهار رسالة للمستخدم عبر MessageService
            try
            {
                var message = exception != null
                    ? GetUserFriendlyMessage(exception)
                    : "حدث خطأ حرج غير معروف في مؤشر عمل خلفي.";

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        MessageService.Current.ShowError(message, "خطأ حرج");
                    }
                    catch
                    {
                        // Fallback إلى MessageBox
                        try { MessageBox.Show(message, "خطأ حرج", MessageBoxButton.OK, MessageBoxImage.Error); }
                        catch { /* تجاهل */ }
                    }
                });
            }
            catch
            {
                // إذا لم نستطع عرض أي رسالة، سجّل فقط
                Log.Warning("فشل إظهار رسالة خطأ AppDomain.UnhandledException للمستخدم");
            }

            // إجبار كتابة السجلات فوراً
            try { Log.CloseAndFlush(); } catch { /* تجاهل */ }
        }

        /// <summary>
        /// ✨ تصنيف الاستثناء: هل يمكن المتابعة بعد هذا الخطأ أم أنه حرج؟
        /// الأخطاء القابلة للاستعادة (Recoverable) تعرض كـ Toast إشعار فقط.
        /// الأخطاء الحرجة (Non-Recoverable) تعرض في نافذة SystemErrorWindow.
        /// </summary>
        private static bool IsRecoverableException(Exception? exception)
        {
            if (exception is null) return false;

            // ✅ أخطاء Owner/ShowDialog — قابلة للاستعادة تماماً
            if (exception is InvalidOperationException &&
                exception.Message.Contains("Owner", StringComparison.OrdinalIgnoreCase))
                return true;

            // ✅ أخطاء تعيين Owner لنافذة في Thread آخر — قابلة للاستعادة
            if (exception is ArgumentException &&
                exception.Message.Contains("Owner", StringComparison.OrdinalIgnoreCase))
                return true;

            // ✅ أخطاء قاعدة البيانات العابرة — قابلة للاستعادة (المستخدم يمكنه المحاولة مرة أخرى)
            if (exception is DbUpdateConcurrencyException)
                return true;

            if (exception is DbUpdateException)
                return true;

            // ✅ أخطاء الاتصال بالخادم — قابلة للاستعادة
            if (exception is SqlException)
                return true;

            // ✅ Timeout — قابل للاستعادة
            if (exception is TimeoutException)
                return true;

            // ✅ أخطاء التحقق من الصحة — قابلة للاستعادة
            if (exception is InvalidOperationException &&
                (exception.Message.Contains("validation", StringComparison.OrdinalIgnoreCase) ||
                 exception.Message.Contains("تحقق", StringComparison.OrdinalIgnoreCase)))
                return true;

            // ✅ أخطاءcancel — قابلة للاستعادة
            if (exception is OperationCanceledException)
                return true;

            // باقي الأخطاء تعتبر حرجة
            return false;
        }

        /// <summary>
        /// ✨ تحويل رسالة الخطأ التقنية إلى رسالة مفهومة للمستخدم.
        /// بدلاً من عرض StackTrace كامل، يُعرض وصف مختصر بلغة المستخدم.
        /// </summary>
        private static string GetUserFriendlyMessage(Exception exception)
        {
            if (exception is null)
                return "حدث خطأ غير معروف.";

            // أخطاء Owner/ShowDialog
            if (exception is InvalidOperationException &&
                exception.Message.Contains("Owner", StringComparison.OrdinalIgnoreCase))
                return "تعذر فتح النافذة. يرجى المحاولة مرة أخرى.";

            // أخطاء Owner في Thread آخر
            if (exception is ArgumentException &&
                exception.Message.Contains("Owner", StringComparison.OrdinalIgnoreCase))
                return "تعذر فتح النافذة بسبب تعارض في مؤشرات التنفيذ. يرجى المحاولة مرة أخرى.";

            // أخطاء قاعدة البيانات
            if (exception is DbUpdateConcurrencyException)
                return "تم تعديل البيانات بواسطة مستخدم آخر. يرجى إعادة تحميل الصفحة والمحاولة مرة أخرى.";

            if (exception is DbUpdateException dbEx)
                return $"حدث خطأ أثناء حفظ البيانات في قاعدة البيانات.\n{dbEx.InnerException?.Message ?? dbEx.Message}";

            // أخطاء SQL Server
            if (exception is SqlException)
            {
                var sqlMessage = exception.Message;
                if (sqlMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                    sqlMessage.Contains("انتهت المهلة", StringComparison.OrdinalIgnoreCase))
                    return "انتهت مهلة الاتصال بقاعدة البيانات. يرجى المحاولة مرة أخرى.";
                if (sqlMessage.Contains("cannot connect", StringComparison.OrdinalIgnoreCase) ||
                    sqlMessage.Contains("تعذر الاتصال", StringComparison.OrdinalIgnoreCase))
                    return "تعذر الاتصال بقاعدة البيانات. تأكد من أن الخادم يعمل.";
                return $"حدث خطأ في قاعدة البيانات.\n{sqlMessage}";
            }

            // Timeout
            if (exception is TimeoutException)
                return "انتهت مهلة العملية. يرجى المحاولة مرة أخرى.";

            // OperationCanceled
            if (exception is OperationCanceledException)
                return "تم إلغاء العملية.";

            // أخطاء عامة
            return $"حدث خطأ غير متوقع.\n{exception.Message}";
        }

        private void ReinitializeLogger()
        {
            try
            {
                if (AppHost != null)
                {
                    var config = AppHost.Services.GetService<IConfiguration>();
                    if (config != null)
                    {
                        var seqUrl = config["Seq:ServerUrl"] ?? "http://localhost:5341";
                        var apiKey = config["Seq:ApiKey"] ?? string.Empty;

                        Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Information()
                            .Enrich.FromLogContext()
                            .WriteTo.Console()
                            .WriteTo.File("logs/radio.log", rollingInterval: RollingInterval.Day)
                            .WriteTo.Seq(seqUrl, apiKey: string.IsNullOrEmpty(apiKey) ? null : apiKey)
                            .CreateLogger();
                        return;
                    }
                }
            }
            catch
            {
                // تجاهل أخطاء إعادة التهيئة
            }

            // Fallback basic logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File("logs/radio.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }
}
