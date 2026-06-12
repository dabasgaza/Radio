using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Validation;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public interface IEmployeeService
{
    Task<List<EmployeeDto>> GetAllActiveAsync();
    Task<Result<int>> CreateAsync(EmployeeDto dto, UserSession session);
    Task<Result> UpdateAsync(EmployeeDto dto, UserSession session);
    Task<Result> SoftDeleteAsync(int employeeId, UserSession session);

    Task<List<StaffRoleDto>> GetAllRolesAsync();
    Task<Result<int>> CreateRoleAsync(StaffRoleDto dto, UserSession session);
    Task<Result> UpdateRoleAsync(StaffRoleDto dto, UserSession session);
    Task<Result> SoftDeleteRoleAsync(int roleId, UserSession session);
}

// ✨ استخدام Primary Constructor
public class EmployeeService(
    IDbContextFactory<BroadcastWorkflowDBContext> contextFactory,
    ICachedLookupService cachedLookup) : IEmployeeService
{
    // ──────────────────────────────────────────────────────────────
    // Compiled Queries — تقليل وقت ترجمة LINQ في المسارات الساخنة
    // ──────────────────────────────────────────────────────────────
    private static readonly Func<BroadcastWorkflowDBContext, IAsyncEnumerable<EmployeeDto>> s_compiledGetAllActive =
        EF.CompileAsyncQuery((BroadcastWorkflowDBContext context) =>
            context.Employees
                .AsNoTracking()
                .Select(e => new EmployeeDto(
                    e.EmployeeId,
                    e.FullName,
                    e.StaffRoleId,
                    e.StaffRole != null ? e.StaffRole.RoleName : null,
                    e.Notes)));

    private static readonly Func<BroadcastWorkflowDBContext, IAsyncEnumerable<StaffRoleDto>> s_compiledGetAllRoles =
        EF.CompileAsyncQuery((BroadcastWorkflowDBContext context) =>
            context.StaffRoles
                .AsNoTracking()
                .Where(r => r.IsActive)
                .Select(r => new StaffRoleDto(r.StaffRoleId, r.RoleName)));

    public async Task<List<EmployeeDto>> GetAllActiveAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();

        var result = new List<EmployeeDto>();
        await foreach (var dto in s_compiledGetAllActive(context))
            result.Add(dto);
        return result;
    }

    public async Task<Result<int>> CreateAsync(EmployeeDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.StaffManage);
        if (!permCheck.IsSuccess) return Result<int>.Fail(permCheck.ErrorMessage!);

        var validation = ValidationPipeline.ValidateEmployee(dto);
        if (!validation.IsSuccess) return Result<int>.Fail(validation.ErrorMessage!);

        try
        {
            using var context = await contextFactory.CreateDbContextAsync();

            var employee = new Employee
            {
                FullName = dto.FullName,
                StaffRoleId = dto.StaffRoleId,
                Notes = dto.Notes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Employees.Add(employee);
            await context.SaveChangesAsync();
            cachedLookup.InvalidateByEntity("Employee");
            return Result<int>.Success(employee.EmployeeId);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to create Employee: {FullName}", dto.FullName);
            return Result<int>.Fail("حدث خطأ في قاعدة البيانات أثناء إضافة الموظف. يرجى المحاولة لاحقاً.");
        }
    }

    public async Task<Result> UpdateAsync(EmployeeDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.StaffManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        var validation = ValidationPipeline.ValidateEmployee(dto);
        if (!validation.IsSuccess) return Result.Fail(validation.ErrorMessage!);

        try
        {
            using var context = await contextFactory.CreateDbContextAsync();

            var employee = await context.Employees.FindAsync(dto.EmployeeId);
            if (employee == null)
                return Result.Fail("الموظف غير موجود");

            employee.FullName = dto.FullName;
            employee.StaffRoleId = dto.StaffRoleId;
            employee.Notes = dto.Notes;
            employee.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            cachedLookup.InvalidateByEntity("Employee");
            return Result.Success();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to update Employee: {EmployeeId}, {FullName}", dto.EmployeeId, dto.FullName);
            return Result.Fail("حدث خطأ في قاعدة البيانات أثناء تعديل بيانات الموظف. يرجى المحاولة لاحقاً.");
        }
    }

    public async Task<Result> SoftDeleteAsync(int employeeId, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.StaffManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        try
        {
            using var context = await contextFactory.CreateDbContextAsync();

            var employee = await context.Employees.FindAsync(employeeId);
            if (employee == null)
                return Result.Fail("الموظف غير موجود");

            employee.IsActive = false;
            employee.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            cachedLookup.InvalidateByEntity("Employee");
            return Result.Success();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to soft delete Employee: {EmployeeId}", employeeId);
            return Result.Fail("حدث خطأ في قاعدة البيانات أثناء حذف الموظف. يرجى المحاولة لاحقاً.");
        }
    }

    public async Task<List<StaffRoleDto>> GetAllRolesAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();

        var result = new List<StaffRoleDto>();
        await foreach (var dto in s_compiledGetAllRoles(context))
            result.Add(dto);
        return result;
    }

    public async Task<Result<int>> CreateRoleAsync(StaffRoleDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.StaffManage);
        if (!permCheck.IsSuccess) return Result<int>.Fail(permCheck.ErrorMessage!);

        var validation = ValidationPipeline.ValidateStaffRole(dto);
        if (!validation.IsSuccess) return Result<int>.Fail(validation.ErrorMessage!);

        try
        {
            using var context = await contextFactory.CreateDbContextAsync();

            var role = new StaffRole
            {
                RoleName = dto.RoleName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.StaffRoles.Add(role);
            await context.SaveChangesAsync();
            cachedLookup.InvalidateByEntity("StaffRole");
            return Result<int>.Success(role.StaffRoleId);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to create StaffRole: {RoleName}", dto.RoleName);
            return Result<int>.Fail("حدث خطأ في قاعدة البيانات أثناء إضافة الدور الوظيفي. يرجى المحاولة لاحقاً.");
        }
    }

    public async Task<Result> UpdateRoleAsync(StaffRoleDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.StaffManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        var validation = ValidationPipeline.ValidateStaffRole(dto);
        if (!validation.IsSuccess) return Result.Fail(validation.ErrorMessage!);

        try
        {
            using var context = await contextFactory.CreateDbContextAsync();

            var role = await context.StaffRoles.FindAsync(dto.StaffRoleId);
            if (role == null)
                return Result.Fail("الدور الوظيفي غير موجود");

            role.RoleName = dto.RoleName;
            role.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            cachedLookup.InvalidateByEntity("StaffRole");
            return Result.Success();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to update StaffRole: {RoleId}, {RoleName}", dto.StaffRoleId, dto.RoleName);
            return Result.Fail("حدث خطأ في قاعدة البيانات أثناء تعديل الدور الوظيفي. يرجى المحاولة لاحقاً.");
        }
    }

    public async Task<Result> SoftDeleteRoleAsync(int roleId, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.StaffManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        try
        {
            using var context = await contextFactory.CreateDbContextAsync();

            var role = await context.StaffRoles.FindAsync(roleId);
            if (role == null)
                return Result.Fail("الدور الوظيفي غير موجود");

            role.IsActive = false;
            role.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            cachedLookup.InvalidateByEntity("StaffRole");
            return Result.Success();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to soft delete StaffRole: {RoleId}", roleId);
            return Result.Fail("حدث خطأ في قاعدة البيانات أثناء حذف الدور الوظيفي. يرجى المحاولة لاحقاً.");
        }
    }
}