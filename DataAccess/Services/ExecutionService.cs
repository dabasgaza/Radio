using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public interface IExecutionService
{
    // ✨ استقبال DTO بدلاً من الكيان
    Task LogExecutionAsync(ExecutionLogDto dto, UserSession session);
}

// ✨ استخدام Primary Constructor
public class ExecutionService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IExecutionService
{
    public async Task LogExecutionAsync(ExecutionLogDto dto, UserSession session)
    {
        // ✨ تصحيح الأمان: استخدام EnsurePermission للصلاحيات
        session.EnsurePermission(AppPermissions.EpisodeExecute);

        using var context = await contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // ✨ إنشاء الكيان من الـ DTO داخل الـ Service فقط (حماية من Mass Assignment)
            var log = new ExecutionLog
            {
                EpisodeId = dto.EpisodeId,
                ExecutedByUserId = session.UserId, // هذا حقل أعمال خاص بالتنفيذ، يجب تعبئته
                ExecutionNotes = dto.ExecutionNotes,
                IssuesEncountered = dto.IssuesEncountered
            };

            context.ExecutionLogs.Add(log);

            var episode = await context.Episodes.FindAsync(dto.EpisodeId);
            if (episode == null) throw new KeyNotFoundException("الحلقة غير موجودة.");

            // ✨ استخدام الثوابت بدلاً من الأرقام السحرية
            if (episode.StatusId == EpisodeStatus.Published)
                throw new InvalidOperationException("لا يمكن تعديل حالة حلقة تم نشرها بالفعل.");

            episode.StatusId = EpisodeStatus.Executed;
            episode.ActualExecutionTime = DateTime.UtcNow;

            // ❌ تم إزالة UpdatedAt و UpdatedByUserId (الـ Interceptor سيتولى الأمر تلقائياً)

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            // ✨ رمي الاستثناء الأصلي كما هو للحفاظ على الـ Stack Trace (بدون تغليف)
            throw;
        }
    }
}