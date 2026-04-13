using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BroadcastWorkflow.Services;

public interface IEpisodeService
{
    Task<List<ActiveEpisodeDto>> GetActiveEpisodesAsync();
    Task CreateEpisodeAsync(EpisodeDto dto, UserSession session);
    Task UpdateStatusAsync(int episodeId, byte newStatus, UserSession session);
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
        return await context.Episodes
            .AsNoTracking()
            .Where(e => e.IsActive)
                       .Select(e => new ActiveEpisodeDto
                      (e.EpisodeId,
                       e.EpisodeName,
                       e.Program.ProgramName,
                       e.ScheduledExecutionTime,
                       e.Status == 0 ? "مخطط لها" : (e.Status == 1 ? "تم التنفيذ" : "تم النشر"),
                       e.SpecialNotes))
            .ToListAsync();
    }

    public async Task CreateEpisodeAsync(EpisodeDto dto, UserSession session)
    {
        SecurityHelper.EnsureRole(session, "Coordination");
        using var context = await _contextFactory.CreateDbContextAsync();
        var episode = new Episode
        {
            ProgramId = dto.ProgramId,
            EpisodeName = dto.EpisodeName,
            ScheduledExecutionTime = dto.ScheduledTime,
            Status = 0,
            CreatedByUserId = session.UserId
        };
        context.Episodes.Add(episode);
        await context.SaveChangesAsync();
        await _audit.LogActionAsync("Episodes", episode.EpisodeId, "INSERT", null, dto, session.UserId);
    }

    public async Task UpdateStatusAsync(int episodeId, byte newStatus, UserSession session)
    {
        // التحقق من الصلاحيات
        if (newStatus == 1) SecurityHelper.EnsurePermission(session, "EPISODE_EXECUTE");

        if (newStatus == 2) SecurityHelper.EnsurePermission(session, "EPISODE_PUBLISH");


        using var context = await _contextFactory.CreateDbContextAsync();
        var episode = await context.Episodes.FindAsync(episodeId);

        if (episode == null) throw new Exception("الحلقة غير موجودة.");

        // بديل منطق الـ Stored Procedure: قواعد انتقال الحالة
        if (episode.Status == 2)
            throw new Exception("لا يمكن تغيير حالة حلقة منشورة (Published).");

        if (episode.Status == 1 && newStatus == 0)
            throw new Exception("لا يمكن إعادة حلقة منفذة (Executed) إلى حالة التخطيط (Planned).");

        // تحديث الحالة
        episode.Status = newStatus;
        episode.UpdatedAt = DateTime.UtcNow;
        episode.UpdatedByUserId = session.UserId;

        if (newStatus == 1) episode.ActualExecutionTime = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }
}