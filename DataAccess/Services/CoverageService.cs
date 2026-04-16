using BroadcastWorkflow.Services;
using DataAccess.Common;
using DataAccess.DTOs;
using DataAccess.Services.Messaging;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.Services
{
    public interface ICoverageService
    {
        Task<List<CoverageDto>> GetAllAsync();
        Task CreateAsync(CoverageDto dto, UserSession session);
        Task UpdateAsync(CoverageDto dto, UserSession session);
        Task DeleteAsync(int id, UserSession session);
    }

    public class CoverageService : ICoverageService
    {
        private readonly IDbContextFactory<BroadcastWorkflowDBContext> _contextFactory;

        public CoverageService(IDbContextFactory<BroadcastWorkflowDBContext> factory) => _contextFactory = factory;

        public async Task<List<CoverageDto>> GetAllAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.CorrespondentCoverages
                .AsNoTracking()
                .Select(c => new CoverageDto
                {
                    CoverageId = c.CoverageId,
                    CorrespondentId = c.CorrespondentId,
                    CorrespondentName = c.Correspondent.FullName,
                    GuestId = c.GuestId,
                    GuestName = c.Guest != null ? c.Guest.FullName : "بدون ضيف",
                    Location = c.Location,
                    Topic = c.Topic,
                    ScheduledTime = c.ScheduledTime,
                    ActualTime = c.ActualTime
                })
                .ToListAsync();
        }

        public async Task CreateAsync(CoverageDto dto, UserSession session)
        {
            SecurityHelper.EnsurePermission(session, AppPermissions.CoordinationManage);
            using var context = await _contextFactory.CreateDbContextAsync();

            var coverage = new CorrespondentCoverage
            {
                CorrespondentId = dto.CorrespondentId,
                GuestId = dto.GuestId,
                Location = dto.Location,
                Topic = dto.Topic,
                ScheduledTime = dto.ScheduledTime,
                ActualTime = dto.ActualTime
            };

            context.CorrespondentCoverages.Add(coverage);
            await context.SaveChangesAsync();
            MessageService.Current.ShowSuccess("تمت إضافة التغطية الميدانية بنجاح.");
        }

        public async Task UpdateAsync(CoverageDto dto, UserSession session)
        {
            SecurityHelper.EnsurePermission(session, AppPermissions.CoordinationManage);
            using var context = await _contextFactory.CreateDbContextAsync();

            var coverage = await context.CorrespondentCoverages.FindAsync(dto.CoverageId);
            if (coverage == null) return;

            coverage.CorrespondentId = dto.CorrespondentId;
            coverage.GuestId = dto.GuestId;
            coverage.Location = dto.Location;
            coverage.Topic = dto.Topic;
            coverage.ScheduledTime = dto.ScheduledTime;
            coverage.ActualTime = dto.ActualTime;

            try
            {
                await context.SaveChangesAsync();
                MessageService.Current.ShowInfo("تم تحديث بيانات التغطية.");
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new Exception("فشل التحديث: قام مستخدم آخر بتعديل هذه التغطية للتو.");
            }
        }

        public async Task DeleteAsync(int id, UserSession session)
        {
            SecurityHelper.EnsurePermission(session, AppPermissions.CoordinationManage);

            using var context = await _contextFactory.CreateDbContextAsync();
            var coverage = await context.CorrespondentCoverages.FindAsync(id);
            if (coverage != null)
            {
                coverage.IsActive = false;
                await context.SaveChangesAsync();
                MessageService.Current.ShowSuccess("تم حذف التغطية بنجاح.");
            }
        }

    }
}
