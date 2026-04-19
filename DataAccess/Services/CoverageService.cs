using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    public interface ICoverageService
    {
        Task<List<CoverageDto>> GetAllAsync();
        Task CreateAsync(CoverageDto dto, UserSession session);
        Task UpdateAsync(CoverageDto dto, UserSession session);
        Task DeleteAsync(int id, UserSession session);
    }

    // ✨ استخدام Primary Constructor
    public class CoverageService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : ICoverageService
    {
        public async Task<List<CoverageDto>> GetAllAsync()
        {
            using var context = await contextFactory.CreateDbContextAsync();

            // ✅ كودك هنا ممتاز ولا يحتاج لتعديل (استخدام الـ DTO في الـ Select هو القمة)
            return await context.CorrespondentCoverages
                .AsNoTracking()
                .Select(c => new CoverageDto
                {
                    CoverageId = c.CoverageId,
                    CorrespondentId = c.CorrespondentId,
                    CorrespondentName = c.Correspondent.FullName,
                    GuestId = c.GuestId,
                    GuestName = c.Guest != null ? c.Guest.FullName : "بدون ضيف",
                    Location = c.Location,
                    Topic = c.Topic,
                    ScheduledTime = c.ScheduledTime,
                    ActualTime = c.ActualTime
                })
                .ToListAsync();
        }

        public async Task CreateAsync(CoverageDto dto, UserSession session)
        {
            // ✨ استخدام الـ Extension Method
            session.EnsurePermission(AppPermissions.CoordinationManage);

            using var context = await contextFactory.CreateDbContextAsync();

            var coverage = new CorrespondentCoverage
            {
                CorrespondentId = dto.CorrespondentId,
                GuestId = dto.GuestId,
                Location = dto.Location,
                Topic = dto.Topic,
                ScheduledTime = dto.ScheduledTime,
                ActualTime = dto.ActualTime
                // ❌ لا حاجة لتمرير CreatedByUserId، الـ Interceptor يتكفل به
            };

            context.CorrespondentCoverages.Add(coverage);
            await context.SaveChangesAsync();

            // ❌ تم إزالة MessageService
        }

        public async Task UpdateAsync(CoverageDto dto, UserSession session)
        {
            session.EnsurePermission(AppPermissions.CoordinationManage);

            using var context = await contextFactory.CreateDbContextAsync();

            var coverage = await context.CorrespondentCoverages.FindAsync(dto.CoverageId);

            // ✨ إطلاق خطأ بدلاً من الصمت
            if (coverage == null) throw new KeyNotFoundException("التغطية غير موجودة.");

            coverage.CorrespondentId = dto.CorrespondentId;
            coverage.GuestId = dto.GuestId;
            coverage.Location = dto.Location;
            coverage.Topic = dto.Topic;
            coverage.ScheduledTime = dto.ScheduledTime;
            coverage.ActualTime = dto.ActualTime;
            // ❌ لا حاجة لتحديث UpdatedAt يدوياً، الـ Interceptor يفعل ذلك

            try
            {
                await context.SaveChangesAsync();
                // ❌ تم إزالة MessageService
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException("فشل التحديث: قام مستخدم آخر بتعديل هذه التغطية للتو.");
            }
        }

        public async Task DeleteAsync(int id, UserSession session)
        {
            session.EnsurePermission(AppPermissions.CoordinationManage);

            using var context = await contextFactory.CreateDbContextAsync();
            var coverage = await context.CorrespondentCoverages.FindAsync(id);

            // ✨ إطلاق خطأ بدلاً من الصمت
            if (coverage == null) throw new KeyNotFoundException("التغطية غير موجودة.");

            coverage.IsActive = false;
            // ❌ لا حاجة لتحديث UpdatedByUserId يدوياً، الـ Interceptor يفعل ذلك عند تغير أي حقل

            await context.SaveChangesAsync();
            // ❌ تم إزالة MessageService
        }
    }
}