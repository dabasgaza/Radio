using DataAccess.Common;
using DataAccess.DTOs;
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
public class EmployeeService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IEmployeeService
{
    public async Task<List<EmployeeDto>> GetAllActiveAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();

        return await context.Employees
            .Include(e => e.StaffRole)
            .Select(e => new EmployeeDto(
                e.EmployeeId,
                e.FullName,
                e.StaffRoleId,
                e.StaffRole != null ? e.StaffRole.RoleName : null,
                e.Notes))
            .ToListAsync();
    }

    public async Task<Result<int>> CreateAsync(EmployeeDto dto, UserSession session)
    {
        if (!session.HasPermission(AppPermissions.StaffManage))
            return Result<int>.Fail("ليس لديك صلاحية إدارة الموظفين");

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
        return Result<int>.Success(employee.EmployeeId);
    }

    public async Task<Result> UpdateAsync(EmployeeDto dto, UserSession session)
    {
        if (!session.HasPermission(AppPermissions.StaffManage))
            return Result.Fail("ليس لديك صلاحية إدارة الموظفين");

        using var context = await contextFactory.CreateDbContextAsync();

        var employee = await context.Employees.FindAsync(dto.EmployeeId);
        if (employee == null)
            return Result.Fail("الموظف غير موجود");

        employee.FullName = dto.FullName;
        employee.StaffRoleId = dto.StaffRoleId;
        employee.Notes = dto.Notes;
        employee.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> SoftDeleteAsync(int employeeId, UserSession session)
    {
        if (!session.HasPermission(AppPermissions.StaffManage))
            return Result.Fail("ليس لديك صلاحية إدارة الموظفين");

        using var context = await contextFactory.CreateDbContextAsync();

        var employee = await context.Employees.FindAsync(employeeId);
        if (employee == null)
            return Result.Fail("الموظف غير موجود");

        employee.IsActive = false;
        employee.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<List<StaffRoleDto>> GetAllRolesAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();

        return await context.StaffRoles
            .Select(r => new StaffRoleDto(r.StaffRoleId, r.RoleName))
            .ToListAsync();
    }

    public async Task<Result<int>> CreateRoleAsync(StaffRoleDto dto, UserSession session)
    {
        if (!session.HasPermission(AppPermissions.StaffManage))
            return Result<int>.Fail("ليس لديك صلاحية إدارة الأدوار الوظيفية");

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
        return Result<int>.Success(role.StaffRoleId);
    }

    public async Task<Result> UpdateRoleAsync(StaffRoleDto dto, UserSession session)
    {
        if (!session.HasPermission(AppPermissions.StaffManage))
            return Result.Fail("ليس لديك صلاحية إدارة الأدوار الوظيفية");

        using var context = await contextFactory.CreateDbContextAsync();

        var role = await context.StaffRoles.FindAsync(dto.StaffRoleId);
        if (role == null)
            return Result.Fail("الدور الوظيفي غير موجود");

        role.RoleName = dto.RoleName;
        role.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> SoftDeleteRoleAsync(int roleId, UserSession session)
    {
        if (!session.HasPermission(AppPermissions.StaffManage))
            return Result.Fail("ليس لديك صلاحية إدارة الأدوار الوظيفية");

        using var context = await contextFactory.CreateDbContextAsync();

        var role = await context.StaffRoles.FindAsync(roleId);
        if (role == null)
            return Result.Fail("الدور الوظيفي غير موجود");

        role.IsActive = false;
        role.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return Result.Success();
    }
}