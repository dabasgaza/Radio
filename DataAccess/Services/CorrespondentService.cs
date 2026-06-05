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
        Task<Result> CreateAsync(CorrespondentDto dto, UserSession session);
        Task<Result> UpdateAsync(CorrespondentDto dto, UserSession session);
        Task<Result> SoftDeleteAsync(int id, UserSession session);
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

        public async Task<Result> CreateAsync(CorrespondentDto dto, UserSession session)
        {
            var permCheck = session.EnsurePermission(AppPermissions.CoordinationManage);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

            try
            {
                using var context = await contextFactory.CreateDbContextAsync();

                context.Correspondents.Add(new Correspondent
                {
                    FullName = dto.FullName,
                    PhoneNumber = dto.PhoneNumber,
                    AssignedLocations = dto.AssignedLocations
                });

                await context.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to create Correspondent: {CorrespondentName}", dto.FullName);
                return Result.Fail("حدث خطأ في قاعدة البيانات أثناء إضافة المراسل. يرجى المحاولة لاحقاً.");
            }
        }

        public async Task<Result> UpdateAsync(CorrespondentDto dto, UserSession session)
        {
            var permCheck = session.EnsurePermission(AppPermissions.CoordinationManage);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

            try
            {
                using var context = await contextFactory.CreateDbContextAsync();
                var cor = await context.Correspondents.FindAsync(dto.CorrespondentId);

                if (cor == null) return Result.Fail("المراسل غير موجود.");

                cor.FullName = dto.FullName;
                cor.PhoneNumber = dto.PhoneNumber;
                cor.AssignedLocations = dto.AssignedLocations;

                await context.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to update Correspondent: {CorrespondentId}, {CorrespondentName}", dto.CorrespondentId, dto.FullName);
                return Result.Fail("حدث خطأ في قاعدة البيانات أثناء تعديل بيانات المراسل. يرجى المحاولة لاحقاً.");
            }
        }

        public async Task<Result> SoftDeleteAsync(int id, UserSession session)
        {
            var permCheck = session.EnsurePermission(AppPermissions.CoordinationManage);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

            try
            {
                using var context = await contextFactory.CreateDbContextAsync();
                var cor = await context.Correspondents.FindAsync(id);

                if (cor == null) return Result.Fail("المراسل غير موجود.");

                cor.IsActive = false;

                await context.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to soft delete Correspondent: {CorrespondentId}", id);
                return Result.Fail("حدث خطأ في قاعدة البيانات أثناء حذف المراسل. يرجى المحاولة لاحقاً.");
            }
        }

        public async Task<List<CorrespondentCoverageDto>> GetCoverageAsync(int correspondentId)
        {
            using var context = await contextFactory.CreateDbContextAsync();

            // ✅ إزالة Include غير الضروري — Select يجلب العلاقة تلقائياً عبر SQL JOIN
            return await context.CorrespondentCoverages
                .AsNoTracking()
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