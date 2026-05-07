using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public interface IReportsService
{
    Task<List<TodayEpisodeDto>> GetTodayEpisodesAsync();
    Task<Dictionary<string, int>> GetEpisodeStatusStatsAsync();
    Task<List<ActiveProgramDto>> GetMostActiveProgramsAsync();
    Task<List<DateRangeEpisodeDto>> GetEpisodesByDateRangeAsync(DateTime from, DateTime to);
    Task<List<TopGuestDto>> GetTopGuestsAsync(int topCount = 10);
    Task<List<CancelledEpisodeDto>> GetCancelledEpisodesAsync();
}

// ✨ استخدام Primary Constructor
public class ReportsService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IReportsService
{
    public async Task<Dictionary<string, int>> GetEpisodeStatusStatsAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();

        var stats = await context.Episodes.AsNoTracking()
            .GroupBy(e => e.EpisodeStatus.StatusName)
            .Select(g => new { StatusName = g.Key, Count = g.Count() })
            .ToListAsync();

        return stats.ToDictionary(x => x.StatusName, x => x.Count);
    }

    public async Task<List<TodayEpisodeDto>> GetTodayEpisodesAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // ✅ نجلب البيانات الخام من DB أولاً مع Include للضيوف
        var raw = await context.Episodes
            .AsNoTracking()
            .Include(e => e.Program)
            .Include(e => e.EpisodeStatus)
            .Include(e => e.EpisodeGuests)
                .ThenInclude(eg => eg.Guest)
            .Where(e => e.ScheduledExecutionTime >= today && e.ScheduledExecutionTime < tomorrow)
            .OrderBy(e => e.ScheduledExecutionTime)
            .ToListAsync();

        // ✅ ثم نطبق التنسيق في الذاكرة (C#) لأن FormatGuestsDisplay لا تُترجم إلى SQL
        return raw.Select(e => new TodayEpisodeDto(
            e.EpisodeId,
            e.EpisodeName,
            e.Program.ProgramName,
            FormatGuestsDisplay(e.EpisodeGuests),
            e.ScheduledExecutionTime,
            e.EpisodeStatus.DisplayName
        )).ToList();
    }

    public async Task<List<ActiveProgramDto>> GetMostActiveProgramsAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();

        return await context.Programs
            .AsNoTracking()
            .Select(p => new ActiveProgramDto
            {
                ProgramName = p.ProgramName,
                Category = p.Category,
                TotalEpisodes = p.Episodes.Count(),
                // ✨ استخدام الثوابت بدلاً من الأرقام السحرية
                PublishedEpisodes = p.Episodes.Count(e => e.StatusId == EpisodeStatus.Published)
            })
            .OrderByDescending(x => x.TotalEpisodes)
            .Take(5)
            .ToListAsync();
    }

    public async Task<List<DateRangeEpisodeDto>> GetEpisodesByDateRangeAsync(DateTime from, DateTime to)
    {
        using var context = await contextFactory.CreateDbContextAsync();

        var toEndOfDay = to.Date.AddDays(1);

        // ✅ نجلب البيانات الخام مع Include للضيوف
        var raw = await context.Episodes
            .AsNoTracking()
            .Include(e => e.Program)
            .Include(e => e.EpisodeStatus)
            .Include(e => e.EpisodeGuests)
                .ThenInclude(eg => eg.Guest)
            .Where(e => e.ScheduledExecutionTime >= from.Date && e.ScheduledExecutionTime < toEndOfDay)
            .OrderBy(e => e.ScheduledExecutionTime)
            .ToListAsync();

        // ✅ ثم نطبق التنسيق في الذاكرة
        return raw.Select(e => new DateRangeEpisodeDto(
            e.EpisodeId,
            e.EpisodeName,
            e.Program.ProgramName,
            FormatGuestsDisplay(e.EpisodeGuests),
            e.ScheduledExecutionTime,
            e.EpisodeStatus.DisplayName
        )).ToList();
    }

    public async Task<List<TopGuestDto>> GetTopGuestsAsync(int topCount = 10)
    {
        using var context = await contextFactory.CreateDbContextAsync();

        // ── تنفيذ GROUP BY في قاعدة البيانات بدلاً من تحميل الكل في الذاكرة ──
        var grouped = await context.EpisodeGuests
            .AsNoTracking()
            .GroupBy(eg => eg.GuestId)
            .Select(g => new
            {
                GuestId = g.Key,
                AppearanceCount = g.Count(),
                LastEpisodeGuestId = g
                    .OrderByDescending(eg => eg.Episode.ScheduledExecutionTime)
                    .Select(eg => eg.EpisodeGuestId)
                    .FirstOrDefault()
            })
            .OrderByDescending(x => x.AppearanceCount)
            .Take(topCount)
            .ToListAsync();

        // ── جلب تفاصيل آخر ظهور فقط للضيوف المطلوبين ──
        var lastGuestIds = grouped.Select(x => x.LastEpisodeGuestId).ToList();
        var lastDetails = await context.EpisodeGuests
            .AsNoTracking()
            .Include(eg => eg.Guest)
            .Include(eg => eg.Episode)
            .Where(eg => lastGuestIds.Contains(eg.EpisodeGuestId))
            .ToDictionaryAsync(eg => eg.EpisodeGuestId);

        return grouped.Select((x, i) =>
        {
            lastDetails.TryGetValue(x.LastEpisodeGuestId, out var last);
            return new TopGuestDto(
                i + 1,
                x.GuestId,
                last?.Guest?.FullName ?? "غير معروف",
                last?.Guest?.Organization,
                x.AppearanceCount,
                last?.Topic,
                last?.Episode?.ScheduledExecutionTime
            );
        }).ToList();
    }

    public async Task<List<CancelledEpisodeDto>> GetCancelledEpisodesAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();

        // جلب الحلقات الملغاة — نتجاوز الـ soft-delete filter باستخدام IgnoreQueryFilters
        var episodes = await context.Episodes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(e => e.Program)
            .Where(e => e.StatusId == EpisodeStatus.Cancelled)
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync();

        if (episodes.Count == 0)
            return [];

        var episodeIds = episodes.Select(e => e.EpisodeId).ToList();

        // جلب سجلات الإلغاء من AuditLog
        var auditLogs = await context.AuditLogs
            .AsNoTracking()
            .Where(a => a.TableName == "Episodes"
                     && a.Action == "CANCEL"
                     && a.RecordId != null
                     && episodeIds.Contains(a.RecordId.Value))
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync();

        // جلب أسماء المستخدمين الذين ألغوا
        var userIds = auditLogs.Where(a => a.UserId.HasValue).Select(a => a.UserId!.Value).Distinct().ToList();
        var users = await context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .Select(u => new { u.UserId, u.FullName })
            .ToDictionaryAsync(u => u.UserId, u => u.FullName);

        // بناء قاموس آخر سجل إلغاء لكل حلقة
        var logDict = auditLogs
            .GroupBy(a => a.RecordId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        return episodes.Select(e =>
        {
            logDict.TryGetValue(e.EpisodeId, out var log);
            var cancelledBy = log?.UserId.HasValue == true
                ? users.GetValueOrDefault(log.UserId!.Value, "غير معروف")
                : "غير معروف";

            return new CancelledEpisodeDto(
                e.EpisodeId,
                e.EpisodeName,
                e.Program?.ProgramName ?? "—",
                e.ScheduledExecutionTime,
                log?.Reason ?? "لم يتم تحديد سبب",
                cancelledBy,
                log?.ChangedAt ?? e.UpdatedAt
            );
        }).ToList();
    }

    private static string FormatGuestsDisplay(IEnumerable<EpisodeGuest> guests)
    {
        var list = guests.OrderBy(g => g.HostingTime).ToList();

        if (list.Count == 0)
            return "لا يوجد ضيف";

        return string.Join(" ، ", list.Select(g =>
        {
            var name = g.Guest?.FullName ?? "غير معروف";
            if (g.HostingTime.HasValue)
                name += $" ({g.HostingTime.Value:hh\\:mm})";
            if (!string.IsNullOrWhiteSpace(g.Topic))
                name += $" — {g.Topic}";
            return name;
        }));
    }
}