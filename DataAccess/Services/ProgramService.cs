using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public interface IProgramService
{
    // ✨ إرجاع DTO بدلاً من Entity
    Task<List<ProgramDto>> GetAllActiveAsync();
    Task<Result> CreateProgramAsync(ProgramDto dto, UserSession session);
    Task<Result> UpdateProgramAsync(ProgramDto dto, UserSession session);
    Task<Result> SoftDeleteAsync(int programId, UserSession session);
}

// ✨ استخدام Primary Constructor
public class ProgramService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IProgramService
{
    public async Task<List<ProgramDto>> GetAllActiveAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();

        // ✨ استخدام AsNoTracking وإرجاع DTOs
        return await context.Programs
            .AsNoTracking()
            .Select(p => new ProgramDto
            (
                p.ProgramId,
                p.ProgramName,
                p.Category,
                p.ProgramDescription
            ))
            .ToListAsync();
    }

    public async Task<Result> CreateProgramAsync(ProgramDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.CoordinationManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();

        context.Programs.Add(new Program
        {
            ProgramName = dto.ProgramName,
            Category = dto.Category,
            ProgramDescription = dto.ProgramDescription
            // ❌ تم إزالة CreatedByUserId (الـ Interceptor سيعبئه)
        });

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateProgramAsync(ProgramDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.CoordinationManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();

        var prog = await context.Programs.FindAsync(dto.ProgramId);

        // ✨ إطلاق خطأ بدلاً من الصمت
        if (prog == null) return Result.Fail("البرنامج غير موجود.");

        prog.ProgramName = dto.ProgramName;
        prog.Category = dto.Category;
        prog.ProgramDescription = dto.ProgramDescription;

        // ❌ تم إزالة UpdatedAt و UpdatedByUserId (الـ Interceptor سيعبئهما)

        await context.SaveChangesAsync();
        return Result.Success();
    }


    /// <summary>
    /// حذف برنامج بشكل ناعم (Soft Delete).
    /// </summary>
    /// <param name="programId">معرّف البرنامج المراد حذفه.</param>
    /// <param name="session">جلسة المستخدم الحالي للتدقيق.</param>
    /// <exception cref="UnauthorizedAccessException">إذا لم يكن لدى المستخدم صلاحية الحذف.</exception>
    /// <exception cref="InvalidOperationException">إذا كان البرنامج مرتبطاً بحلقات لا يمكن حذفه.</exception>
    public async Task<Result> SoftDeleteAsync(int programId, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.ProgramManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        await using var context = await contextFactory.CreateDbContextAsync();

        var program = await context.Programs
            .FindAsync(programId);
            
        if (program == null) return Result.Fail("البرنامج المحدد غير موجود أو تم حذفه مسبقاً.");

        // ── فحص وجود حلقات نشطة باستخدام AnyAsync بدلاً من Lazy Loading ──
        var hasActiveEpisodes = await context.Episodes
            .AnyAsync(e => e.ProgramId == programId);
        if (hasActiveEpisodes)
            return Result.Fail("لا يمكن حذف برنامج مرتبط بحلقات نشطة. يرجى حذف الحلقات أولاً.");

        program.IsActive = false;

        await context.SaveChangesAsync();
        return Result.Success();
    }
}