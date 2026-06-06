using DataAccess.Common;
using Domain.Models;

namespace DataAccess.Services
{
    public interface IDatabaseManagementService
    {
        Task<Result<string>> BackupDatabaseAsync(string? customBackupFolder = null);
        Task<Result> RestoreDatabaseAsync(string backupFilePath);
        Task<Result> InitializeDatabaseAsync();
        Task<Result> ResetDatabaseAsync();
        Task<Result<List<DatabaseBackupLog>>> GetBackupHistoryAsync();
        Task<Result> CloudSyncBackupAsync(string localBackupPath);
        Task<Result> RunRetentionPolicyAsync(int retentionDays);
    }
}
