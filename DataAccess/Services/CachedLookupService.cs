using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DataAccess.Services;

/// <summary>
/// خدمة موحّدة للتخزين المؤقت لبيانات القوائم الثابتة (lookup tables)
/// تقلل استعلامات SQL المتكررة على جداول نادراً ما تتغير.
/// </summary>
public interface ICachedLookupService
{
    Task<List<StaffRoleDto>> GetStaffRolesAsync();
    Task<List<ProgramDto>> GetProgramsAsync();
    Task<List<GuestDto>> GetGuestsAsync();
    Task<List<CorrespondentDto>> GetCorrespondentsAsync();
    Task<Dictionary<byte, string>> GetEpisodeStatusesAsync();
    void Invalidate(string key);
    void InvalidateAll();
}

public class CachedLookupService(
    IDbContextFactory<BroadcastWorkflowDBContext> contextFactory,
    IMemoryCache cache) : ICachedLookupService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public async Task<List<StaffRoleDto>> GetStaffRolesAsync()
    {
        return await cache.GetOrCreateAsync("lookup:staffroles", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.StaffRoles
                .AsNoTracking()
                .Select(r => new StaffRoleDto(r.StaffRoleId, r.RoleName))
                .ToListAsync();
        }) ?? [];
    }

    public async Task<List<ProgramDto>> GetProgramsAsync()
    {
        return await cache.GetOrCreateAsync("lookup:programs", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Programs
                .AsNoTracking()
                .Select(p => new ProgramDto(p.ProgramId, p.ProgramName, p.Category, p.ProgramDescription))
                .ToListAsync();
        }) ?? [];
    }

    public async Task<List<GuestDto>> GetGuestsAsync()
    {
        return await cache.GetOrCreateAsync("lookup:guests", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Guests
                .AsNoTracking()
                .Select(g => new GuestDto(g.GuestId, g.FullName, g.Organization, g.PhoneNumber, g.EmailAddress, string.Empty, string.Empty))
                .ToListAsync();
        }) ?? [];
    }

    public async Task<List<CorrespondentDto>> GetCorrespondentsAsync()
    {
        return await cache.GetOrCreateAsync("lookup:correspondents", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Correspondents
                .AsNoTracking()
                .Select(c => new CorrespondentDto(c.CorrespondentId, c.FullName, c.PhoneNumber, c.AssignedLocations))
                .ToListAsync();
        }) ?? [];
    }

    public async Task<Dictionary<byte, string>> GetEpisodeStatusesAsync()
    {
        return await cache.GetOrCreateAsync("lookup:episodestatuses", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.EpisodeStatuses
                .AsNoTracking()
                .ToDictionaryAsync(s => s.StatusId, s => s.DisplayName);
        }) ?? new Dictionary<byte, string>();
    }

    public void Invalidate(string key)
    {
        cache.Remove(key);
    }

    public void InvalidateAll()
    {
        cache.Remove("lookup:staffroles");
        cache.Remove("lookup:programs");
        cache.Remove("lookup:guests");
        cache.Remove("lookup:correspondents");
        cache.Remove("lookup:episodestatuses");
    }
}