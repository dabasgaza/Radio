using BroadcastWorkflow.Services;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    public interface IPublishingService
    {
        Task LogPublishingAsync(PublishingLog log, UserSession session);
    }

    public class PublishingService : IPublishingService
    {
        private readonly IDbContextFactory<BroadcastWorkflowDBContext> _contextFactory;
        public PublishingService(IDbContextFactory<BroadcastWorkflowDBContext> factory) => _contextFactory = factory;

        public async Task LogPublishingAsync(PublishingLog log, UserSession session)
        {
            SecurityHelper.EnsureRole(session, "Coordination", "Publishing");
            using var context = await _contextFactory.CreateDbContextAsync();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                log.PublishedByUserId = session.UserId;
                context.PublishingLogs.Add(log);

                // Update Episode Status via Stored Procedure
                await context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateEpisodeStatus @p0, 2, @p1", log.EpisodeId, session.UserId);

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

}
