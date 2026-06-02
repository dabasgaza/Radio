using DataAccess.Common;
using DataAccess.Services;
using DataAccess.Services.Messaging;
using Domain.Models;
using Microsoft.Win32;
using Radio.Messaging;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Radio.Views.Database
{
    public partial class DatabaseManagementView : UserControl
    {
        private readonly IDatabaseManagementService _dbService;
        private readonly UserSession _session;
        private readonly string _configPath;

        public DatabaseManagementView(IDatabaseManagementService dbService, UserSession session)
        {
            InitializeComponent();
            _dbService = dbService;
            _session = session;
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            // Fallback if appsettings doesn't exist in build output, search parent directory
            if (!File.Exists(_configPath))
            {
                _configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            }

            Loaded += DatabaseManagementView_Loaded;
        }

        private async void DatabaseManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfig();
            await InitializeBackupLogsAsync();
            await LoadHistoryAsync();
        }

        private async Task InitializeBackupLogsAsync()
        {
            var result = await _dbService.InitializeDatabaseAsync();
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(_configPath)) return;

                var jsonText = File.ReadAllText(_configPath);
                var root = JsonNode.Parse(jsonText);
                if (root?["DatabaseBackup"] is JsonObject backupNode)
                {
                    ChkEnabled.IsChecked = backupNode["Enabled"]?.GetValue<bool>() ?? false;
                    ChkCloudSync.IsChecked = backupNode["CloudSync"]?.GetValue<bool>() ?? false;
                    
                    var interval = backupNode["IntervalHours"]?.GetValue<double>() ?? 24;
                    foreach (ComboBoxItem item in ComboInterval.Items)
                    {
                        if (item.Tag?.ToString() == interval.ToString())
                        {
                            ComboInterval.SelectedItem = item;
                            break;
                        }
                    }

                    TxtRetentionDays.Text = backupNode["RetentionDays"]?.ToString() ?? "30";
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Show(NotificationType.Warning, "تحذير", $"لم يتمكن التطبيق من تحميل الإعدادات: {ex.Message}");
            }
        }

        private void BtnSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    NotificationManager.Show(NotificationType.Error, "خطأ", "ملف الإعدادات appsettings.json غير موجود.");
                    return;
                }

                if (!int.TryParse(TxtRetentionDays.Text, out int retentionDays) || retentionDays <= 0)
                {
                    NotificationManager.Show(NotificationType.Warning, "تنبيه", "الرجاء إدخال عدد أيام صحيح لسياسة الاحتفاظ.");
                    return;
                }

                var jsonText = File.ReadAllText(_configPath);
                var root = JsonNode.Parse(jsonText) as JsonObject ?? new JsonObject();
                
                var backupNode = root["DatabaseBackup"] as JsonObject;
                if (backupNode == null)
                {
                    backupNode = new JsonObject();
                    root["DatabaseBackup"] = backupNode;
                }

                backupNode["Enabled"] = ChkEnabled.IsChecked == true;
                backupNode["CloudSync"] = ChkCloudSync.IsChecked == true;

                if (ComboInterval.SelectedItem is ComboBoxItem selectedItem && double.TryParse(selectedItem.Tag?.ToString(), out double interval))
                {
                    backupNode["IntervalHours"] = interval;
                }
                else
                {
                    backupNode["IntervalHours"] = 24;
                }

                backupNode["RetentionDays"] = retentionDays;

                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(_configPath, root.ToJsonString(options));

                NotificationManager.Show(NotificationType.Success, "تم بنجاح", "تم حفظ وتحديث إعدادات النسخ الاحتياطي بنجاح.");
            }
            catch (Exception ex)
            {
                NotificationManager.Show(NotificationType.Error, "خطأ", $"فشل حفظ الإعدادات: {ex.Message}");
            }
        }

        private async Task LoadHistoryAsync()
        {
            var result = await _dbService.GetBackupHistoryAsync();
            if (result.IsSuccess)
            {
                GridHistory.ItemsSource = result.Value;
            }
            else
            {
                NotificationManager.Show(NotificationType.Error, "خطأ", $"فشل تحميل سجل النسخ الاحتياطي: {result.ErrorMessage}");
            }
        }

        private async void BtnQuickBackup_Click(object sender, RoutedEventArgs e)
        {
            BtnQuickBackup.IsEnabled = false;
            try
            {
                NotificationManager.Show(NotificationType.Info, "جاري العمل", "بدء عملية النسخ الاحتياطي الفوري...");
                
                var result = await Task.Run(() => _dbService.BackupDatabaseAsync());
                if (result.IsSuccess)
                {
                    NotificationManager.Show(NotificationType.Success, "نجاح", $"تم إنشاء النسخة الاحتياطية بنجاح في: {result.Value}");
                    
                    if (ChkCloudSync.IsChecked == true && result.Value != null)
                    {
                        var cloudResult = await Task.Run(() => _dbService.CloudSyncBackupAsync(result.Value));
                        if (cloudResult.IsSuccess)
                        {
                            NotificationManager.Show(NotificationType.Success, "مزامنة سحابية", "تمت مزامنة النسخة الاحتياطية سحابياً.");
                        }
                    }

                    await LoadHistoryAsync();
                }
                else
                {
                    NotificationManager.Show(NotificationType.Error, "فشل النسخ", result.ErrorMessage);
                }
            }
            finally
            {
                BtnQuickBackup.IsEnabled = true;
            }
        }

        private async void BtnChooseAndRestore_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Database Backup files (*.bak)|*.bak|All files (*.*)|*.*",
                Title = "اختر ملف النسخة الاحتياطية لاستعادتها"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var confirmResult = await MessageService.Current.ShowConfirmationAsync(
                    "تحذير: سيتم قطع كافة الاتصالات النشطة مؤقتاً واستعادة قاعدة البيانات بالكامل من هذا الملف، وسيتم فقدان أي تغييرات تمت بعد هذا الملف. هل أنت متأكد من المتابعة؟",
                    "تأكيد الاستعادة");

                if (confirmResult)
                {
                    BtnChooseAndRestore.IsEnabled = false;
                    try
                    {
                        NotificationManager.Show(NotificationType.Info, "جاري العمل", "بدء عملية استعادة قاعدة البيانات...");
                        var result = await Task.Run(() => _dbService.RestoreDatabaseAsync(openFileDialog.FileName));
                        
                        if (result.IsSuccess)
                        {
                            NotificationManager.Show(NotificationType.Success, "نجاح", "تمت استعادة قاعدة البيانات بالكامل بنجاح!");
                            await LoadHistoryAsync();
                        }
                        else
                        {
                            NotificationManager.Show(NotificationType.Error, "فشل الاستعادة", result.ErrorMessage);
                        }
                    }
                    finally
                    {
                        BtnChooseAndRestore.IsEnabled = true;
                    }
                }
            }
        }

        private async void BtnInitializeDb_Click(object sender, RoutedEventArgs e)
        {
            var confirmResult = await MessageService.Current.ShowConfirmationAsync(
                "تحذير خطير: سيؤدي هذا الإجراء إلى تهيئة قاعدة البيانات والتأكد من تثبيت كافة الهجرات (Migrations) المطلوبة. هل تريد المتابعة؟",
                "تأكيد التهيئة");

            if (confirmResult)
            {
                BtnInitializeDb.IsEnabled = false;
                try
                {
                    NotificationManager.Show(NotificationType.Info, "جاري العمل", "جاري تهيئة قاعدة البيانات...");
                    var result = await Task.Run(() => _dbService.InitializeDatabaseAsync());

                    if (result.IsSuccess)
                    {
                        NotificationManager.Show(NotificationType.Success, "نجاح", "تمت تهيئة قاعدة البيانات بنجاح.");
                        await LoadHistoryAsync();
                    }
                    else
                    {
                        NotificationManager.Show(NotificationType.Error, "فشل التهيئة", result.ErrorMessage);
                    }
                }
                finally
                {
                    BtnInitializeDb.IsEnabled = true;
                }
            }
        }

        private async void BtnRefreshHistory_Click(object sender, RoutedEventArgs e)
        {
            await LoadHistoryAsync();
        }

        private async void BtnRestoreRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DatabaseBackupLog log)
            {
                var confirmResult = await MessageService.Current.ShowConfirmationAsync(
                    $"هل أنت متأكد من رغبتك في استعادة قاعدة البيانات للنسخة المحددة بتاريخ {log.CreatedAt}؟",
                    "تأكيد الاستعادة");

                if (confirmResult)
                {
                    try
                    {
                        NotificationManager.Show(NotificationType.Info, "جاري العمل", "بدء عملية الاستعادة...");
                        var result = await Task.Run(() => _dbService.RestoreDatabaseAsync(log.BackupPath));

                        if (result.IsSuccess)
                        {
                            NotificationManager.Show(NotificationType.Success, "نجاح", "تمت استعادة قاعدة البيانات بنجاح.");
                        }
                        else
                        {
                            NotificationManager.Show(NotificationType.Error, "فشل الاستعادة", result.ErrorMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        NotificationManager.Show(NotificationType.Error, "خطأ", ex.Message);
                    }
                }
            }
        }

        private async void BtnSyncRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DatabaseBackupLog log)
            {
                try
                {
                    NotificationManager.Show(NotificationType.Info, "جاري العمل", "جاري رفع النسخة الاحتياطية إلى السحابة...");
                    var result = await Task.Run(() => _dbService.CloudSyncBackupAsync(log.BackupPath));

                    if (result.IsSuccess)
                    {
                        NotificationManager.Show(NotificationType.Success, "نجاح", "تمت المزامنة السحابية بنجاح.");
                        await LoadHistoryAsync();
                    }
                    else
                    {
                        NotificationManager.Show(NotificationType.Error, "فشل المزامنة", result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    NotificationManager.Show(NotificationType.Error, "خطأ", ex.Message);
                }
            }
        }
    }
}
