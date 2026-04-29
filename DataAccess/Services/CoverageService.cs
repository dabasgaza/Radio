using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    public interface ICoverageService
    {
        Task<List<CoverageDto>> GetAllAsync();
        Task<Result> CreateAsync(CoverageDto dto, UserSession session);
        Task<Result> UpdateAsync(CoverageDto dto, UserSession session);
        Task<Result> DeleteAsync(int id, UserSession session);
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

        public async Task<Result> CreateAsync(CoverageDto dto, UserSession session)
        {
            var permCheck = session.EnsurePermission(AppPermissions.CoordinationManage);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

            using var context = await contextFactory.CreateDbContextAsync();

            var coverage = new CorrespondentCoverage
            {
                CorrespondentId = dto.CorrespondentId,
                GuestId = dto.GuestId,
                Location = dto.Location,
                Topic = dto.Topic,
                ScheduledTime = dto.ScheduledTime,
                ActualTime = dto.ActualTime
            };

            context.CorrespondentCoverages.Add(coverage);
            await context.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> UpdateAsync(CoverageDto dto, UserSession session)
        {
            var permCheck = session.EnsurePermission(AppPermissions.CoordinationManage);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

            using var context = await contextFactory.CreateDbContextAsync();

            var coverage = await context.CorrespondentCoverages.FindAsync(dto.CoverageId);

            if (coverage == null) return Result.Fail("التغطية غير موجودة.");

            coverage.CorrespondentId = dto.CorrespondentId;
            coverage.GuestId = dto.GuestId;
            coverage.Location = dto.Location;
            coverage.Topic = dto.Topic;
            coverage.ScheduledTime = dto.ScheduledTime;
            coverage.ActualTime = dto.ActualTime;

            try
            {
                await context.SaveChangesAsync();
                return Result.Success();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail("فشل التحديث: قام مستخدم آخر بتعديل هذه التغطية للتو.");
            }
        }

        public async Task<Result> DeleteAsync(int id, UserSession session)
        {
            var permCheck = session.EnsurePermission(AppPermissions.CoordinationManage);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

            using var context = await contextFactory.CreateDbContextAsync();
            var coverage = await context.CorrespondentCoverages.FindAsync(id);

            if (coverage == null) return Result.Fail("التغطية غير موجودة.");

            coverage.IsActive = false;

            await context.SaveChangesAsync();
            return Result.Success();
        }
    }
}