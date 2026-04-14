using DataAccess.Common;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace DataAccess.Data
{
    /// <summary>
    /// هذا الكلاس يعترض أي عملية حفظ في قاعدة البيانات (SaveChanges)
    /// ليقوم تلقائياً بتحديث توقيت التعديل، وتسجيل سجل التتبع (AuditLog).
    /// </summary>
    /// <summary>
    /// معترض عمليات الحفظ: يقوم بتحديث تواريخ التعديل تلقائياً وتسجيل التغييرات في جدول AuditLogs
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
            var auditEntries = new List<AuditLog>();

            // الحصول على كافة السجلات التي تم تعديلها أو إضافتها أو حذفها
            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in entries)
            {
                // 1. تحديث حقول التدقيق الأساسية إذا كان الكيان يرث من BaseEntity
                if (entry.Entity is BaseEntity baseEntity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        baseEntity.CreatedAt = DateTime.UtcNow;
                        baseEntity.CreatedByUserId = userId;
                    }

                    baseEntity.UpdatedAt = DateTime.UtcNow;
                    baseEntity.UpdatedByUserId = userId;
                }

                // 2. تجهيز سجل التدقيق (Audit Log)
                var auditEntry = new AuditLog
                {
                    TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                    UserId = userId,
                    ChangedAt = DateTime.UtcNow,
                    Action = entry.State.ToString().ToUpper()
                };

                // 3. جلب المفتاح الأساسي ديناميكياً (حل مشكلة Invalid column name 'Id')
                var primaryKey = entry.Metadata.FindPrimaryKey();
                if (primaryKey != null && entry.State != EntityState.Added)
                {
                    var pkName = primaryKey.Properties[0].Name;
                    var pkValue = entry.Property(pkName).CurrentValue;
                    if (pkValue != null)
                    {
                        auditEntry.RecordId = Convert.ToInt32(pkValue);
                    }
                }

                // 4. تسجيل القيم القديمة والجديدة بصيغة JSON
                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();

                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.IsPrimaryKey() || property.Metadata.Name == "RowVersion")
                        continue;

                    string propertyName = property.Metadata.Name;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            newValues[propertyName] = property.CurrentValue;
                            break;

                        case EntityState.Deleted:
                            oldValues[propertyName] = property.OriginalValue;
                            break;

                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                oldValues[propertyName] = property.OriginalValue;
                                newValues[propertyName] = property.CurrentValue;
                            }
                            break;
                    }
                }

                auditEntry.OldValues = oldValues.Count == 0 ? null : JsonSerializer.Serialize(oldValues);
                auditEntry.NewValues = newValues.Count == 0 ? null : JsonSerializer.Serialize(newValues);

                // لا نقوم بإضافة سجلات التدقيق لجدول AuditLogs نفسه لتجنب حلقة مفرغة
                if (auditEntry.TableName != "AuditLogs")
                {
                    auditEntries.Add(auditEntry);
                }
            }

            // 5. إضافة سجلات التدقيق للـ Context
            if (auditEntries.Any())
            {
                context.Set<AuditLog>().AddRange(auditEntries);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
