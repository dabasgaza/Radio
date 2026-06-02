using DataAccess.Common;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IDbContextFactory<BroadcastWorkflowDBContext> _dbContextFactory;

        public AuditLogService(IDbContextFactory<BroadcastWorkflowDBContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<Result<List<AuditLogDto>>> GetFilteredAuditLogsAsync(
            string? tableName = null, 
            int? userId = null, 
            string? action = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                
                var query = from log in context.AuditLogs
                            join user in context.Users on log.UserId equals user.UserId into userJoin
                            from u in userJoin.DefaultIfEmpty()
                            select new AuditLogDto
                            {
                                AuditLogId = log.AuditLogId,
                                TableName = log.TableName,
                                RecordId = log.RecordId,
                                Action = log.Action,
                                OldValues = log.OldValues,
                                NewValues = log.NewValues,
                                Reason = log.Reason,
                                UserId = log.UserId,
                                Username = u != null ? u.Username : "غير معروف",
                                UserFullName = u != null ? u.FullName : "غير معروف",
                                ChangedAt = log.ChangedAt
                            };

                // Apply filters
                if (!string.IsNullOrEmpty(tableName))
                {
                    query = query.Where(x => x.TableName == tableName);
                }

                if (userId.HasValue)
                {
                    query = query.Where(x => x.UserId == userId.Value);
                }

                if (!string.IsNullOrEmpty(action))
                {
                    query = query.Where(x => x.Action == action);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(x => x.ChangedAt >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    // To include the entire 'to' day, set to end of day
                    var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(x => x.ChangedAt <= endOfDay);
                }

                var list = await query
                    .OrderByDescending(x => x.ChangedAt)
                    .Take(500) // Limit to prevent performance issues
                    .ToListAsync();

                return Result<List<AuditLogDto>>.Success(list);
            }
            catch (Exception ex)
            {
                return Result<List<AuditLogDto>>.Fail($"حدث خطأ أثناء جلب سجل العمليات: {ex.Message}");
            }
        }

        public async Task<Result<List<User>>> GetAuditUsersAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var users = await context.Users
                    .AsNoTracking()
                    .OrderBy(x => x.FullName)
                    .ToListAsync();
                return Result<List<User>>.Success(users);
            }
            catch (Exception ex)
            {
                return Result<List<User>>.Fail($"حدث خطأ أثناء جلب المستخدمين: {ex.Message}");
            }
        }
    }
}
