using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

// 1. استخدام الثوابت بدلاً من الأرقام السحرية لتسهيل القراءة
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
    // ─── قراءة الحلقات ──────────────────────────────────────────────────────────

    public async Task<List<ActiveEpisodeDto>> GetActiveEpisodesAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();

        // ✅ CancellationReason الآن جزء من الحلقة مباشرة — لا استعلام ثاني على AuditLogs
        var episodes = await context.Episodes
            .AsNoTracking()
            .OrderBy(e => e.ScheduledExecutionTime)
            .Select(e => new ActiveEpisodeDto
            {
                EpisodeId = e.EpisodeId,
                StatusId = e.StatusId,
                ProgramId = e.ProgramId,
                EpisodeName = e.EpisodeName,
                GuestItems = e.EpisodeGuests
                    .OrderBy(g => g.HostingTime)
                    .Select(g => new GuestDisplayItem(
                        g.Guest.FullName,
                        g.Topic,
                        g.HostingTime))
                    .ToList(),
                ProgramName = e.Program.ProgramName,
                ScheduledExecutionTime = e.ScheduledExecutionTime,
                StatusText = e.EpisodeStatus.DisplayName,
                SpecialNotes = e.SpecialNotes,
                CancellationReason = e.CancellationReason
            }).ToListAsync();

        return episodes;
    }

    public async Task<ActiveEpisodeDto?> GetActiveEpisodeByIdAsync(int episodeId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.Episodes
            .AsNoTracking()
            .Where(e => e.EpisodeId == episodeId)
            .Select(e => new ActiveEpisodeDto
            {
                EpisodeId = e.EpisodeId,
                StatusId = e.StatusId,
                ProgramId = e.ProgramId,
                EpisodeName = e.EpisodeName,
                GuestItems = e.EpisodeGuests
                    .OrderBy(g => g.HostingTime)
                    .Select(g => new GuestDisplayItem(
                        g.Guest.FullName,
                        g.Topic,
                        g.HostingTime))
                    .ToList(),
                ProgramName = e.Program.ProgramName,
                ScheduledExecutionTime = e.ScheduledExecutionTime,
                StatusText = e.EpisodeStatus.DisplayName,
                SpecialNotes = e.SpecialNotes,
                CancellationReason = e.CancellationReason
            }).FirstOrDefaultAsync();
    }

    // ─── إنشاء وتعديل ───────────────────────────────────────────────────────────

    public async Task<Result<int>> CreateEpisodeAsync(EpisodeDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeManage);
        if (!permCheck.IsSuccess) return Result<int>.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();

        var episode = new Episode
        {
            ProgramId = dto.ProgramId,
            EpisodeName = dto.EpisodeName,
            ScheduledExecutionTime = dto.ScheduledTime,
            StatusId = EpisodeStatus.Planned,
            SpecialNotes = dto.SpecialNotes
        };

        if (dto.Guests is { Count: > 0 })
            AddGuestsToEpisode(episode, dto.Guests);

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
            .FirstOrDefaultAsync(e => e.EpisodeId == dto.EpisodeId);

        if (episode == null) return Result.Fail("الحلقة غير موجودة.");

        episode.ProgramId = dto.ProgramId;
        episode.EpisodeName = dto.EpisodeName;
        episode.ScheduledExecutionTime = dto.ScheduledTime;
        episode.SpecialNotes = dto.SpecialNotes;

        if (dto.Guests is { Count: > 0 } || episode.EpisodeGuests.Count > 0)
            SyncGuests(episode.EpisodeGuests.ToList(), dto.Guests ?? [], episode);

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<List<EpisodeGuestDto>> GetEpisodeGuestsAsync(int episodeId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.EpisodeGuests
            .AsNoTracking()
            .Where(eg => eg.EpisodeId == episodeId)
            .Select(eg => new EpisodeGuestDto(
                eg.EpisodeGuestId,
                eg.GuestId,
                eg.Topic,
                eg.HostingTime))
            .ToListAsync();
    }

    // ─── انتقالات الحالة ─────────────────────────────────────────────────────────

    public async Task<Result> UpdateStatusAsync(int episodeId, byte newStatusId, UserSession session)
    {
        if (newStatusId == EpisodeStatus.Executed)
        {
            var permCheck = session.EnsurePermission(AppPermissions.EpisodeExecute);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);
        }
        if (newStatusId == EpisodeStatus.Published)
        {
            var permCheck = session.EnsurePermission(AppPermissions.EpisodePublish);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);
        }

        using var context = await contextFactory.CreateDbContextAsync();

        var episode = await context.Episodes.FindAsync(episodeId);
        if (episode == null) return Result.Fail("الحلقة غير موجودة.");

        if (episode.StatusId == EpisodeStatus.Published)
            return Result.Fail("لا يمكن تعديل حالة حلقة تم نشرها بالفعل.");

        if (episode.StatusId == EpisodeStatus.Executed && newStatusId == EpisodeStatus.Planned)
            return Result.Fail("لا يمكن إعادة حلقة منفذة إلى حالة الجدولة.");

        if (episode.StatusId == EpisodeStatus.Planned && newStatusId == EpisodeStatus.Published)
            return Result.Fail("يجب تنفيذ الحلقة وتوثيقها قبل عملية النشر الرقمي.");

        episode.StatusId = newStatusId;

        if (newStatusId == EpisodeStatus.Executed)
            episode.ActualExecutionTime = DateTime.UtcNow;

        try
        {
            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Fail("فشل التحديث: قام مستخدم آخر بتعديل حالة هذه الحلقة للتو.");
        }
    }

    /// <summary>
    /// تبديل حالة النشر على الموقع الإلكتروني.
    /// الإصلاح: ينشئ الآن سجلاً فعلياً في WebsitePublishingLogs ليدعم التراجع لاحقاً.
    /// </summary>
    public async Task<Result> ToggleWebsitePublishAsync(int episodeId, bool isPublished, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeWebPublish);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        await using var context = await contextFactory.CreateDbContextAsync();

        var episode = await context.Episodes.FindAsync(episodeId);
        if (episode == null)
            return Result.Fail("الحلقة غير موجودة.");

        if (isPublished)
        {
            if (episode.StatusId != EpisodeStatus.Published)
                return Result.Fail("لا يمكن نشر حلقة على الموقع قبل نشرها رقمياً.");

            // ✅ إصلاح: إنشاء سجل فعلي في WebsitePublishingLogs
            context.WebsitePublishingLogs.Add(new WebsitePublishingLog
            {
                EpisodeId = episodeId,
                PublishedByUserId = session.UserId,
                PublishedAt = DateTime.UtcNow,
                MediaType = MediaType.Audio  // القيمة الافتراضية — يمكن توسيعها مستقبلاً
            });

            episode.StatusId = EpisodeStatus.WebsitePublished;
        }
        else
        {
            if (episode.StatusId != EpisodeStatus.WebsitePublished)
                return Result.Fail("الحلقة غير منشورة على الموقع.");

            // ✅ Soft Delete لأحدث سجل نشر على الموقع
            var latestLog = await context.WebsitePublishingLogs
                .Where(l => l.EpisodeId == episodeId && l.IsActive)
                .OrderByDescending(l => l.PublishedAt)
                .FirstOrDefaultAsync();

            if (latestLog != null)
                latestLog.IsActive = false;

            episode.StatusId = EpisodeStatus.Published;
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ─── التراجع والإلغاء ────────────────────────────────────────────────────────

    public async Task<Result> RevertEpisodeStatusAsync(int episodeId, string reason, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeRevert);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Fail("يجب إدخال سبب التراجع.");

        using var context = await contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var episode = await context.Episodes.FindAsync(episodeId);
            if (episode == null) return Result.Fail("الحلقة غير موجودة.");

            switch (episode.StatusId)
            {
                case EpisodeStatus.Executed:
                {
                    // Soft Delete لسجل التنفيذ
                    var execLog = await context.ExecutionLogs
                        .Where(l => l.EpisodeId == episodeId && l.IsActive)
                        .OrderByDescending(l => l.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (execLog != null)
                        execLog.IsActive = false;

                    episode.StatusId = EpisodeStatus.Planned;
                    episode.ActualExecutionTime = null;
                    break;
                }

                case EpisodeStatus.Published:
                {
                    // ✅ إصلاح: التراجع من "منشورة رقمياً" يعني إلغاء سجلات وسائل التواصل
                    // (لا يوجد WebsitePublishingLog في هذه المرحلة — ذلك في WebsitePublished)
                    var socialLogs = await context.SocialMediaPublishingLogs
                        .Where(l => l.EpisodeGuest.EpisodeId == episodeId && l.IsActive)
                        .ToListAsync();

                    foreach (var log in socialLogs)
                        log.IsActive = false;

                    episode.StatusId = EpisodeStatus.Executed;
                    break;
                }

                case EpisodeStatus.WebsitePublished:
                {
                    // ✅ Soft Delete لسجل النشر على الموقع
                    var webLog = await context.WebsitePublishingLogs
                        .Where(l => l.EpisodeId == episodeId && l.IsActive)
                        .OrderByDescending(l => l.PublishedAt)
                        .FirstOrDefaultAsync();

                    if (webLog != null)
                        webLog.IsActive = false;

                    episode.StatusId = EpisodeStatus.Published;
                    break;
                }

                case EpisodeStatus.Planned:
                    return Result.Fail("الحلقة في حالة جدولة مسبقاً، لا يوجد شيء للتراجع عنه.");

                case EpisodeStatus.Cancelled:
                    return Result.Fail("لا يمكن التراجع عن حالة حلقة ملغاة.");

                default:
                    return Result.Fail("لا يمكن التراجع عن هذه الحالة.");
            }

            // تسجيل يدوي في AuditLogs (لأن هذا الإجراء لا يُغطيه الـ Interceptor بالكامل)
            context.AuditLogs.Add(new AuditLog
            {
                TableName = "Episodes",
                RecordId = episode.EpisodeId,
                Action = "REVERT",
                Reason = reason,
                UserId = session.UserId,
                ChangedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result.Success();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Result> CancelEpisodeAsync(int episodeId, string reason, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeRevert);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Fail("يجب إدخال سبب الإلغاء.");

        using var context = await contextFactory.CreateDbContextAsync();

        var episode = await context.Episodes.FindAsync(episodeId);
        if (episode == null) return Result.Fail("الحلقة غير موجودة.");

        if (episode.StatusId == EpisodeStatus.Cancelled)
            return Result.Fail("الحلقة ملغاة مسبقاً.");

        if (episode.StatusId == EpisodeStatus.Published || episode.StatusId == EpisodeStatus.WebsitePublished)
            return Result.Fail("لا يمكن إلغاء حلقة منشورة.");

        // ✅ إصلاح: تخزين السبب مباشرة في العمود بدلاً من AuditLogs فقط
        episode.StatusId = EpisodeStatus.Cancelled;
        episode.CancellationReason = reason;

        // نحتفظ بالتسجيل في AuditLogs أيضاً للتتبع التاريخي
        context.AuditLogs.Add(new AuditLog
        {
            TableName = "Episodes",
            RecordId = episodeId,
            Action = "CANCEL",
            Reason = reason,
            UserId = session.UserId,
            ChangedAt = DateTime.UtcNow
        });

        try
        {
            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Fail("فشل الإلغاء: قام مستخدم آخر بتعديل هذه الحلقة للتو.");
        }
    }

    public async Task<Result> UpdateCancellationReasonAsync(int episodeId, string newReason, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeRevert);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        if (string.IsNullOrWhiteSpace(newReason))
            return Result.Fail("يجب إدخال سبب الإلغاء.");

        using var context = await contextFactory.CreateDbContextAsync();

        var episode = await context.Episodes.FindAsync(episodeId);
        if (episode == null) return Result.Fail("الحلقة غير موجودة.");

        if (episode.StatusId != EpisodeStatus.Cancelled)
            return Result.Fail("لا يمكن تعديل سبب الإلغاء لحلقة غير ملغاة.");

        // ✅ إصلاح: تحديث العمود مباشرة — بدون تعديل JSON يدوي
        episode.CancellationReason = newReason;

        // تحديث آخر سجل CANCEL في AuditLogs أيضاً للاتساق
        var log = await context.AuditLogs
            .Where(a => a.TableName == "Episodes"
                     && a.Action == "CANCEL"
                     && a.RecordId == episodeId)
            .OrderByDescending(a => a.ChangedAt)
            .FirstOrDefaultAsync();

        if (log != null)
            log.Reason = newReason;

        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ─── حذف ────────────────────────────────────────────────────────────────────

    public async Task<Result> DeleteEpisodeAsync(int episodeId, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        await using var context = await contextFactory.CreateDbContextAsync();

        var episode = await context.Episodes
            .Include(e => e.EpisodeGuests)
            .Include(e => e.ExecutionLogs)
            .Include(e => e.WebsitePublishingLogs)
            .FirstOrDefaultAsync(e => e.EpisodeId == episodeId);

        if (episode == null) return Result.Fail("الحلقة غير موجودة.");

        if (episode.ExecutionLogs.Any(l => l.IsActive))
            return Result.Fail("لا يمكن حذف حلقة تم تنفيذها، يُرجى إلغاء التنفيذ أولاً.");

        bool hasSocialPublish = await context.SocialMediaPublishingLogs
            .AnyAsync(l => l.EpisodeGuest.EpisodeId == episodeId && l.IsActive);

        if (episode.WebsitePublishingLogs.Any(l => l.IsActive) || hasSocialPublish)
            return Result.Fail("لا يمكن حذف حلقة تم نشرها، يُرجى إلغاء النشر أولاً.");

        episode.IsActive = false;

        foreach (var guest in episode.EpisodeGuests)
            guest.IsActive = false;

        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// مزامنة ذكية للضيوف — المقارنة بالمفتاح الأساسي Id وليس GuestId
    /// </summary>
    private static void SyncGuests(
        List<EpisodeGuest> existingGuests,
        List<EpisodeGuestDto> newGuests,
        Episode episode)
    {
        var existingById = existingGuests.ToDictionary(g => g.EpisodeGuestId);
        var newIds = newGuests
            .Where(g => g.Id != 0)
            .Select(g => g.Id)
            .ToHashSet();

        // حذف الضيوف الذين لم يعودوا في القائمة
        foreach (var guest in existingGuests.Where(g => !newIds.Contains(g.EpisodeGuestId)).ToList())
            episode.EpisodeGuests.Remove(guest);

        // تحديث أو إضافة
        foreach (var dto in newGuests)
        {
            if (dto.Id != 0 && existingById.TryGetValue(dto.Id, out var existing))
            {
                existing.GuestId = dto.GuestId;
                existing.Topic = dto.Topic;
                existing.HostingTime = dto.HostingTime;
            }
            else
            {
                episode.EpisodeGuests.Add(new EpisodeGuest
                {
                    GuestId = dto.GuestId,
                    Topic = dto.Topic,
                    HostingTime = dto.HostingTime
                });
            }
        }
    }

    private static void AddGuestsToEpisode(Episode episode, List<EpisodeGuestDto> guestDtos)
    {
        foreach (var guestDto in guestDtos)
        {
            episode.EpisodeGuests.Add(new EpisodeGuest
            {
                GuestId = guestDto.GuestId,
                Topic = guestDto.Topic,
                HostingTime = guestDto.HostingTime
            });
        }
    }
}
