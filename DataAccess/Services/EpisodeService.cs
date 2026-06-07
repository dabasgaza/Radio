using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.ApplicationInsights;
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
    Task<List<ConflictInfo>> GetConflictingEpisodesAsync(int programId, DateTime scheduledTime, int? excludeEpisodeId = null);
    // ── Batch Operations — حل مشكلة N+1 في العمليات المجمعة ──
    Task<(int success, int fail)> CancelEpisodesBatchAsync(List<int> episodeIds, string reason, UserSession session);
    Task<(int success, int fail)> DeleteEpisodesBatchAsync(List<int> episodeIds, UserSession session);
}

public class EpisodeService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory, TelemetryClient telemetryClient) : IEpisodeService
{
    // ──────────────────────────────────────────────────────────────
    // Compiled Query for hot path
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Compiled query لاستعلام الحلقات النشطة — المسار الأكثر استخداماً
    /// </summary>
    private static readonly Func<BroadcastWorkflowDBContext, IAsyncEnumerable<ActiveEpisodeDto>> s_compiledActiveEpisodes =
        EF.CompileAsyncQuery((BroadcastWorkflowDBContext context) =>
            context.Episodes
                .AsNoTracking()
                .Where(e => e.IsActive)
                .AsSplitQuery()
                .OrderByDescending(e => e.ScheduledExecutionTime.HasValue ? e.ScheduledExecutionTime.Value.Date : DateTime.MinValue)
                .ThenBy(e => e.ScheduledExecutionTime)
                .Select(e => new ActiveEpisodeDto
                {
                    EpisodeId = e.EpisodeId,
                    StatusId = e.StatusId,
                    ProgramId = e.ProgramId,
                    EpisodeName = e.EpisodeName,
                    ProgramName = e.Program != null ? e.Program.ProgramName : null,
                    StatusText = e.EpisodeStatus != null ? e.EpisodeStatus.DisplayName : null,
                    ScheduledExecutionTime = e.ScheduledExecutionTime,
                    SpecialNotes = e.SpecialNotes,
                    CancellationReason = e.CancellationReason,
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
                        .Select(ee => new EpisodeEmployeeDto(
                            ee.EpisodeEmployeeId,
                            ee.EmployeeId,
                            ee.Employee.FullName,
                            ee.Employee.StaffRole != null ? ee.Employee.StaffRole.RoleName : null))
                        .ToList(),
                }));

    /// <summary>
    /// استعلام داخلي موحّد — يُستخدم من GetActiveEpisodesAsync و GetActiveEpisodeByIdAsync
    /// </summary>
    private static readonly Func<BroadcastWorkflowDBContext, int, IAsyncEnumerable<ActiveEpisodeDto>> s_compiledActiveEpisodeById =
        EF.CompileAsyncQuery((BroadcastWorkflowDBContext context, int episodeId) =>
            context.Episodes
                .AsNoTracking()
                .Where(e => e.IsActive && e.EpisodeId == episodeId)
                .AsSplitQuery()
                .Select(e => new ActiveEpisodeDto
                {
                    EpisodeId = e.EpisodeId,
                    StatusId = e.StatusId,
                    ProgramId = e.ProgramId,
                    EpisodeName = e.EpisodeName,
                    ProgramName = e.Program != null ? e.Program.ProgramName : null,
                    StatusText = e.EpisodeStatus != null ? e.EpisodeStatus.DisplayName : null,
                    ScheduledExecutionTime = e.ScheduledExecutionTime,
                    SpecialNotes = e.SpecialNotes,
                    CancellationReason = e.CancellationReason,
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
                        .Select(ee => new EpisodeEmployeeDto(
                            ee.EpisodeEmployeeId,
                            ee.EmployeeId,
                            ee.Employee.FullName,
                            ee.Employee.StaffRole != null ? ee.Employee.StaffRole.RoleName : null))
                        .ToList(),
                }));

    /// <summary>
    /// تطبيق string.Join على GuestsDisplay بعد الاستعلام
    /// </summary>
    private static void SetGuestsDisplay(ActiveEpisodeDto episode)
    {
        episode.GuestsDisplay = string.Join(" · ", episode.GuestItems.Select(g => g.Name));
    }

    // ──────────────────────────────────────────────────────────────
    // Queries
    // ──────────────────────────────────────────────────────────────

    public async Task<List<ActiveEpisodeDto>> GetActiveEpisodesAsync()
    {
        var operation = telemetryClient.StartOperation<Microsoft.ApplicationInsights.DataContracts.RequestTelemetry>("GetActiveEpisodes");
        try
        {
            using var context = await contextFactory.CreateDbContextAsync();

            // ✅ استخدام Compiled Query للمسار الأكثر حرارة
            var episodes = new List<ActiveEpisodeDto>();
            await foreach (var ep in s_compiledActiveEpisodes(context))
            {
                SetGuestsDisplay(ep);
                episodes.Add(ep);
            }

            telemetryClient.TrackMetric("ActiveEpisodesCount", episodes.Count);
            operation.Telemetry.Success = true;
            return episodes;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "An unexpected error occurred during processing");
            telemetryClient.TrackException(ex);
            operation.Telemetry.Success = false;
            throw;
        }
        finally
        {
            operation.Dispose();
        }
    }

    public async Task<ActiveEpisodeDto?> GetActiveEpisodeByIdAsync(int episodeId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // ✅ استخدام Compiled Query مع الباراميتر
        ActiveEpisodeDto? episode = null;
        await foreach (var ep in s_compiledActiveEpisodeById(context, episodeId))
        {
            episode = ep;
            break;
        }

        if (episode is null)
            return null;

        SetGuestsDisplay(episode);
        return episode;
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

        try
        {
            using var context = await contextFactory.CreateDbContextAsync();

            // ── التحقق من صحة البرنامج ──
            var programExists = await context.Programs.AnyAsync(p => p.ProgramId == dto.ProgramId && p.IsActive);
            if (!programExists) return Result<int>.Fail("البرنامج المحدد غير موجود أو غير نشط.");

            // ── التحقق من صحة الضيوف ──
            if (dto.Guests?.Count > 0)
            {
                var guestIds = dto.Guests.Select(g => g.GuestId).ToList();
                var existingCount = await context.Guests.CountAsync(g => guestIds.Contains(g.GuestId) && g.IsActive);
                if (existingCount != guestIds.Distinct().Count())
                {
                    return Result<int>.Fail("بعض الضيوف المحددين غير موجودين أو تم حذفهم.");
                }
            }

            // ── التحقق من صحة المراسلين ──
            if (dto.Correspondents?.Count > 0)
            {
                var corrIds = dto.Correspondents.Select(c => c.CorrespondentId).ToList();
                var existingCount = await context.Correspondents.CountAsync(c => corrIds.Contains(c.CorrespondentId) && c.IsActive);
                if (existingCount != corrIds.Distinct().Count())
                {
                    return Result<int>.Fail("بعض المراسلين المحددين غير موجودين أو تم حذفهم.");
                }
            }

            // ── التحقق من صحة الموظفين ──
            if (dto.Employees?.Count > 0)
            {
                var empIds = dto.Employees.Select(ee => ee.EmployeeId).ToList();
                var existingCount = await context.Employees.CountAsync(e => empIds.Contains(e.EmployeeId) && e.IsActive);
                if (existingCount != empIds.Distinct().Count())
                {
                    return Result<int>.Fail("بعض الموظفين المحددين غير موجودين في النظام. قد يكون تم حذفهم أو أنك تستخدم مسودة قديمة.");
                }
            }

            var episode = new Episode
            {
                ProgramId = dto.ProgramId,
                EpisodeName = dto.EpisodeName,
                ScheduledExecutionTime = dto.ScheduledDateTime,   // دمج التاريخ + الوقت
                StatusId = EpisodeStatus.Planned,
                SpecialNotes = dto.SpecialNotes
            };

            if (dto.Guests?.Count > 0)
                foreach (var g in dto.Guests)
                    episode.EpisodeGuests.Add(new EpisodeGuest
                    {
                        GuestId = g.GuestId,
                        Topic = g.Topic,
                        HostingTime = g.HostingTime,
                        ClipNotes = g.ClipNotes
                    });

            if (dto.Correspondents?.Count > 0)
                foreach (var c in dto.Correspondents)
                    episode.EpisodeCorrespondents.Add(new EpisodeCorrespondent
                    {
                        CorrespondentId = c.CorrespondentId,
                        Topic = c.Topic,
                        HostingTime = c.HostingTime
                    });

            if (dto.Employees?.Count > 0)
                foreach (var ee in dto.Employees)
                    episode.EpisodeEmployees.Add(new EpisodeEmployee { EmployeeId = ee.EmployeeId });

            context.Episodes.Add(episode);
            await context.SaveChangesAsync();
            return Result<int>.Success(episode.EpisodeId);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to create Episode: {EpisodeName}, ProgramId: {ProgramId}", dto.EpisodeName, dto.ProgramId);
            return Result<int>.Fail("حدث خطأ في قاعدة البيانات أثناء جدولة الحلقة. يرجى المحاولة لاحقاً.");
        }
    }

    public async Task<Result> UpdateEpisodeAsync(EpisodeDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        try
        {
            using var context = await contextFactory.CreateDbContextAsync();

            // ── التحقق من صحة البرنامج ──
            var programExists = await context.Programs.AnyAsync(p => p.ProgramId == dto.ProgramId && p.IsActive);
            if (!programExists) return Result.Fail("البرنامج المحدد غير موجود أو غير نشط.");

            // ── التحقق من صحة الضيوف ──
            if (dto.Guests?.Count > 0)
            {
                var guestIds = dto.Guests.Select(g => g.GuestId).ToList();
                var existingCount = await context.Guests.CountAsync(g => guestIds.Contains(g.GuestId) && g.IsActive);
                if (existingCount != guestIds.Distinct().Count())
                {
                    return Result.Fail("بعض الضيوف المحددين غير موجودين أو تم حذفهم.");
                }
            }

            // ── التحقق من صحة المراسلين ──
            if (dto.Correspondents?.Count > 0)
            {
                var corrIds = dto.Correspondents.Select(c => c.CorrespondentId).ToList();
                var existingCount = await context.Correspondents.CountAsync(c => corrIds.Contains(c.CorrespondentId) && c.IsActive);
                if (existingCount != corrIds.Distinct().Count())
                {
                    return Result.Fail("بعض المراسلين المحددين غير موجودين أو تم حذفهم.");
                }
            }

            // ── التحقق من صحة الموظفين ──
            if (dto.Employees?.Count > 0)
            {
                var empIds = dto.Employees.Select(ee => ee.EmployeeId).ToList();
                var existingCount = await context.Employees.CountAsync(e => empIds.Contains(e.EmployeeId) && e.IsActive);
                if (existingCount != empIds.Distinct().Count())
                {
                    return Result.Fail("بعض الموظفين المحددين غير موجودين في النظام. قد يكون تم حذفهم أو أنك تستخدم مسودة قديمة.");
                }
            }

            var episode = await context.Episodes
                .Include(e => e.EpisodeGuests)
                .Include(e => e.EpisodeCorrespondents)
                .Include(e => e.EpisodeEmployees)
                .FirstOrDefaultAsync(e => e.EpisodeId == dto.EpisodeId);

            if (episode == null) return Result.Fail("الحلقة غير موجودة.");

            // ── تحميل جميع السجلات بما فيها المحذوفة ناعمياً لتجنب تعارض الفهرس الفريد ──
            var allEpisodeEmployees = await context.EpisodeEmployees
                .IgnoreQueryFilters()
                .Where(ee => ee.EpisodeId == dto.EpisodeId)
                .ToListAsync();

            var allEpisodeGuests = await context.EpisodeGuests
                .IgnoreQueryFilters()
                .Where(eg => eg.EpisodeId == dto.EpisodeId)
                .ToListAsync();

            var allEpisodeCorrespondents = await context.EpisodeCorrespondents
                .IgnoreQueryFilters()
                .Where(ec => ec.EpisodeId == dto.EpisodeId)
                .ToListAsync();

            episode.ProgramId = dto.ProgramId;
            episode.EpisodeName = dto.EpisodeName;
            episode.ScheduledExecutionTime = dto.ScheduledDateTime;   // دمج التاريخ + الوقت
            episode.SpecialNotes = dto.SpecialNotes;

            SyncGuests(episode.EpisodeGuests.ToList(), allEpisodeGuests, dto.Guests ?? [], episode);
            SyncCorrespondents(episode.EpisodeCorrespondents.ToList(), allEpisodeCorrespondents, dto.Correspondents ?? [], episode);
            SyncEmployees(episode.EpisodeEmployees.ToList(), allEpisodeEmployees, dto.Employees ?? [], episode);

            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to update Episode: {EpisodeId}, {EpisodeName}", dto.EpisodeId, dto.EpisodeName);
            return Result.Fail("حدث خطأ في قاعدة البيانات أثناء تعديل بيانات الحلقة. يرجى المحاولة لاحقاً.");
        }
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
                EpisodeId = episodeId,
                PublishedByUserId = session.UserId,
                PublishedAt = DateTime.UtcNow,
                MediaType = MediaType.Audio
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
    /// يستخدم Select مباشر مع AsSplitQuery بدلاً من Include لتجنب جلب بيانات غير ضرورية.
    /// </summary>
    public async Task<Result> DeleteEpisodeAsync(int episodeId, UserSession session)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            // ✅ استخدام IgnoreQueryFilters لضمان إيجاد السجل حتى لو كانت حالته غير مستقرة برمجياً
            var episode = await context.Episodes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.EpisodeId == episodeId);

            if (episode == null) return Result.Fail("الحلقة غير موجودة.");
            if (!episode.IsActive) return Result.Success(); // محذوفة بالفعل

            // ✅ حذف ناعم للأطفال (الضيوف، المراسلين، الموظفين)
            var guestChildren = await context.EpisodeGuests
                .Where(g => g.EpisodeId == episodeId && g.IsActive)
                .ToListAsync();
            foreach (var g in guestChildren) g.IsActive = false;

            var corrChildren = await context.EpisodeCorrespondents
                .Where(c => c.EpisodeId == episodeId && c.IsActive)
                .ToListAsync();
            foreach (var c in corrChildren) c.IsActive = false;

            var empChildren = await context.EpisodeEmployees
                .Where(e => e.EpisodeId == episodeId && e.IsActive)
                .ToListAsync();
            foreach (var ee in empChildren) ee.IsActive = false;

            // ✅ تحديث حالة الحلقة الأساسية مع بيانات التدقيق
            episode.IsActive = false;
            episode.UpdatedAt = DateTime.UtcNow;
            episode.UpdatedByUserId = session.UserId;

            await context.SaveChangesAsync();

            telemetryClient.TrackEvent("EpisodeDeleted", new Dictionary<string, string>
            {
                { "EpisodeId", episodeId.ToString() },
                { "UserId", session.UserId.ToString() }
            });

            return Result.Success();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "An unexpected error occurred during processing");
            telemetryClient.TrackException(ex);
            return Result.Fail($"خطأ أثناء الحذف: {ex.Message}");
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Sync Helpers
    // ──────────────────────────────────────────────────────────────

    private static void SyncGuests(List<EpisodeGuest> existing, List<EpisodeGuest> allIncludingDeleted, List<EpisodeGuestDto> newItems, Episode ep)
    {
        var existingById = existing.ToDictionary(g => g.EpisodeGuestId);
        var newIds = newItems.Where(i => i.EpisodeGuestId != 0).Select(i => i.EpisodeGuestId).ToHashSet();

        // ✨ حذف ناعم للعناصر المحذوفة من الواجهة بدلاً من الحذف الصلب
        // كان يستخدم ep.EpisodeGuests.Remove(ex) مما يحذف السجل نهائياً من قاعدة البيانات
        // الآن يستخدم IsActive = false للحفاظ على سلامة البيانات وسجل التدقيق
        foreach (var ex in existing.Where(g => !newIds.Contains(g.EpisodeGuestId)))
            ex.IsActive = false;

        foreach (var dto in newItems)
        {
            if (dto.EpisodeGuestId != 0 && existingById.TryGetValue(dto.EpisodeGuestId, out var ex))
            {
                // تحديث سجل موجود
                ex.GuestId = dto.GuestId;
                ex.Topic = dto.Topic;
                ex.HostingTime = dto.HostingTime;
                ex.ClipNotes = dto.ClipNotes;
            }
            else
            {
                // ✨ التحقق من وجود سجل محذوف ناعمياً لنفس الضيف وإعادة تفعيله بدلاً من إدراج سجل مكرر
                // هذا يمنع تعارض الفهرس الفريد UQ_EpisodeGuests على (EpisodeId, GuestId)
                var softDeleted = allIncludingDeleted.FirstOrDefault(g => g.GuestId == dto.GuestId && !g.IsActive);
                if (softDeleted != null)
                {
                    softDeleted.IsActive = true;
                    softDeleted.Topic = dto.Topic;
                    softDeleted.HostingTime = dto.HostingTime;
                    softDeleted.ClipNotes = dto.ClipNotes;
                }
                else
                {
                    ep.EpisodeGuests.Add(new EpisodeGuest
                    {
                        GuestId = dto.GuestId,
                        Topic = dto.Topic,
                        HostingTime = dto.HostingTime,
                        ClipNotes = dto.ClipNotes
                    });
                }
            }
        }
    }

    private static void SyncCorrespondents(List<EpisodeCorrespondent> existing, List<EpisodeCorrespondent> allIncludingDeleted, List<EpisodeCorrespondentDto> newItems, Episode ep)
    {
        var existingById = existing.ToDictionary(c => c.EpisodeCorrespondentId);
        var newIds = newItems.Where(i => i.Id != 0).Select(i => i.Id).ToHashSet();

        // ✨ حذف ناعم للعناصر المحذوفة من الواجهة بدلاً من الحذف الصلب
        // كان يستخدم ep.EpisodeCorrespondents.Remove(ex) مما يحذف السجل نهائياً من قاعدة البيانات
        // الآن يستخدم IsActive = false للحفاظ على سلامة البيانات وسجل التدقيق
        foreach (var ex in existing.Where(c => !newIds.Contains(c.EpisodeCorrespondentId)))
            ex.IsActive = false;

        foreach (var dto in newItems)
        {
            if (dto.Id != 0 && existingById.TryGetValue(dto.Id, out var ex))
            {
                ex.CorrespondentId = dto.CorrespondentId;
                ex.Topic = dto.Topic;
                ex.HostingTime = dto.HostingTime;
            }
            else
            {
                // ✨ التحقق من وجود سجل محذوف ناعمياً لنفس المراسل وإعادة تفعيله بدلاً من إدراج سجل مكرر
                // هذا يمنع تعارض الفهرس الفريد UQ_EpisodeCorrespondents على (EpisodeId, CorrespondentId)
                var softDeleted = allIncludingDeleted.FirstOrDefault(c => c.CorrespondentId == dto.CorrespondentId && !c.IsActive);
                if (softDeleted != null)
                {
                    softDeleted.IsActive = true;
                    softDeleted.Topic = dto.Topic;
                    softDeleted.HostingTime = dto.HostingTime;
                }
                else
                {
                    ep.EpisodeCorrespondents.Add(new EpisodeCorrespondent
                    {
                        CorrespondentId = dto.CorrespondentId,
                        Topic = dto.Topic,
                        HostingTime = dto.HostingTime
                    });
                }
            }
        }
    }

    private static void SyncEmployees(List<EpisodeEmployee> existing, List<EpisodeEmployee> allIncludingDeleted, List<EpisodeEmployeeDto> newItems, Episode ep)
    {
        var existingById = existing.ToDictionary(e => e.EpisodeEmployeeId);
        var newIds = newItems.Where(i => i.Id != 0).Select(i => i.Id).ToHashSet();

        // حذف ناعم للعناصر المحذوفة من الواجهة بدلاً من الحذف الصلب
        foreach (var ex in existing.Where(e => !newIds.Contains(e.EpisodeEmployeeId)))
            ex.IsActive = false;

        foreach (var dto in newItems)
        {
            if (dto.Id != 0 && existingById.TryGetValue(dto.Id, out var ex))
            {
                ex.EmployeeId = dto.EmployeeId;
            }
            else
            {
                // التحقق من وجود سجل محذوف ناعمياً لنفس الموظف وإعادة تفعيله بدلاً من إدراج سجل مكرر
                var softDeleted = allIncludingDeleted.FirstOrDefault(e => e.EmployeeId == dto.EmployeeId && !e.IsActive);
                if (softDeleted != null)
                {
                    softDeleted.IsActive = true;
                }
                else
                {
                    ep.EpisodeEmployees.Add(new EpisodeEmployee { EmployeeId = dto.EmployeeId });
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Batch Operations — تنفيذ جماعي بجولة DB واحدة بدلاً من N جولات
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// إلغاء مجموعة حلقات دفعة واحدة باستخدام ExecuteUpdateAsync
    /// بدلاً من استدعاء CancelEpisodeAsync لكل حلقة على حدة (N+1 مشكلة).
    /// </summary>
    public async Task<(int success, int fail)> CancelEpisodesBatchAsync(List<int> episodeIds, string reason, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeManage);
        if (!permCheck.IsSuccess) return (0, episodeIds.Count);

        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            // ✅ جولة DB واحدة — تحديث جميع الحلقات المحددة بشرط أنها نشطة وغير ملغاة
            var affected = await context.Episodes
                .Where(e => episodeIds.Contains(e.EpisodeId) && e.IsActive && e.StatusId != EpisodeStatus.Cancelled)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.StatusId, EpisodeStatus.Cancelled)
                    .SetProperty(e => e.CancellationReason, reason)
                    .SetProperty(e => e.UpdatedAt, DateTime.UtcNow));

            return (affected, episodeIds.Count - affected);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to batch cancel {Count} episodes", episodeIds.Count);
            return (0, episodeIds.Count);
        }
    }

    /// <summary>
    /// حذف ناعم لمجموعة حلقات دفعة واحدة باستخدام ExecuteUpdateAsync
    /// بدلاً من استدعاء DeleteEpisodeAsync لكل حلقة على حدة (N+1 مشكلة).
    /// </summary>
    public async Task<(int success, int fail)> DeleteEpisodesBatchAsync(List<int> episodeIds, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeManage);
        if (!permCheck.IsSuccess) return (0, episodeIds.Count);

        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            // ✅ حذف ناعم جماعي للسجلات الفرعية أولاً
            await context.EpisodeGuests
                .Where(eg => episodeIds.Contains(eg.EpisodeId) && eg.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(eg => eg.IsActive, false));

            await context.EpisodeCorrespondents
                .Where(ec => episodeIds.Contains(ec.EpisodeId) && ec.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(ec => ec.IsActive, false));

            await context.EpisodeEmployees
                .Where(ee => episodeIds.Contains(ee.EpisodeId) && ee.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(ee => ee.IsActive, false));

            // ✅ حذف ناعم جماعي للحلقات الأساسية
            var affected = await context.Episodes
                .Where(e => episodeIds.Contains(e.EpisodeId) && e.IsActive)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.IsActive, false)
                    .SetProperty(e => e.UpdatedAt, DateTime.UtcNow)
                    .SetProperty(e => e.UpdatedByUserId, session.UserId));

            telemetryClient.TrackEvent("EpisodesBatchDeleted", new Dictionary<string, string>
            {
                { "Count", affected.ToString() },
                { "UserId", session.UserId.ToString() }
            });

            return (affected, episodeIds.Count - affected);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to batch delete {Count} episodes", episodeIds.Count);
            return (0, episodeIds.Count);
        }
    }

    // ═══════════════════════════════════════════════════════
    // كشف تعارض المواعيد
    // ═══════════════════════════════════════════════════════
    public async Task<List<ConflictInfo>> GetConflictingEpisodesAsync(int programId, DateTime scheduledTime, int? excludeEpisodeId = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // ✅ الفلترة بنافذة ساعة في SQL بدلاً من جلب الكل وفلترة في الذاكرة
        var windowStart = scheduledTime.AddHours(-1);
        var windowEnd = scheduledTime.AddHours(1);

        return await context.Episodes
            .AsNoTracking()
            .Where(e => e.IsActive
                     && e.StatusId != EpisodeStatus.Cancelled
                     && e.EpisodeId != (excludeEpisodeId ?? -1)
                     && e.ScheduledExecutionTime.HasValue
                     && e.ScheduledExecutionTime.Value > windowStart
                     && e.ScheduledExecutionTime.Value < windowEnd)
            .Select(e => new ConflictInfo(
                e.EpisodeId,
                e.EpisodeName ?? "",
                e.Program != null ? e.Program.ProgramName : "",
                e.ScheduledExecutionTime!.Value,
                e.ProgramId == programId ? ConflictLevel.High : ConflictLevel.Medium))
            .ToListAsync();
    }
}

public record ConflictInfo(int EpisodeId, string EpisodeName, string ProgramName, DateTime ScheduledTime, ConflictLevel Level);
public enum ConflictLevel { Medium, High }
