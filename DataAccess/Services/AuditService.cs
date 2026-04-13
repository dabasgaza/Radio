using Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BroadcastWorkflow.Services;

public interface IAuditService
{
    Task LogActionAsync(string tableName, int recordId, string action, object? oldVal, object? newVal, int userId);
}

public class AuditService : IAuditService
{
    private readonly IDbContextFactory<BroadcastWorkflowDBContext> _contextFactory;
    public AuditService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) => _contextFactory = contextFactory;

    public async Task LogActionAsync(string tableName, int recordId, string action, object? oldVal, object? newVal, int userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var log = new AuditLog
        {
            TableName = tableName,
            RecordId = recordId,
            Action = action,
            OldValues = oldVal != null ? JsonSerializer.Serialize(oldVal) : null,
            NewValues = newVal != null ? JsonSerializer.Serialize(newVal) : null,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };
        context.AuditLogs.Add(log);
        await context.SaveChangesAsync();
    }
}