namespace DataAccess.DTOs;

public record EmployeeDto(
    int EmployeeId,
    string FullName,
    int? StaffRoleId,
    string? StaffRoleName,
    string? Notes);