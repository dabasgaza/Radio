using BroadcastWorkflow.Services;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    public interface IProgramService
    {
        Task<List<Program>> GetAllActiveAsync();
        Task CreateProgramAsync(ProgramDto dto, UserSession session);
        Task UpdateProgramAsync(ProgramDto dto, UserSession session);
    }

    public class ProgramService : IProgramService
    {
        private readonly IDbContextFactory<BroadcastWorkflowDBContext> _contextFactory;
        public ProgramService(IDbContextFactory<BroadcastWorkflowDBContext> factory) => _contextFactory = factory;

        public async Task<List<Program>> GetAllActiveAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Programs.AsNoTracking().Where(p => p.IsActive).ToListAsync();
        }

        public async Task CreateProgramAsync(ProgramDto dto, UserSession session)
        {
            SecurityHelper.EnsureRole(session, "Coordination");
            using var context = await _contextFactory.CreateDbContextAsync();
            context.Programs.Add(new Program
            {
                ProgramName = dto.ProgramName,
                Category = dto.Category,
                ProgramDescription = dto.ProgramDescription,
                CreatedByUserId = session.UserId
            });
            await context.SaveChangesAsync();
        }

        public async Task UpdateProgramAsync(ProgramDto dto, UserSession session)
        {
            SecurityHelper.EnsureRole(session, "Coordination");
            using var context = await _contextFactory.CreateDbContextAsync();
            var prog = await context.Programs.FindAsync(dto.ProgramId);
            if (prog == null) return;
            prog.ProgramName = dto.ProgramName;
            prog.Category = dto.Category;
            prog.ProgramDescription = dto.ProgramDescription;
            prog.UpdatedAt = DateTime.UtcNow;
            prog.UpdatedByUserId = session.UserId;
            await context.SaveChangesAsync();
        }
    }

}
