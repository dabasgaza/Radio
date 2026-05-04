using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public static class EpisodeStatus
{
    public const byte Planned = 0;
    public const byte Executed = 1;
    public const byte Published = 2;
    public const byte WebsitePublished = 3;
    public const byte Cancelled = 4;
}

public interface IEpisodeService
{
    Task<List<ActiveEpisodeDto>> GetActiveEpisodesAsync();
    Task<Result<int>> CreateEpisodeAsync(EpisodeDto dto, UserSession session);
    Task<Result> UpdateEpisodeAsync(EpisodeDto dto, UserSession session);
    Task<Result> UpdateStatusAsync(int episodeId, byte newStatusId, UserSession session);
    Task<Result> DeleteEpisodeAsync(int episodeId, UserSession session);
    Task<Result> ToggleWebsitePublishAsync(int episodeId, bool isPublished, UserSession session);
    Task<List<EpisodeGuestDto>> GetEpisodeGuestsAsync(int episodeId);
    Task<ActiveEpisodeDto?> GetActiveEpisodeByIdAsync(int episodeId);
    Task<Result> RevertEpisodeStatusAsync(int episodeId, string reason, UserSession session);
    Task<Result> CancelEpisodeAsync(int episodeId, string reason, UserSession session);
    Task<Result> UpdateCancellationReasonAsync(int episodeId, string newReason, UserSession session);
}

public class EpisodeService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IEpisodeService
{
    // ──────────────────────────────────────────────────────────────
    // Queries
    // ──────────────────────────────────────────────────────────────

    public async Task<List<ActiveEpisodeDto>> GetActiveEpisodesAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();

        return await context.Episodes
            .AsNoTracking()
            .OrderBy(e => e.ScheduledExecutionTime)
            .Select(e => new ActiveEpisodeDto
            {
                EpisodeId               = e.EpisodeId,
                StatusId                = e.StatusId,
                ProgramId               = e.ProgramId,
                EpisodeName             = e.EpisodeName,
                ProgramName             = e.Program.ProgramName,
                ScheduledExecutionTime  = e.ScheduledExecutionTime,
                StatusText              = e.EpisodeStatus.DisplayName,
                SpecialNotes            = e.SpecialNotes,
                CancellationReason      = e.CancellationReason,

                // ملخص أسماء الضيوف لعرضه في قوائم الحلقات
                GuestsDisplay = string.Join(" · ",
                    e.EpisodeGuests
                        .OrderBy(g => g.HostingTime)
                        .Select(g => g.Guest.FullName)),

                GuestItems = e.EpisodeGuests
                    .OrderBy(g => g.HostingTime)
                    .Select(g => new GuestDisplayItem(g.Guest.FullName, g.Topic, g.HostingTime))
                    .ToList(),

                CorrespondentItems = e.EpisodeCorrespondents
                    .Select(c => new EpisodeCorrespondentDto(
                        c.EpisodeCorrespondentId,
                        c.CorrespondentId,
                        c.Correspondent.FullName,
                        c.Topic,
                        c.HostingTime))
                    .ToList(),

                EmployeeItems = e.EpisodeEmployees
                    .Select(ee => new EpisodeEmployeeDto(ee.EpisodeEmployeeId, ee.EmployeeId))
                    .ToList(),

            }).ToListAsync();
    }

    public async Task<ActiveEpisodeDto?> GetActiveEpisodeByIdAsync(int episodeId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.Episodes
            .AsNoTracking()
            .Where(e => e.EpisodeId == episodeId)
            .Select(e => new ActiveEpisodeDto
            {
                EpisodeId               = e.EpisodeId,
                StatusId                = e.StatusId,
                ProgramId               = e.ProgramId,
                EpisodeName             = e.EpisodeName,
                ProgramName             = e.Program.ProgramName,
                ScheduledExecutionTime  = e.ScheduledExecutionTime,
                StatusText              = e.EpisodeStatus.DisplayName,
                SpecialNotes            = e.SpecialNotes,
                CancellationReason      = e.CancellationReason,

                GuestsDisplay = string.Join(" · ",
                    e.EpisodeGuests
                        .OrderBy(g => g.HostingTime)
                        .Select(g => g.Guest.FullName)),

                GuestItems = e.EpisodeGuests
                    .OrderBy(g => g.HostingTime)
                    .Select(g => new GuestDisplayItem(g.Guest.FullName, g.Topic, g.HostingTime))
                    .ToList(),

                CorrespondentItems = e.EpisodeCorrespondents
                    .Select(c => new EpisodeCorrespondentDto(
                        c.EpisodeCorrespondentId,
                        c.CorrespondentId,
                        c.Correspondent.FullName,
                        c.Topic,
                        c.HostingTime))
                    .ToList(),

                EmployeeItems = e.EpisodeEmployees
                    .Select(ee => new EpisodeEmployeeDto(ee.EpisodeEmployeeId, ee.EmployeeId))
                    .ToList(),

            }).FirstOrDefaultAsync();
    }

    /// <summary>
    /// يجلب الضيوف الكاملين لحلقة معينة بما فيهم الاسم الكامل والموضوع وساعة الاستضافة.
    /// يُستخدم عند فتح نموذج التعديل.
    /// </summary>
    public async Task<List<EpisodeGuestDto>> GetEpisodeGuestsAsync(int episodeId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.EpisodeGuests
            .AsNoTracking()
            .Where(eg => eg.EpisodeId == episodeId)
            .OrderBy(eg => eg.HostingTime)
            .Select(eg => new EpisodeGuestDto(
                eg.EpisodeGuestId,
                eg.GuestId,
                eg.Guest.FullName,
                eg.Topic,
                eg.HostingTime,
                eg.ClipNotes))
            .ToListAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Commands — Create / Update
    // ──────────────────────────────────────────────────────────────

    public async Task<Result<int>> CreateEpisodeAsync(EpisodeDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeManage);
        if (!permCheck.IsSuccess) return Result<int>.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();

        var episode = new Episode
        {
            ProgramId              = dto.ProgramId,
            EpisodeName            = dto.EpisodeName,
            ScheduledExecutionTime = dto.ScheduledDateTime,   // دمج التاريخ + الوقت
            StatusId               = EpisodeStatus.Planned,
            SpecialNotes           = dto.SpecialNotes
        };

        if (dto.Guests?.Count > 0)
            foreach (var g in dto.Guests)
                episode.EpisodeGuests.Add(new EpisodeGuest
                {
                    GuestId     = g.GuestId,
                    Topic       = g.Topic,
                    HostingTime = g.HostingTime,
                    ClipNotes   = g.ClipNotes
                });

        if (dto.Correspondents?.Count > 0)
            foreach (var c in dto.Correspondents)
                episode.EpisodeCorrespondents.Add(new EpisodeCorrespondent
                {
                    CorrespondentId = c.CorrespondentId,
                    Topic           = c.Topic,
                    HostingTime     = c.HostingTime
                });

        if (dto.Employees?.Count > 0)
            foreach (var ee in dto.Employees)
                episode.EpisodeEmployees.Add(new EpisodeEmployee { EmployeeId = ee.EmployeeId });

        context.Episodes.Add(episode);
        await context.SaveChangesAsync();
        return Result<int>.Success(episode.EpisodeId);
    }

    public async Task<Result> UpdateEpisodeAsync(EpisodeDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();

        var episode = await context.Episodes
            .Include(e => e.EpisodeGuests)
            .Include(e => e.EpisodeCorrespondents)
            .Include(e => e.EpisodeEmployees)
            .FirstOrDefaultAsync(e => e.EpisodeId == dto.EpisodeId);

        if (episode == null) return Result.Fail("الحلقة غير موجودة.");

        episode.ProgramId              = dto.ProgramId;
        episode.EpisodeName            = dto.EpisodeName;
        episode.ScheduledExecutionTime = dto.ScheduledDateTime;   // دمج التاريخ + الوقت
        episode.SpecialNotes           = dto.SpecialNotes;

        SyncGuests(episode.EpisodeGuests.ToList(), dto.Guests ?? [], episode);
        SyncCorrespondents(episode.EpisodeCorrespondents.ToList(), dto.Correspondents ?? [], episode);
        SyncEmployees(episode.EpisodeEmployees.ToList(), dto.Employees ?? [], episode);

        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ──────────────────────────────────────────────────────────────
    // Commands — Status
    // ──────────────────────────────────────────────────────────────

    public async Task<Result> UpdateStatusAsync(int episodeId, byte newStatusId, UserSession session)
    {
        using var context = await contextFactory.CreateDbContextAsync();
        var episode = await context.Episodes.FindAsync(episodeId);
        if (episode == null) return Result.Fail("الحلقة غير موجودة.");

        episode.StatusId = newStatusId;
        if (newStatusId == EpisodeStatus.Executed)
            episode.ActualExecutionTime = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> RevertEpisodeStatusAsync(int episodeId, string reason, UserSession session)
    {
        using var context = await contextFactory.CreateDbContextAsync();
        var episode = await context.Episodes.FindAsync(episodeId);
        if (episode == null) return Result.Fail("الحلقة غير موجودة.");

        switch (episode.StatusId)
        {
            case EpisodeStatus.Executed:
                var execLog = await context.ExecutionLogs
                    .Where(l => l.EpisodeId == episodeId && l.IsActive)
                    .OrderByDescending(l => l.CreatedAt)
                    .FirstOrDefaultAsync();
                if (execLog != null) execLog.IsActive = false;
                episode.StatusId = EpisodeStatus.Planned;
                episode.ActualExecutionTime = null;
                break;

            case EpisodeStatus.Published:
                var socialLogs = await context.SocialMediaPublishingLogs
                    .Where(l => l.EpisodeGuest.EpisodeId == episodeId && l.IsActive)
                    .ToListAsync();
                foreach (var log in socialLogs) log.IsActive = false;
                episode.StatusId = EpisodeStatus.Executed;
                break;

            case EpisodeStatus.WebsitePublished:
                var webLog = await context.WebsitePublishingLogs
                    .Where(l => l.EpisodeId == episodeId && l.IsActive)
                    .OrderByDescending(l => l.PublishedAt)
                    .FirstOrDefaultAsync();
                if (webLog != null) webLog.IsActive = false;
                episode.StatusId = EpisodeStatus.Published;
                break;
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> CancelEpisodeAsync(int episodeId, string reason, UserSession session)
    {
        using var context = await contextFactory.CreateDbContextAsync();
        var episode = await context.Episodes.FindAsync(episodeId);
        if (episode == null) return Result.Fail("الحلقة غير موجودة.");
        episode.StatusId = EpisodeStatus.Cancelled;
        episode.CancellationReason = reason;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateCancellationReasonAsync(int episodeId, string newReason, UserSession session)
    {
        using var context = await contextFactory.CreateDbContextAsync();
        var episode = await context.Episodes.FindAsync(episodeId);
        if (episode == null) return Result.Fail("الحلقة غير موجودة.");
        episode.CancellationReason = newReason;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ToggleWebsitePublishAsync(int episodeId, bool isPublished, UserSession session)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var episode = await context.Episodes.FindAsync(episodeId);
        if (episode == null) return Result.Fail("الحلقة غير موجودة.");

        if (isPublished)
        {
            context.WebsitePublishingLogs.Add(new WebsitePublishingLog
            {
                EpisodeId        = episodeId,
                PublishedByUserId= session.UserId,
                PublishedAt      = DateTime.UtcNow,
                MediaType        = MediaType.Audio
            });
            episode.StatusId = EpisodeStatus.WebsitePublished;
        }
        else
        {
            var log = await context.WebsitePublishingLogs
                .Where(l => l.EpisodeId == episodeId && l.IsActive)
                .OrderByDescending(l => l.PublishedAt)
                .FirstOrDefaultAsync();
            if (log != null) log.IsActive = false;
            episode.StatusId = EpisodeStatus.Published;
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    /// <summary>
    /// حذف ناعم للحلقة وجميع السجلات المرتبطة بها (ضيوف، مراسلون، موظفون).
    /// </summary>
    public async Task<Result> DeleteEpisodeAsync(int episodeId, UserSession session)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var episode = await context.Episodes
            .Include(e => e.EpisodeGuests)
            .Include(e => e.EpisodeCorrespondents)
            .Include(e => e.EpisodeEmployees)
            .FirstOrDefaultAsync(e => e.EpisodeId == episodeId);

        if (episode == null) return Result.Fail("الحلقة غير موجودة.");

        episode.IsActive = false;
        foreach (var g  in episode.EpisodeGuests)          g.IsActive  = false;
        foreach (var c  in episode.EpisodeCorrespondents)  c.IsActive  = false;
        foreach (var ee in episode.EpisodeEmployees)        ee.IsActive = false;

        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ──────────────────────────────────────────────────────────────
    // Sync Helpers
    // ──────────────────────────────────────────────────────────────

    private static void SyncGuests(List<EpisodeGuest> existing, List<EpisodeGuestDto> newItems, Episode ep)
    {
        var existingById = existing.ToDictionary(g => g.EpisodeGuestId);
        var newIds = newItems.Where(i => i.EpisodeGuestId != 0).Select(i => i.EpisodeGuestId).ToHashSet();

        // حذف ناعم للعناصر المحذوفة من الواجهة
        foreach (var ex in existing.Where(g => !newIds.Contains(g.EpisodeGuestId)))
            ep.EpisodeGuests.Remove(ex);

        foreach (var dto in newItems)
        {
            if (dto.EpisodeGuestId != 0 && existingById.TryGetValue(dto.EpisodeGuestId, out var ex))
            {
                // تحديث سجل موجود
                ex.GuestId     = dto.GuestId;
                ex.Topic       = dto.Topic;
                ex.HostingTime = dto.HostingTime;
                ex.ClipNotes   = dto.ClipNotes;
            }
            else
            {
                // إضافة سجل جديد
                ep.EpisodeGuests.Add(new EpisodeGuest
                {
                    GuestId     = dto.GuestId,
                    Topic       = dto.Topic,
                    HostingTime = dto.HostingTime,
                    ClipNotes   = dto.ClipNotes
                });
            }
        }
    }

    private static void SyncCorrespondents(List<EpisodeCorrespondent> existing, List<EpisodeCorrespondentDto> newItems, Episode ep)
    {
        var existingById = existing.ToDictionary(c => c.EpisodeCorrespondentId);
        var newIds = newItems.Where(i => i.Id != 0).Select(i => i.Id).ToHashSet();

        foreach (var ex in existing.Where(c => !newIds.Contains(c.EpisodeCorrespondentId)))
            ep.EpisodeCorrespondents.Remove(ex);

        foreach (var dto in newItems)
        {
            if (dto.Id != 0 && existingById.TryGetValue(dto.Id, out var ex))
            {
                ex.CorrespondentId = dto.CorrespondentId;
                ex.Topic           = dto.Topic;
                ex.HostingTime     = dto.HostingTime;
            }
            else
            {
                ep.EpisodeCorrespondents.Add(new EpisodeCorrespondent
                {
                    CorrespondentId = dto.CorrespondentId,
                    Topic           = dto.Topic,
                    HostingTime     = dto.HostingTime
                });
            }
        }
    }

    private static void SyncEmployees(List<EpisodeEmployee> existing, List<EpisodeEmployeeDto> newItems, Episode ep)
    {
        var existingById = existing.ToDictionary(e => e.EpisodeEmployeeId);
        var newIds = newItems.Where(i => i.Id != 0).Select(i => i.Id).ToHashSet();

        foreach (var ex in existing.Where(e => !newIds.Contains(e.EpisodeEmployeeId)))
            ep.EpisodeEmployees.Remove(ex);

        foreach (var dto in newItems)
        {
            if (dto.Id != 0 && existingById.TryGetValue(dto.Id, out var ex))
                ex.EmployeeId = dto.EmployeeId;
            else
                ep.EpisodeEmployees.Add(new EpisodeEmployee { EmployeeId = dto.EmployeeId });
        }
    }
}
