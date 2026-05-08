using DataAccess.Common;
using DataAccess.DTOs;

namespace DataAccess.Services
{
    public interface IPermissionService
    {
        Task<Result<List<PermissionDto>>> GetAllPermissionsAsync();
        Task<Result<PermissionDto>> GetPermissionByIdAsync(int id);
        Task<Result<int>> CreatePermissionAsync(PermissionUpsertDto dto);
        Task<Result> UpdatePermissionAsync(int id, PermissionUpsertDto dto);
        Task<Result> DeletePermissionAsync(int id);
    }
}
