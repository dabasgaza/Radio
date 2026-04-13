using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services.Messaging;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BroadcastWorkflow.Services;

public interface IGuestService
{
    Task<List<Guest>> GetAllActiveAsync();
    Task CreateGuestAsync(GuestDto dto, UserSession session);
    Task UpdateGuestAsync(GuestDto dto, UserSession session);
    Task SoftDeleteGuestAsync(int guestId, UserSession session);
}

public class GuestService : IGuestService
{
    private readonly IDbContextFactory<BroadcastWorkflowDBContext> _contextFactory;
    private readonly IAuditService _audit;

    public GuestService(IDbContextFactory<BroadcastWorkflowDBContext> factory, IAuditService audit)
    { _contextFactory = factory; _audit = audit; }

    public async Task<List<Guest>> GetAllActiveAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Guests.AsNoTracking().Where(g => g.IsActive).ToListAsync();
    }

    public async Task CreateGuestAsync(GuestDto dto, UserSession session)
    {
        SecurityHelper.EnsurePermission(session, AppPermissions.GuestManage);
        if (string.IsNullOrEmpty(dto.PhoneNumber) && string.IsNullOrEmpty(dto.EmailAddress))
            throw new Exception("Guest must have a phone number or email address.");

        using var context = await _contextFactory.CreateDbContextAsync();
        var guest = new Guest
        {
            FullName = dto.FullName,
            Organization = dto.Organization,
            PhoneNumber = dto.PhoneNumber,
            EmailAddress = dto.EmailAddress,
            CreatedByUserId = session.UserId
        };
        context.Guests.Add(guest);
        await context.SaveChangesAsync();

        // إرسال الإشعار من قلب الخدمة
        MessageService.Current.ShowSuccess($"تم حفظ بيانات الضيف: {dto.FullName}");

        await _audit.LogActionAsync("Guests", guest.GuestId, "INSERT", null, dto, session.UserId);
    }

    public async Task UpdateGuestAsync(GuestDto dto, UserSession session)
    {
        SecurityHelper.EnsureRole(session, AppPermissions.GuestManage);

        using var context = await _contextFactory.CreateDbContextAsync();
        var guest = await context.Guests.FindAsync(dto.GuestId);
        if (guest == null) return;

        var oldData = new { guest.FullName, guest.PhoneNumber };
        guest.FullName = dto.FullName;
        guest.Organization = dto.Organization;
        guest.PhoneNumber = dto.PhoneNumber;
        guest.UpdatedAt = DateTime.UtcNow;
        guest.UpdatedByUserId = session.UserId;

        try
        {
            await context.SaveChangesAsync();
            MessageService.Current.ShowInfo($"تم تحديث بيانات الضيف: {dto.FullName}");

            await _audit.LogActionAsync("Guests", guest.GuestId, "UPDATE", oldData, dto, session.UserId);
        }
        catch (DbUpdateConcurrencyException) { throw new Exception("Record modified by another user."); }
    }

    public async Task SoftDeleteGuestAsync(int guestId, UserSession session)
    {
        SecurityHelper.EnsurePermission(session, AppPermissions.GuestManage); // 👈 احترافية وأمان أعلى

        //SecurityHelper.EnsureRole(session, "Coordination");

        using var context = await _contextFactory.CreateDbContextAsync();

        // بديل الـ Stored Procedure: التحقق من الارتباط بحلقات منفذة أو منشورة
        var hasExecutedEpisodes = await context.EpisodeGuests
            .AnyAsync(eg => eg.GuestId == guestId &&
                            eg.IsActive &&
                            (eg.Episode.Status == 1 || eg.Episode.Status == 2));

        if (hasExecutedEpisodes)
        {
            throw new Exception("لا يمكن حذف ضيف مرتبط بحلقات تم تنفيذها أو نشرها بالفعل.");
        }

        var guest = await context.Guests.FindAsync(guestId);
        if (guest != null)
        {
            guest.IsActive = false;
            guest.UpdatedAt = DateTime.UtcNow;
            guest.UpdatedByUserId = session.UserId;
            await context.SaveChangesAsync();
        }
    }
}