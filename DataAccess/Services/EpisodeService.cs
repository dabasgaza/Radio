using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BroadcastWorkflow.Services;

public interface IEpisodeService
{
    Task<List<ActiveEpisodeDto>> GetActiveEpisodesAsync();
    Task CreateEpisodeAsync(EpisodeDto dto, UserSession session);
    Task UpdateStatusAsync(int episodeId, byte newStatusId, UserSession session);
}

public class EpisodeService : IEpisodeService
{
    private readonly IDbContextFactory<BroadcastWorkflowDBContext> _contextFactory;
    private readonly IAuditService _audit;

    public EpisodeService(IDbContextFactory<BroadcastWorkflowDBContext> factory, IAuditService audit)
    { _contextFactory = factory; _audit = audit; }

    public async Task<List<ActiveEpisodeDto>> GetActiveEpisodesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var source = await context.Episodes
            .AsNoTracking()
            .Include(e => e.Program)
            .Include(e => e.EpisodeStatus) // تضمين جدول الحالات الجديد
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new ActiveEpisodeDto(
                e.EpisodeId,
                e.EpisodeName,
                e.Program.ProgramName,
                e.ScheduledExecutionTime,
                e.EpisodeStatus.DisplayName,
                e.SpecialNotes
            )).ToListAsync();


        return await context.Episodes
            .AsNoTracking()
            .Include(e => e.Program)
            .Include(e => e.EpisodeStatus) // تضمين جدول الحالات الجديد
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new ActiveEpisodeDto(
                e.EpisodeId,
                e.EpisodeName,
                e.Program.ProgramName,
                e.ScheduledExecutionTime,
                e.EpisodeStatus.DisplayName,
                e.SpecialNotes
            ))
            .ToListAsync();
    }

    public async Task CreateEpisodeAsync(EpisodeDto dto, UserSession session)
    {
        SecurityHelper.EnsureRole(session, AppPermissions.CoordinationManage);
        using var context = await _contextFactory.CreateDbContextAsync();
        var episode = new Episode
        {
            ProgramId = dto.ProgramId,
            EpisodeName = dto.EpisodeName,
            ScheduledExecutionTime = dto.ScheduledTime,
            StatusId = 0,
            //CreatedByUserId = session.UserId
        };
        context.Episodes.Add(episode);
        await context.SaveChangesAsync();
        await _audit.LogActionAsync("Episodes", episode.EpisodeId, "INSERT", null, dto, session.UserId);
    }

    public async Task UpdateStatusAsync(int episodeId, byte newStatusId, UserSession session)
    {
        // التحقق من الصلاحيات
        if (newStatusId == 1) SecurityHelper.EnsurePermission(session, AppPermissions.EpisodeExecute);

        if (newStatusId == 2) SecurityHelper.EnsurePermission(session, AppPermissions.EpisodePublish);


        using var context = await _contextFactory.CreateDbContextAsync();

        var episode = await context.Episodes.FindAsync(episodeId);

        if (episode == null) throw new Exception("الحلقة غير موجودة.");

        // قاعدة: لا يمكن تغيير حالة حلقة منشورة نهائياً
        if (episode.StatusId == 2)
            throw new Exception("لا يمكن تعديل حالة حلقة تم نشرها بالفعل.");

        // قاعدة: لا يمكن العودة من "منفذة" إلى "مجدولة"
        if (episode.StatusId == 1 && newStatusId == 0)
            throw new Exception("لا يمكن إعادة حلقة منفذة إلى حالة الجدولة.");

        // قاعدة: لا يمكن الانتقال من "مجدولة" إلى "منشورة" مباشرة (يجب التنفيذ أولاً)
        if (episode.StatusId == 0 && newStatusId == 2)
            throw new Exception("يجب تنفيذ الحلقة وتوثيقها قبل عملية النشر الرقمي.");


        // تحديث الحالة
        episode.StatusId = newStatusId;


        if (newStatusId == 1) episode.ActualExecutionTime = DateTime.UtcNow;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new Exception("فشل التحديث: قام مستخدم آخر بتعديل حالة هذه الحلقة للتو.");
        }
    }
}