using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    public interface ICorrespondentService
    {
        // ✨ إرجاع DTOs بدلاً من الكيانات
        Task<List<CorrespondentDto>> GetAllActiveAsync();
        Task CreateAsync(CorrespondentDto dto, UserSession session);
        Task UpdateAsync(CorrespondentDto dto, UserSession session);
        Task SoftDeleteAsync(int id, UserSession session);
        Task<List<CorrespondentCoverageDto>> GetCoverageAsync(int correspondentId);
    }

    // ✨ استخدام Primary Constructor
    public class CorrespondentService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : ICorrespondentService
    {
        public async Task<List<CorrespondentDto>> GetAllActiveAsync()
        {
            using var context = await contextFactory.CreateDbContextAsync();

            // ✨ لا نحتاج لكتابة Where(c => c.IsActive) لأن الـ Global Query Filter يعمل تلقائياً
            return await context.Correspondents
                .AsNoTracking()
                .Select(c => new CorrespondentDto
                (
                    c.CorrespondentId,
                    c.FullName,
                    c.PhoneNumber,
                    c.AssignedLocations
                ))
                .ToListAsync();
        }

        public async Task CreateAsync(CorrespondentDto dto, UserSession session)
        {
            // ✨ تصحيح الأمان: استخدام EnsurePermission
            session.EnsurePermission(AppPermissions.CoordinationManage);

            using var context = await contextFactory.CreateDbContextAsync();

            context.Correspondents.Add(new Correspondent
            {
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                AssignedLocations = dto.AssignedLocations
                // ❌ تم إزالة CreatedByUserId (الـ Interceptor يعمل)
            });

            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(CorrespondentDto dto, UserSession session)
        {
            session.EnsurePermission(AppPermissions.CoordinationManage);

            using var context = await contextFactory.CreateDbContextAsync();
            var cor = await context.Correspondents.FindAsync(dto.CorrespondentId);

            // ✨ إطلاق خطأ بدلاً من الصمت
            if (cor == null) throw new KeyNotFoundException("المراسل غير موجود.");

            cor.FullName = dto.FullName;
            cor.PhoneNumber = dto.PhoneNumber;
            cor.AssignedLocations = dto.AssignedLocations;
            // ❌ تم إزالة UpdatedAt و UpdatedByUserId (الـ Interceptor يعمل)

            await context.SaveChangesAsync();
        }

        public async Task SoftDeleteAsync(int id, UserSession session)
        {
            session.EnsurePermission(AppPermissions.CoordinationManage);

            using var context = await contextFactory.CreateDbContextAsync();
            var cor = await context.Correspondents.FindAsync(id);

            // ✨ إطلاق خطأ بدلاً من الصمت
            if (cor == null) throw new KeyNotFoundException("المراسل غير موجود.");

            cor.IsActive = false;
            // ❌ تم إزالة UpdatedByUserId (الـ Interceptor يلتقط التغيير ويحدثها تلقائياً)

            await context.SaveChangesAsync();
        }

        public async Task<List<CorrespondentCoverageDto>> GetCoverageAsync(int correspondentId)
        {
            using var context = await contextFactory.CreateDbContextAsync();

            // ✨ إرجاع DTOs مع الـ Include للضيف
            return await context.CorrespondentCoverages
                .AsNoTracking()
                .Include(c => c.Guest)
                .Where(c => c.CorrespondentId == correspondentId)
                .Select(c => new CorrespondentCoverageDto
                {
                    CoverageId = c.CoverageId,
                    Topic = c.Topic,
                    Location = c.Location,
                    GuestName = c.Guest != null ? c.Guest.FullName : "لا يوجد ضيف"
                })
                .ToListAsync();
        }
    }
}