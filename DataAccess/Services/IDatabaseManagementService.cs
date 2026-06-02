using DataAccess.Common;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public interface IDatabaseManagementService
    {
        Task<Result<string>> BackupDatabaseAsync(string? customBackupFolder = null);
        Task<Result> RestoreDatabaseAsync(string backupFilePath);
        Task<Result> InitializeDatabaseAsync();
        Task<Result<List<DatabaseBackupLog>>> GetBackupHistoryAsync();
        Task<Result> CloudSyncBackupAsync(string localBackupPath);
        Task<Result> RunRetentionPolicyAsync(int retentionDays);
    }
}
