using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public interface IProgramService
{
    // ✨ إرجاع DTO بدلاً من Entity
    Task<List<ProgramDto>> GetAllActiveAsync();
    Task CreateProgramAsync(ProgramDto dto, UserSession session);
    Task UpdateProgramAsync(ProgramDto dto, UserSession session);
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
            .Where(p => p.IsActive) // حماية إضافية
            .Select(p => new ProgramDto
            (
                p.ProgramId,
                p.ProgramName,
                p.Category,
                p.ProgramDescription
            ))
            .ToListAsync();
    }

    public async Task CreateProgramAsync(ProgramDto dto, UserSession session)
    {
        // ✨ تصحيح الأمان: استخدام EnsurePermission بدلاً من EnsureRole
        session.EnsurePermission(AppPermissions.CoordinationManage);

        using var context = await contextFactory.CreateDbContextAsync();

        context.Programs.Add(new Program
        {
            ProgramName = dto.ProgramName,
            Category = dto.Category,
            ProgramDescription = dto.ProgramDescription
            // ❌ تم إزالة CreatedByUserId (الـ Interceptor سيعبئه)
        });

        await context.SaveChangesAsync();
    }

    public async Task UpdateProgramAsync(ProgramDto dto, UserSession session)
    {
        // ✨ تصحيح الأمان: استخدام EnsurePermission بدلاً من EnsureRole
        session.EnsurePermission(AppPermissions.CoordinationManage);

        using var context = await contextFactory.CreateDbContextAsync();

        var prog = await context.Programs.FindAsync(dto.ProgramId);

        // ✨ إطلاق خطأ بدلاً من الصمت
        if (prog == null) throw new KeyNotFoundException("البرنامج غير موجود.");

        prog.ProgramName = dto.ProgramName;
        prog.Category = dto.Category;
        prog.ProgramDescription = dto.ProgramDescription;

        // ❌ تم إزالة UpdatedAt و UpdatedByUserId (الـ Interceptor سيعبئهما)

        await context.SaveChangesAsync();
    }
}