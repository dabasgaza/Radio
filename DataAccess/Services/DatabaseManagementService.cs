using DataAccess.Common;
using Domain.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public class DatabaseManagementService : IDatabaseManagementService
    {
        private readonly IDbContextFactory<BroadcastWorkflowDBContext> _dbContextFactory;
        private readonly IConfiguration _configuration;
        private readonly CurrentSessionProvider _sessionProvider;

        public DatabaseManagementService(
            IDbContextFactory<BroadcastWorkflowDBContext> dbContextFactory,
            IConfiguration configuration,
            CurrentSessionProvider sessionProvider)
        {
            _dbContextFactory = dbContextFactory;
            _configuration = configuration;
            _sessionProvider = sessionProvider;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection") 
                ?? "Server=.;Database=BroadcastWorkflowDB;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        private string GetDatabaseName(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return builder.InitialCatalog;
        }

        public async Task<Result<string>> BackupDatabaseAsync(string? customBackupFolder = null)
        {
            try
            {
                var connectionString = GetConnectionString();
                var dbName = GetDatabaseName(connectionString);
                
                // Determine folder path
                var backupFolder = customBackupFolder;
                if (string.IsNullOrEmpty(backupFolder))
                {
                    backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                }
                
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                var fileName = $"{dbName}_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                var fullPath = Path.Combine(backupFolder, fileName);

                // SQL command to backup
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    // We check if compression is supported (SQL Server Standard/Developer/Enterprise support it)
                    // If not, we fall back to regular backup.
                    string backupQuery = $@"
                        BACKUP DATABASE [{dbName}] 
                        TO DISK = @BackupPath 
                        WITH FORMAT, INIT, NAME = @BackupName";
                    
                    try
                    {
                        // Try with compression first
                        string compressionQuery = backupQuery + ", COMPRESSION";
                        using (var command = new SqlCommand(compressionQuery, connection))
                        {
                            command.Parameters.AddWithValue("@BackupPath", fullPath);
                            command.Parameters.AddWithValue("@BackupName", $"Radio Full Database Backup - {DateTime.Now}");
                            command.CommandTimeout = 300; // 5 minutes
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    catch
                    {
                        // Fallback to no compression
                        using (var command = new SqlCommand(backupQuery, connection))
                        {
                            command.Parameters.AddWithValue("@BackupPath", fullPath);
                            command.Parameters.AddWithValue("@BackupName", $"Radio Full Database Backup - {DateTime.Now}");
                            command.CommandTimeout = 300;
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }

                // Log the backup in database
                long fileSize = 0;
                if (File.Exists(fullPath))
                {
                    fileSize = new FileInfo(fullPath).Length;
                }

                await LogBackupAsync(fullPath, "Local", fileSize, "Success");

                return Result<string>.Success(fullPath);
            }
            catch (Exception ex)
            {
                await LogBackupAsync(customBackupFolder ?? "N/A", "Local", 0, "Failed", ex.Message);
                return Result<string>.Fail($"حدث خطأ أثناء عمل النسخة الاحتياطية: {ex.Message}");
            }
        }

        public async Task<Result> RestoreDatabaseAsync(string backupFilePath)
        {
            try
            {
                if (!File.Exists(backupFilePath))
                {
                    return Result.Fail("ملف النسخة الاحتياطية غير موجود.");
                }

                var connectionString = GetConnectionString();
                var dbName = GetDatabaseName(connectionString);

                // Build a connection string targeting master database to restore
                var masterBuilder = new SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = "master"
                };

                using (var connection = new SqlConnection(masterBuilder.ConnectionString))
                {
                    await connection.OpenAsync();

                    // 1. Terminate active connections to the database to prevent locking errors
                    string setSingleUserQuery = $@"
                        ALTER DATABASE [{dbName}] 
                        SET SINGLE_USER 
                        WITH ROLLBACK IMMEDIATE;";
                    
                    using (var cmd = new SqlCommand(setSingleUserQuery, connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 2. Perform the Restore
                    string restoreQuery = $@"
                        RESTORE DATABASE [{dbName}] 
                        FROM DISK = @BackupPath 
                        WITH REPLACE;";
                    
                    using (var cmd = new SqlCommand(restoreQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@BackupPath", backupFilePath);
                        cmd.CommandTimeout = 600; // 10 minutes
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 3. Set the database back to Multi-User mode
                    string setMultiUserQuery = $@"
                        ALTER DATABASE [{dbName}] 
                        SET MULTI_USER;";
                    
                    using (var cmd = new SqlCommand(setMultiUserQuery, connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                // Re-enable multi-user mode just in case of failure
                try
                {
                    var connectionString = GetConnectionString();
                    var dbName = GetDatabaseName(connectionString);
                    var masterBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
                    using (var connection = new SqlConnection(masterBuilder.ConnectionString))
                    {
                        await connection.OpenAsync();
                        using (var cmd = new SqlCommand($"ALTER DATABASE [{dbName}] SET MULTI_USER;", connection))
                        {
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
                catch { /* Ignore fallback errors */ }

                return Result.Fail($"حدث خطأ أثناء استعادة النسخة الاحتياطية: {ex.Message}");
            }
        }

        public async Task<Result> InitializeDatabaseAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                
                // التحقق مما إذا كانت قاعدة البيانات موجودة ومتاحة للاتصال
                bool dbExists = false;
                try
                {
                    dbExists = await context.Database.CanConnectAsync();
                }
                catch
                {
                    // في حال عدم الوجود، سيفشل الاتصال ونعتبر قاعدة البيانات غير موجودة
                    dbExists = false;
                }

                if (!dbExists)
                {
                    // إنشاء قاعدة البيانات بالكامل وتطبيق جميع الهجرات (Migrations) من الصفر
                    await context.Database.MigrateAsync();
                    return Result.Success();
                }

                // إذا كانت قاعدة البيانات موجودة، نحاول تطبيق أي هجرات جديدة معلقة
                try
                {
                    await context.Database.MigrateAsync();
                }
                catch (SqlException ex) when (ex.Message.Contains("already an object named"))
                {
                    // The database already has the tables. Let's make sure the DatabaseBackupLogs table is created.
                    using (var connection = new SqlConnection(GetConnectionString()))
                    {
                        await connection.OpenAsync();
                        
                        // Check if DatabaseBackupLogs table exists
                        string checkTableQuery = "SELECT OBJECT_ID(N'[dbo].[DatabaseBackupLogs]', N'U')";
                        object? tableId = null;
                        using (var cmd = new SqlCommand(checkTableQuery, connection))
                        {
                            tableId = await cmd.ExecuteScalarAsync();
                        }

                        if (tableId == null || tableId == DBNull.Value)
                        {
                            // Create the DatabaseBackupLogs table
                            string createTableQuery = @"
                                CREATE TABLE [DatabaseBackupLogs] (
                                    [DatabaseBackupLogId] int NOT NULL IDENTITY,
                                    [BackupPath] nvarchar(500) NOT NULL,
                                    [BackupType] nvarchar(50) NOT NULL,
                                    [FileSize] bigint NOT NULL,
                                    [Status] nvarchar(50) NOT NULL,
                                    [ErrorMessage] nvarchar(2000) NULL,
                                    [CloudUrl] nvarchar(1000) NULL,
                                    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
                                    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
                                    [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
                                    [CreatedByUserId] int NULL,
                                    [UpdatedByUserId] int NULL,
                                    [RowVersion] rowversion NOT NULL,
                                    CONSTRAINT [PK_DatabaseBackupLogs] PRIMARY KEY ([DatabaseBackupLogId]),
                                    CONSTRAINT [FK_DatabaseBackupLogs_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
                                    CONSTRAINT [FK_DatabaseBackupLogs_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
                                );
                                CREATE INDEX [IX_DatabaseBackupLogs_CreatedAt] ON [DatabaseBackupLogs] ([CreatedAt]);";
                            
                            using (var cmd = new SqlCommand(createTableQuery, connection))
                            {
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        // Ensure we register this migration as done in __EFMigrationsHistory
                        string checkMigrationQuery = "SELECT COUNT(*) FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260602111641_AddDatabaseBackupLog'";
                        int migrationExists = 0;
                        using (var cmd = new SqlCommand(checkMigrationQuery, connection))
                        {
                            try
                            {
                                migrationExists = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                            }
                            catch
                            {
                                // If __EFMigrationsHistory doesn't exist, create it and register
                                string createHistoryTable = @"
                                    IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
                                    BEGIN
                                        CREATE TABLE [__EFMigrationsHistory] (
                                            [MigrationId] nvarchar(150) NOT NULL,
                                            [ProductVersion] nvarchar(32) NOT NULL,
                                            CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                                        );
                                    END";
                                using (var cmdCreate = new SqlCommand(createHistoryTable, connection))
                                {
                                    await cmdCreate.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        if (migrationExists == 0)
                        {
                            string insertMigrationQuery = "INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260602111641_AddDatabaseBackupLog', '10.0.8')";
                            using (var cmd = new SqlCommand(insertMigrationQuery, connection))
                            {
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Fail($"حدث خطأ أثناء تهيئة قاعدة البيانات: {ex.Message}");
            }
        }

        public async Task<Result<List<DatabaseBackupLog>>> GetBackupHistoryAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var list = await context.DatabaseBackupLogs
                    .AsNoTracking()
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();
                return Result<List<DatabaseBackupLog>>.Success(list);
            }
            catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException || ex is InvalidOperationException)
            {
                return Result<List<DatabaseBackupLog>>.Fail("جدول سجل النسخ الاحتياطي (DatabaseBackupLogs) غير موجود أو لم يُنشأ بعد. الرجاء الضغط على زر \"تهيئة قاعدة البيانات\" أولاً.");
            }
            catch (Exception ex)
            {
                return Result<List<DatabaseBackupLog>>.Fail($"حدث خطأ أثناء جلب سجل النسخ الاحتياطي: {ex.Message}");
            }
        }

        public async Task<Result> CloudSyncBackupAsync(string localBackupPath)
        {
            try
            {
                if (!File.Exists(localBackupPath))
                {
                    return Result.Fail("ملف النسخة الاحتياطية المحلي غير موجود.");
                }

                var fileName = Path.GetFileName(localBackupPath);
                
                // Define a simulated Cloud backup folder for demo/local infrastructure purposes
                var cloudFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups", "CloudSimulation");
                if (!Directory.Exists(cloudFolder))
                {
                    Directory.CreateDirectory(cloudFolder);
                }

                var cloudPath = Path.Combine(cloudFolder, fileName);
                File.Copy(localBackupPath, cloudPath, true);

                // Update the log in database
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var log = await context.DatabaseBackupLogs
                    .FirstOrDefaultAsync(x => x.BackupPath == localBackupPath);

                if (log != null)
                {
                    log.CloudUrl = $"https://simulated-cloud.radio.local/backups/{fileName}";
                    log.BackupType = "Both";
                    log.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
                else
                {
                    var newLog = new DatabaseBackupLog
                    {
                        BackupPath = localBackupPath,
                        BackupType = "Cloud",
                        FileSize = new FileInfo(localBackupPath).Length,
                        Status = "Success",
                        CloudUrl = $"https://simulated-cloud.radio.local/backups/{fileName}"
                    };
                    context.DatabaseBackupLogs.Add(newLog);
                    await context.SaveChangesAsync();
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Fail($"حدث خطأ أثناء المزامنة السحابية: {ex.Message}");
            }
        }

        public async Task<Result> RunRetentionPolicyAsync(int retentionDays)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                using var context = await _dbContextFactory.CreateDbContextAsync();
                
                // Get logs older than cutoff
                var oldLogs = await context.DatabaseBackupLogs
                    .Where(x => x.CreatedAt < cutoffDate && x.Status == "Success")
                    .ToListAsync();

                int deletedCount = 0;
                foreach (var log in oldLogs)
                {
                    try
                    {
                        if (File.Exists(log.BackupPath))
                        {
                            File.Delete(log.BackupPath);
                        }
                        
                        // Also delete simulated cloud copy
                        var cloudSimPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups", "CloudSimulation", Path.GetFileName(log.BackupPath));
                        if (File.Exists(cloudSimPath))
                        {
                            File.Delete(cloudSimPath);
                        }

                        log.IsActive = false; // Soft delete
                        log.UpdatedAt = DateTime.UtcNow;
                        deletedCount++;
                    }
                    catch { /* Continue next */ }
                }

                if (deletedCount > 0)
                {
                    await context.SaveChangesAsync();
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Fail($"حدث خطأ أثناء تطبيق سياسة الاحتفاظ: {ex.Message}");
            }
        }

        private async Task LogBackupAsync(string path, string type, long size, string status, string? error = null)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var log = new DatabaseBackupLog
                {
                    BackupPath = path,
                    BackupType = type,
                    FileSize = size,
                    Status = status,
                    ErrorMessage = error
                };
                context.DatabaseBackupLogs.Add(log);
                await context.SaveChangesAsync();
            }
            catch { /* Avoid crashing on logging fail */ }
        }
    }
}
