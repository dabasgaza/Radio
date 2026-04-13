using BroadcastWorkflow.Services;
using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    public interface ICorrespondentService
    {
        Task<List<Correspondent>> GetAllActiveAsync();
        Task CreateAsync(CorrespondentDto dto, UserSession session);
        Task UpdateAsync(CorrespondentDto dto, UserSession session);
        Task SoftDeleteAsync(int id, UserSession session);
        Task<List<CorrespondentCoverage>> GetCoverageAsync(int correspondentId);
    }

    public class CorrespondentService : ICorrespondentService
    {
        private readonly IDbContextFactory<BroadcastWorkflowDBContext> _contextFactory;
        public CorrespondentService(IDbContextFactory<BroadcastWorkflowDBContext> factory) => _contextFactory = factory;

        public async Task<List<Correspondent>> GetAllActiveAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Correspondents.AsNoTracking().Where(c => c.IsActive).ToListAsync();
        }

        public async Task CreateAsync(CorrespondentDto dto, UserSession session)
        {
            SecurityHelper.EnsureRole(session, AppPermissions.CoordinationManage);
            using var context = await _contextFactory.CreateDbContextAsync();
            context.Correspondents.Add(new Correspondent
            {
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                AssignedLocations = dto.AssignedLocations,
                CreatedByUserId = session.UserId
            });
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(CorrespondentDto dto, UserSession session)
        {
            SecurityHelper.EnsureRole(session, AppPermissions.CoordinationManage);
            using var context = await _contextFactory.CreateDbContextAsync();
            var cor = await context.Correspondents.FindAsync(dto.CorrespondentId);
            if (cor == null) return;
            cor.FullName = dto.FullName;
            cor.PhoneNumber = dto.PhoneNumber;
            cor.AssignedLocations = dto.AssignedLocations;
            cor.UpdatedAt = DateTime.UtcNow;
            cor.UpdatedByUserId = session.UserId;
            await context.SaveChangesAsync();
        }

        public async Task SoftDeleteAsync(int id, UserSession session)
        {
            SecurityHelper.EnsureRole(session, AppPermissions.CoordinationManage);
            using var context = await _contextFactory.CreateDbContextAsync();
            var cor = await context.Correspondents.FindAsync(id);
            if (cor != null)
            {
                cor.IsActive = false;
                cor.UpdatedByUserId = session.UserId;
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<CorrespondentCoverage>> GetCoverageAsync(int correspondentId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.CorrespondentCoverages
                .AsNoTracking()
                .Include(c => c.Guest)
                .Where(c => c.CorrespondentId == correspondentId && c.IsActive)
                .ToListAsync();
        }

    }
}