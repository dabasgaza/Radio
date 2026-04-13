using DataAccess.Common;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DataAccess.Data
{
    /// <summary>
    /// هذا الكلاس يعترض أي عملية حفظ في قاعدة البيانات (SaveChanges)
    /// ليقوم تلقائياً بتحديث توقيت التعديل، وتسجيل سجل التتبع (AuditLog).
    /// </summary>
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly CurrentSessionProvider _sessionProvider;

        public AuditInterceptor(CurrentSessionProvider sessionProvider)
        {
            _sessionProvider = sessionProvider;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

            var userId = _sessionProvider.CurrentSession?.UserId;

            // 1. أتمتة حقول التحديث (UpdatedAt, UpdatedByUserId) لكل الجداول التي ترث من BaseEntity
            var entries = context.ChangeTracker.Entries<BaseEntity>().ToList();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedByUserId = userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedByUserId = userId;
                }
            }

            // 2. (اختياري متقدم): هنا يمكن كتابة كود لقراءة القيم القديمة والجديدة وحقنها في جدول AuditLogs 
            // ولكن تم الاكتفاء بأتمتة التواريخ لتبسيط الكود كبداية.

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
