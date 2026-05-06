using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public interface IExecutionService
{
    // ✨ استقبال DTO بدلاً من الكيان
    Task<Result> LogExecutionAsync(ExecutionLogDto dto, UserSession session);

    // ═══════════════════════════════════════════
    //  دوال استرجاع وتعديل سجل التنفيذ
    // ═══════════════════════════════════════════

    /// <summary>
    /// استرجاع سجل التنفيذ لحلقة معيّنة
    /// يُرجع null إذا لم يوجد سجل تنفيذ نشط
    /// </summary>
    Task<ExecutionLogDto?> GetExecutionLogAsync(int episodeId);

    /// <summary>
    /// تعديل سجل تنفيذ موجود (المدة، الملاحظات، المشاكل)
    /// لا يُغيّر حالة الحلقة — فقط يحدّث البيانات
    /// </summary>
    Task<Result> UpdateExecutionLogAsync(ExecutionLogDto dto, UserSession session);
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

    // ═══════════════════════════════════════════
    //  استرجاع وتعديل سجل التنفيذ
    // ═══════════════════════════════════════════

    /// <summary>
    /// استرجاع سجل التنفيذ لحلقة معيّنة
    /// يُرجع null إذا لم يوجد سجل تنفيذ نشط
    /// </summary>
    public async Task<ExecutionLogDto?> GetExecutionLogAsync(int episodeId)
    {
        // لا نحتاج صلاحية خاصة — مجرد قراءة
        using var context = await contextFactory.CreateDbContextAsync();

        var log = await context.ExecutionLogs
            .AsNoTracking()
            .Where(l => l.EpisodeId == episodeId && l.IsActive)
            .OrderByDescending(l => l.CreatedAt)  // أحدث سجل أولاً
            .FirstOrDefaultAsync();

        if (log is null) return null;

        // تحويل الكيان إلى DTO
        return new ExecutionLogDto
        {
            ExecutionLogId = log.ExecutionLogId,
            EpisodeId = log.EpisodeId,
            ExecutedByUserId = log.ExecutedByUserId,
            ExecutionNotes = log.ExecutionNotes,
            IssuesEncountered = log.IssuesEncountered,
            DurationMinutes = log.DurationMinutes ?? 0
        };
    }

    /// <summary>
    /// تعديل سجل تنفيذ موجود
    /// يحدّث: المدة، الملاحظات، المشاكل التقنية
    /// لا يُغيّر حالة الحلقة — فقط يحدّث البيانات
    /// </summary>
    public async Task<Result> UpdateExecutionLogAsync(ExecutionLogDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeExecute);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();

        var log = await context.ExecutionLogs
            .FirstOrDefaultAsync(l => l.ExecutionLogId == dto.ExecutionLogId && l.IsActive);

        if (log is null)
            return Result.Fail("سجل التنفيذ غير موجود أو تم حذفه.");

        // تحديث الحقول القابلة للتعديل فقط
        log.DurationMinutes = dto.DurationMinutes;
        log.ExecutionNotes = dto.ExecutionNotes;
        log.IssuesEncountered = dto.IssuesEncountered;
        log.UpdatedByUserId = session.UserId;

        await context.SaveChangesAsync();
        return Result.Success();
    }
}