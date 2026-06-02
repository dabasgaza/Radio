using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public class DatabaseBackupScheduler : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseBackupScheduler> _logger;
        private readonly IConfiguration _configuration;

        public DatabaseBackupScheduler(
            IServiceProvider serviceProvider, 
            ILogger<DatabaseBackupScheduler> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("خدمة جدولة النسخ الاحتياطي لقاعدة البيانات قد بدأت.");

            // Run database initialization on startup
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbService = scope.ServiceProvider.GetRequiredService<IDatabaseManagementService>();
                    await dbService.InitializeDatabaseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تهيئة قاعدة البيانات في بداية تشغيل الخدمة.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Reload configuration values
                    var enabled = _configuration.GetValue<bool>("DatabaseBackup:Enabled", false);
                    var intervalHours = _configuration.GetValue<double>("DatabaseBackup:IntervalHours", 24);
                    var retentionDays = _configuration.GetValue<int>("DatabaseBackup:RetentionDays", 30);
                    var cloudSync = _configuration.GetValue<bool>("DatabaseBackup:CloudSync", false);

                    if (enabled)
                    {
                        _logger.LogInformation("بدء عملية النسخ الاحتياطي المجدول...");
                        
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var dbService = scope.ServiceProvider.GetRequiredService<IDatabaseManagementService>();
                                                        // Perform local backup
                             var backupResult = await dbService.BackupDatabaseAsync();
                             if (backupResult.IsSuccess && backupResult.Value != null)
                             {
                                 _logger.LogInformation($"تم إنشاء النسخة الاحتياطية المجدولة بنجاح: {backupResult.Value}");
                                 
                                 // Cloud sync if enabled
                                 if (cloudSync)
                                 {
                                     var cloudResult = await dbService.CloudSyncBackupAsync(backupResult.Value);
                                     if (cloudResult.IsSuccess)
                                     {
                                         _logger.LogInformation("تمت مزامنة النسخة الاحتياطية السحابية بنجاح.");
                                     }
                                     else
                                     {
                                         _logger.LogWarning($"فشلت المزامنة السحابية المجدولة: {cloudResult.ErrorMessage}");
                                     }
                                 }

                                 // Run retention policy
                                 var retentionResult = await dbService.RunRetentionPolicyAsync(retentionDays);
                                 if (!retentionResult.IsSuccess)
                                 {
                                     _logger.LogWarning($"فشل تطبيق سياسة الاحتفاظ بالملفات: {retentionResult.ErrorMessage}");
                                 }
                             }
                             else
                             {
                                 _logger.LogError($"فشل النسخ الاحتياطي المجدول: {backupResult.ErrorMessage}");
                             }
                        }
                    }

                    // Sleep for the configured interval or check cancellation token frequently
                    var delayTime = TimeSpan.FromHours(intervalHours);
                    if (delayTime <= TimeSpan.Zero) delayTime = TimeSpan.FromHours(24);

                    // To respond quickly to stopping, we sleep in shorter increments (e.g. 5 minutes)
                    var totalSlept = TimeSpan.Zero;
                    var sleepChunk = TimeSpan.FromMinutes(5);
                    
                    while (totalSlept < delayTime && !stoppingToken.IsCancellationRequested)
                    {
                        await Task.Delay(sleepChunk, stoppingToken);
                        totalSlept += sleepChunk;
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ غير متوقع في خدمة جدولة النسخ الاحتياطي.");
                    // Delay for 1 hour before retrying on error to avoid rapid failure loops
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }

            _logger.LogInformation("خدمة جدولة النسخ الاحتياطي لقاعدة البيانات قد توقفت.");
        }
    }
}
