using DataAccess.Common;
using DataAccess.DTOs;

namespace DataAccess.Services;

public interface IPlatformService
{
    Task<List<SocialMediaPlatformDto>> GetAllActiveAsync();
    Task<Result<int>> CreateAsync(SocialMediaPlatformDto dto, UserSession session);
    Task<Result> UpdateAsync(SocialMediaPlatformDto dto, UserSession session);
    Task<Result> DeleteAsync(int platformId, UserSession session);
}

