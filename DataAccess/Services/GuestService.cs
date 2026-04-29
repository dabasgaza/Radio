using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Validation;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public interface IGuestService
{
    Task<List<GuestDto>> GetAllActiveAsync(); // ✨ إرجاع DTO بدلاً من Entity
    Task<Result> CreateGuestAsync(GuestDto dto, UserSession session);
    Task<Result> UpdateGuestAsync(GuestDto dto, UserSession session);
    Task<Result> SoftDeleteGuestAsync(int guestId, UserSession session);
}

// ✨ استخدام Primary Constructor وإزالة IAuditService
public class GuestService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IGuestService
{
    public async Task<List<GuestDto>> GetAllActiveAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();

        // ✨ استخدام AsNoTracking وإرجاع DTOs نظيفة للطبقة العليا
        return await context.Guests
            .AsNoTracking()
            .Where(g => g.IsActive) // ضمان إضافي (رغم وجود الـ Global Filter)
            .Select(g => new GuestDto
            (
                 g.GuestId,
                 g.FullName,
                 g.Organization,
                 g.PhoneNumber,
                 g.EmailAddress, string.Empty, string.Empty))
            .ToListAsync();
    }

    public async Task<Result> CreateGuestAsync(GuestDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.GuestManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        var validation = ValidationPipeline.ValidateGuest(dto);
        if (!validation.IsSuccess)
            return validation;

        using var context = await contextFactory.CreateDbContextAsync();

        var guest = new Guest
        {
            FullName = dto.FullName,
            Organization = dto.Organization,
            PhoneNumber = dto.PhoneNumber,
            EmailAddress = dto.EmailAddress
        };

        context.Guests.Add(guest);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> UpdateGuestAsync(GuestDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.GuestManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();
        var guest = await context.Guests.FindAsync(dto.GuestId);

        if (guest == null) return Result.Fail("الضيف غير موجود.");

        guest.FullName = dto.FullName;
        guest.Organization = dto.Organization;
        guest.PhoneNumber = dto.PhoneNumber;
        guest.EmailAddress = dto.EmailAddress;

        try
        {
            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entry = ex.Entries.Single();
            var dbValues = await entry.GetDatabaseValuesAsync();

            if (dbValues == null)
                return Result.Fail("تم حذف هذا السجل من قبل مستخدم آخر.");

            var diff = new Dictionary<string, object?>();
            foreach (var property in dbValues.Properties)
            {
                diff[property.Name] = dbValues[property.Name];
            }

            throw new ConcurrencyException(diff);
        }
    }

    public async Task<Result> SoftDeleteGuestAsync(int guestId, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.GuestManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();

        var hasExecutedEpisodes = await context.EpisodeGuests
            .AnyAsync(eg => eg.GuestId == guestId &&
                            eg.IsActive &&
                            (eg.Episode.StatusId == EpisodeStatus.Executed ||
                             eg.Episode.StatusId == EpisodeStatus.Published));

        if (hasExecutedEpisodes)
            return Result.Fail("لا يمكن حذف ضيف مرتبط بحلقات تم تنفيذها أو نشرها بالفعل.");

        var guest = await context.Guests.FindAsync(guestId);
        if (guest == null) return Result.Fail("الضيف غير موجود.");

        guest.IsActive = false;

        await context.SaveChangesAsync();
        return Result.Success();
    }
}