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
    public const byte WebsitePublished = 3;   // منشورة عبر الموقع
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

// ✨ استخدام C# 13 Primary Constructor
public class EpisodeService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IEpisodeService
{
    // تم إزالة IAuditService لأن الـ AuditInterceptor يتولى الأمر تلقائياً!

    public async Task<List<ActiveEpisodeDto>> GetActiveEpisodesAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();

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
                SpecialNotes = e.SpecialNotes
            }).ToListAsync();

        var cancelledEpisodes = episodes
            .Where(e => e.StatusId == EpisodeStatus.Cancelled)
            .ToList();

        if (cancelledEpisodes.Count > 0)
        {
            var cancelledIds = cancelledEpisodes.Select(e => e.EpisodeId).ToList();

            using var auditContext = await contextFactory.CreateDbContextAsync();
            var auditLogs = await auditContext.AuditLogs
                .AsNoTracking()
                .Where(a => a.TableName == "Episodes"
                         && a.Action == "CANCEL"
                         && a.RecordId != null
                         && cancelledIds.Contains(a.RecordId.Value))
                .OrderByDescending(a => a.ChangedAt)
                .Select(a => new { a.RecordId, a.Reason, a.NewValues })
                .ToListAsync();

            var reasonDict = auditLogs
                .GroupBy(a => a.RecordId!.Value)
                .ToDictionary(g => g.Key, g =>
                {
                    var latest = g.First();
                    var reason = latest.Reason ?? TryParseReasonFromJson(latest.NewValues);
                    return reason;
                });

            foreach (var ep in cancelledEpisodes)
            {
                ep.CancellationReason = reasonDict.TryGetValue(ep.EpisodeId, out var reason) && !string.IsNullOrWhiteSpace(reason)
                    ? reason
                    : "لم يتم تحديد سبب الإلغاء";
            }
        }
        else
        {
            foreach (var ep in episodes.Where(e => e.StatusId == EpisodeStatus.Cancelled))
                ep.CancellationReason = "لم يتم تحديد سبب الإلغاء";
        }

        return episodes;
    }

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
            StatusId = EpisodeStatus.Planned, // ✨ استخدام الثوابت
            SpecialNotes = dto.SpecialNotes
        };

        // ✅ إضافة الضيوف مع حماية من null
        if (dto.Guests is { Count: > 0 })
            AddGuestsToEpisode(episode, dto.Guests);

        context.Episodes.Add(episode);
        await context.SaveChangesAsync();
        return Result<int>.Success(episode.EpisodeId);
    }

    public async Task<ActiveEpisodeDto?> GetActiveEpisodeByIdAsync(int episodeId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var dto = await context.Episodes
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
                SpecialNotes = e.SpecialNotes
            }).FirstOrDefaultAsync();

        if (dto != null && dto.StatusId == EpisodeStatus.Cancelled)
        {
            await using var auditContext = await contextFactory.CreateDbContextAsync();
            var auditLog = await auditContext.AuditLogs
                .AsNoTracking()
                .Where(a => a.TableName == "Episodes"
                         && a.Action == "CANCEL"
                         && a.RecordId == episodeId)
                .OrderByDescending(a => a.ChangedAt)
                .Select(a => new { a.Reason, a.NewValues })
                .FirstOrDefaultAsync();

            if (auditLog != null)
            {
                var reason = auditLog.Reason ?? TryParseReasonFromJson(auditLog.NewValues);
                dto.CancellationReason = !string.IsNullOrWhiteSpace(reason) ? reason : "لم يتم تحديد سبب الإلغاء";
            }
            else
            {
                dto.CancellationReason = "لم يتم تحديد سبب الإلغاء";
            }
        }

        return dto;
    }

    public async Task<Result> UpdateEpisodeAsync(EpisodeDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);
        using var context = await contextFactory.CreateDbContextAsync();

        var episode = await context.Episodes
            .Include(e => e.EpisodeGuests)     // ✅ ضروري
            .FirstOrDefaultAsync(e => e.EpisodeId == dto.EpisodeId);

        if (episode == null) return Result.Fail("الحلقة غير موجودة.");

        // تحديث الحقول
        episode.ProgramId = dto.ProgramId;
        episode.EpisodeName = dto.EpisodeName;
        episode.ScheduledExecutionTime = dto.ScheduledTime;
        episode.SpecialNotes = dto.SpecialNotes;

        // ✅ مزامنة ذكية — فقط إذا تغيرت قائمة الضيوف فعلاً
        if (dto.Guests is { Count: > 0 } || episode.EpisodeGuests.Count > 0)
            SyncGuests(episode.EpisodeGuests.ToList(), dto.Guests ?? [], episode);

        // الـ Interceptor سيتولى تحديث UpdatedAt و UpdatedByUserId تلقائياً
        await context.SaveChangesAsync();
        return Result.Success();
    }

    /// <summary>
    /// مزامنة ذكية للضيوف — المقارنة بالمفتاح الأساسي Id وليس GuestId
    /// حتى عند تغيير الضيف يتم تحديث الصف نفسه (UPDATE) بدلاً من حذفه وإعادة إدراجه
    /// </summary>
    private static void SyncGuests(
        List<EpisodeGuest> existingGuests,
        List<EpisodeGuestDto> newGuests,
        Episode episode)
    {
        // 1. بناء قاموس بالضيوف الحاليين حسب المفتاح الأساسي
        var existingById = existingGuests.ToDictionary(g => g.EpisodeGuestId);
        var newIds = newGuests
            .Where(g => g.Id != 0)
            .Select(g => g.Id)
            .ToHashSet();

        // 2. حذف الصفوف المُزالة فقط (لم تعد موجودة في القائمة الجديدة)
        foreach (var guest in existingGuests.Where(g => !newIds.Contains(g.EpisodeGuestId)).ToList())
            episode.EpisodeGuests.Remove(guest);

        // 3. تحديث أو إضافة
        foreach (var dto in newGuests)
        {
            if (dto.Id != 0 && existingById.TryGetValue(dto.Id, out var existing))
            {
                // ✅ تحديث الصف الموجود — بما في ذلك تغيير GuestId
                existing.GuestId = dto.GuestId;
                existing.Topic = dto.Topic;
                existing.HostingTime = dto.HostingTime;
            }
            else
            {
                // ✅ ضيف جديد تماماً (Id == 0) — إضافة فقط
                episode.EpisodeGuests.Add(new EpisodeGuest
                {
                    GuestId = dto.GuestId,
                    Topic = dto.Topic,
                    HostingTime = dto.HostingTime
                });
            }
        }
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

    // تحديث الحالة مع قواعد الـ Workflow والصلاحيات
    public async Task<Result> UpdateStatusAsync(int episodeId, byte newStatusId, UserSession session)
    {
        // التحقق من الصلاحيات بناءً على الحالة الجديدة
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

        // ✨ قواعد الـ Workflow باستخدام الثوابت (أوضح وأسهل للصيانة)
        if (episode.StatusId == EpisodeStatus.Published)
            return Result.Fail("لا يمكن تعديل حالة حلقة تم نشرها بالفعل.");

        if (episode.StatusId == EpisodeStatus.Executed && newStatusId == EpisodeStatus.Planned)
            return Result.Fail("لا يمكن إعادة حلقة منفذة إلى حالة الجدولة.");

        if (episode.StatusId == EpisodeStatus.Planned && newStatusId == EpisodeStatus.Published)
            return Result.Fail("يجب تنفيذ الحلقة وتوثيقها قبل عملية النشر الرقمي.");

        // تحديث الحالة
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

    public async Task<Result> DeleteEpisodeAsync(int episodeId, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        await using var context = await contextFactory.CreateDbContextAsync();

        var episode = await context.Episodes
            .Include(e => e.EpisodeGuests)
            .FirstOrDefaultAsync(e => e.EpisodeId == episodeId);

        if (episode == null) return Result.Fail("الحلقة غير موجودة.");

        // ✅ قاعدة عمل: منع حذف الحلقات المنفّذة أو المنشورة
        if (episode.ExecutionLogs.Any())
            return Result.Fail("لا يمكن حذف حلقة تم تنفيذها، يُرجى إلغاء التنفيذ أولاً.");

        bool hasSocialPublish = await context.SocialMediaPublishingLogs.AnyAsync(l => l.EpisodeGuest.EpisodeId == episodeId);
        if (episode.WebsitePublishingLogs.Any() || hasSocialPublish)
            return Result.Fail("لا يمكن حذف حلقة تم نشرها، يُرجى إلغاء النشر أولاً.");

        episode.IsActive = false;

        foreach (var guest in episode.EpisodeGuests)
            guest.IsActive = false;

        await context.SaveChangesAsync();
        return Result.Success();
    }

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
            // يمكن النشر بالموقع فقط من حالة المنشورة رقمياً
            if (episode.StatusId != EpisodeStatus.Published)
                return Result.Fail("لا يمكن نشر حلقة على الموقع قبل نشرها رقمياً.");

            episode.StatusId = EpisodeStatus.WebsitePublished;
        }
        else
        {
            // يمكن إلغاء النشر فقط إذا كانت منشورة بالفعل على الموقع
            if (episode.StatusId != EpisodeStatus.WebsitePublished)
                return Result.Fail("الحلقة غير منشورة على الموقع.");

            episode.StatusId = EpisodeStatus.Published;
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

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
                    var pubLog = await context.WebsitePublishingLogs
                        .Where(l => l.EpisodeId == episodeId && l.IsActive)
                        .OrderByDescending(l => l.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (pubLog != null)
                        pubLog.IsActive = false;

                    episode.StatusId = EpisodeStatus.Executed;
                    break;
                }

                case EpisodeStatus.WebsitePublished:
                    episode.StatusId = EpisodeStatus.Published;
                    break;

                case EpisodeStatus.Planned:
                    return Result.Fail("الحلقة في حالة جدولة مسبقاً، لا يوجد شيء للتراجع عنه.");

                case EpisodeStatus.Cancelled:
                    return Result.Fail("لا يمكن التراجع عن حالة حلقة ملغاة.");

                default:
                    return Result.Fail("لا يمكن التراجع عن هذه الحالة.");
            }

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

        episode.StatusId = EpisodeStatus.Cancelled;

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

        var log = await context.AuditLogs
            .Where(a => a.TableName == "Episodes"
                     && a.Action == "CANCEL"
                     && a.RecordId == episodeId)
            .OrderByDescending(a => a.ChangedAt)
            .FirstOrDefaultAsync();

        if (log == null)
            return Result.Fail("لم يتم العثور على سجل إلغاء لهذه الحلقة.");

        log.Reason = newReason;

        if (!string.IsNullOrWhiteSpace(log.NewValues) && log.NewValues.Contains("\"Reason\":"))
        {
            var start = log.NewValues.IndexOf("\"Reason\":") + 9;
            var valueStart = log.NewValues.IndexOf('"', start) + 1;
            var valueEnd = log.NewValues.IndexOf('"', valueStart);
            if (valueStart > 0 && valueEnd > valueStart)
            {
                log.NewValues = log.NewValues[..valueStart] + newReason + log.NewValues[valueEnd..];
            }
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    #region Private Helpers

    private static string? TryParseReasonFromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var start = json.IndexOf("\"Reason\":");
            if (start < 0) return null;

            start = json.IndexOf('"', start + 9);
            if (start < 0) return null;

            var end = json.IndexOf('"', start + 1);
            if (end < 0) return null;

            return json.Substring(start + 1, end - start - 1);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// إضافة ضيوف إلى كيان الحلقة.
    /// </summary>
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

    #endregion
}
