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

                // ✅ AsNoTracking + بناء الاستعلام ديناميكياً قبل التنفيذ
                var logsQuery = context.AuditLogs.AsNoTracking().AsQueryable();

                if (!string.IsNullOrEmpty(tableName))
                    logsQuery = logsQuery.Where(x => x.TableName == tableName);

                if (userId.HasValue)
                    logsQuery = logsQuery.Where(x => x.UserId == userId.Value);

                if (!string.IsNullOrEmpty(action))
                    logsQuery = logsQuery.Where(x => x.Action == action);

                if (fromDate.HasValue)
                    logsQuery = logsQuery.Where(x => x.ChangedAt >= fromDate.Value);

                if (toDate.HasValue)
                {
                    var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                    logsQuery = logsQuery.Where(x => x.ChangedAt <= endOfDay);
                }

                // ✅ Left join مع Users — الفلترة تطبّقت أولاً فيُقلّل حجم الـ join
                var list = await (from log in logsQuery.OrderByDescending(x => x.ChangedAt).Take(500)
                                  join u in context.Users.AsNoTracking()
                                      on log.UserId equals u.UserId into userJoin
                                  from u in userJoin.DefaultIfEmpty()
                                  select new AuditLogDto
                                  {
                                      AuditLogId   = log.AuditLogId,
                                      TableName    = log.TableName,
                                      RecordId     = log.RecordId,
                                      Action       = log.Action,
                                      OldValues    = log.OldValues,
                                      NewValues    = log.NewValues,
                                      Reason       = log.Reason,
                                      UserId       = log.UserId,
                                      Username     = u != null ? u.Username   : "غير معروف",
                                      UserFullName = u != null ? u.FullName   : "غير معروف",
                                      ChangedAt    = log.ChangedAt
                                  })
                                 .ToListAsync();

                return Result<List<AuditLogDto>>.Success(list);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An unexpected error occurred during processing");
                return Result<List<AuditLogDto>>.Fail($"حدث خطأ أثناء جلب سجل العمليات: {ex.Message}");
            }
        }

        public async Task<Result<List<User>>> GetAuditUsersAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                // ✅ Select الحقول المطلوبة فقط بدلاً من جلب الكيان كاملاً
                var users = await context.Users
                    .AsNoTracking()
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.FullName)
                    .Select(u => new User { UserId = u.UserId, FullName = u.FullName, Username = u.Username })
                    .ToListAsync();
                return Result<List<User>>.Success(users);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An unexpected error occurred during processing");
                return Result<List<User>>.Fail($"حدث خطأ أثناء جلب المستخدمين: {ex.Message}");
            }
        }
    }
}
