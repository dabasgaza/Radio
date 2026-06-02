using System;

namespace Domain.Models;

public partial class DatabaseBackupLog : BaseEntity
{
    public int DatabaseBackupLogId { get; set; }
    
    public string BackupPath { get; set; } = null!;
    
    public string BackupType { get; set; } = null!; // Local, Cloud, Both
    
    public long FileSize { get; set; }
    
    public string Status { get; set; } = null!; // Success, Failed
    
    public string? ErrorMessage { get; set; }
    
    public string? CloudUrl { get; set; }
}
