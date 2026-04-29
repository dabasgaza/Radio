using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public interface IExecutionService
{
    // ✨ استقبال DTO بدلاً من الكيان
    Task<Result> LogExecutionAsync(ExecutionLogDto dto, UserSession session);
}

// ✨ استخدام Primary Constructor
public class ExecutionService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IExecutionService
{
    public async Task<Result> LogExecutionAsync(ExecutionLogDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeExecute);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

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
            if (episode == null) return Result.Fail("الحلقة غير موجودة.");

            // ✨ استخدام الثوابت بدلاً من الأرقام السحرية
            if (episode.StatusId == EpisodeStatus.Published)
                return Result.Fail("لا يمكن تعديل حالة حلقة تم نشرها بالفعل.");

            episode.StatusId = EpisodeStatus.Executed;
            episode.ActualExecutionTime = DateTime.UtcNow;

            // ❌ تم إزالة UpdatedAt و UpdatedByUserId (الـ Interceptor سيتولى الأمر تلقائياً)

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result.Success();
        }
        catch
        {
            await transaction.RollbackAsync();
            // ✨ رمي الاستثناء الأصلي كما هو للحفاظ على الـ Stack Trace (بدون تغليف)
            throw;
        }
    }
}