using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public interface IReportsService
{
    Task<List<TodayEpisodeDto>> GetTodayEpisodesAsync();
    Task<Dictionary<string, int>> GetEpisodeStatusStatsAsync();
    Task<List<ActiveProgramDto>> GetMostActiveProgramsAsync();
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

        // ✨ تحسين الأداء: استخدام نطاق التاريخ بدلاً من .Date (SARGable Query)
        var today = DateTime.UtcNow.Date; // نستخدم UtcNow لأن قاعدة البيانات تخزن بالـ UTC
        var tomorrow = today.AddDays(1);

        return await context.Episodes
            .AsNoTracking()
            .Where(e => e.ScheduledExecutionTime >= today && e.ScheduledExecutionTime < tomorrow)
            .OrderBy(e => e.ScheduledExecutionTime)
            .Select(e => new TodayEpisodeDto
            (
                e.EpisodeId,
                e.EpisodeName,
                e.Program.ProgramName,
                e.Guest != null ? e.Guest.FullName : "بدون ضيف",
                e.ScheduledExecutionTime,
                e.EpisodeStatus.DisplayName
            ))
            .ToListAsync();
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
}