using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Validation;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public interface IGuestService
{
    Task<List<GuestDto>> GetAllActiveAsync(); // ✨ إرجاع DTO بدلاً من Entity
    Task CreateGuestAsync(GuestDto dto, UserSession session);
    Task UpdateGuestAsync(GuestDto dto, UserSession session);
    Task SoftDeleteGuestAsync(int guestId, UserSession session);
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
                 g.EmailAddress,"",""
                 ))
            .ToListAsync();
    }

    public async Task CreateGuestAsync(GuestDto dto, UserSession session)
    {
        session.EnsurePermission(AppPermissions.GuestManage);
        ValidationPipeline.ValidateGuest(dto); // بافتراض أن هذا Static Helper

        using var context = await contextFactory.CreateDbContextAsync();

        var guest = new Guest
        {
            FullName = dto.FullName,
            Organization = dto.Organization,
            PhoneNumber = dto.PhoneNumber,
            EmailAddress = dto.EmailAddress
            // ❌ تم إزالة CreatedByUserId لأن الـ AuditInterceptor سيعبئه تلقائياً!
        };

        context.Guests.Add(guest);
        await context.SaveChangesAsync();

        // ❌ تم إزالة MessageService و _audit.LogActionAsync
    }

    public async Task UpdateGuestAsync(GuestDto dto, UserSession session)
    {
        session.EnsurePermission(AppPermissions.GuestManage);

        using var context = await contextFactory.CreateDbContextAsync();
        var guest = await context.Guests.FindAsync(dto.GuestId);

        if (guest == null) throw new KeyNotFoundException("الضيف غير موجود.");

        guest.FullName = dto.FullName;
        guest.Organization = dto.Organization;
        guest.PhoneNumber = dto.PhoneNumber;
        // ❌ تم إزالة UpdatedAt و UpdatedByUserId لأن الـ Interceptor يفعل ذلك!

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // ✨ معالجة التزامن الممتازة التي كتبتها، لكن بطريقة أنظف قليلاً
            var entry = ex.Entries.Single();
            var dbValues = await entry.GetDatabaseValuesAsync();

            if (dbValues == null)
                throw new InvalidOperationException("تم حذف هذا السجل من قبل مستخدم آخر.");

            var diff = new Dictionary<string, object?>();
            foreach (var property in dbValues.Properties)
            {
                diff[property.Name] = dbValues[property.Name];
            }

            throw new ConcurrencyException(diff); // تمرير القيم الحالية لواجهة المستخدم
        }
    }

    public async Task SoftDeleteGuestAsync(int guestId, UserSession session)
    {
        session.EnsurePermission(AppPermissions.GuestManage);

        using var context = await contextFactory.CreateDbContextAsync();

        // ✨ استخدام الثوابت بدلاً من الأرقام السحرية
        var hasExecutedEpisodes = await context.EpisodeGuests
            .AnyAsync(eg => eg.GuestId == guestId &&
                            eg.IsActive &&
                            (eg.Episode.StatusId == EpisodeStatus.Executed ||
                             eg.Episode.StatusId == EpisodeStatus.Published));

        if (hasExecutedEpisodes)
        {
            throw new InvalidOperationException("لا يمكن حذف ضيف مرتبط بحلقات تم تنفيذها أو نشرها بالفعل.");
        }

        var guest = await context.Guests.FindAsync(guestId);
        if (guest == null) throw new KeyNotFoundException("الضيف غير موجود.");

        guest.IsActive = false;
        
        
        // ❌ تم إزالة UpdatedAt و UpdatedByUserId لأن الـ Interceptor يفعل ذلك تلقائياً عند ملاحظة التعديل!

        await context.SaveChangesAsync();
    }
}