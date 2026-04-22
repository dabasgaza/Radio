using DataAccess.Common;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace DataAccess.Data
{
    /// <summary>
    /// معترض عمليات الحفظ: يقوم بتحديث تواريخ التعديل تلقائياً وتسجيل التغييرات في جدول AuditLogs
    /// </summary>
    public class AuditInterceptor(CurrentSessionProvider sessionProvider) : SaveChangesInterceptor
    {
        // إعدادات الـ JSON المشتركة لتحسين الأداء وتجنب تخصيص الذاكرة في كل مرة
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false // عدم التنسيق لتوفير مساحة التخزين في قاعدة البيانات
        };

        // حقول لا يجب تسجيلها في الـ JSON لأنها بيانات تشويشية (Audit Fields)
        private static readonly HashSet<string> AuditFields = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(BaseEntity.UpdatedAt), nameof(BaseEntity.UpdatedByUserId),
        nameof(BaseEntity.CreatedAt), nameof(BaseEntity.CreatedByUserId),
        nameof(BaseEntity.RowVersion), nameof(BaseEntity.IsActive) // IsActive يتم تسجيله كفعل (SoftDelete) وليس كقيمة
    };

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

            var userId = sessionProvider.CurrentSession?.UserId;
            var auditEntries = new List<AuditLog>();

            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in entries)
            {
                // 1. تحديث حقول التدقيق الأساسية
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

                // 2. تحديد نوع الإجراء بدقة (خصوصاً الحذف المنطقي)
                var action = entry.State.ToString();
                if (entry.State == EntityState.Modified && entry.Entity is BaseEntity { IsActive: false } &&
                    entry.Property(nameof(BaseEntity.IsActive)).OriginalValue is true)
                {
                    action = "SOFT_DELETED"; // ✨ تمييز الحذف المنطقي
                }

                // 3. تجهيز سجل التدقيق
                var auditEntry = new AuditLog
                {
                    TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                    UserId = userId,
                    ChangedAt = DateTime.UtcNow,
                    Action = action.ToUpper()
                };

                // 4. جلب المفتاح الأساسي (حتى للسجلات المضافة Added)
                // 4. جلب المفتاح الأساسي
                var primaryKey = entry.Metadata.FindPrimaryKey();
                if (primaryKey != null)
                {
                    var pkName = primaryKey.Properties[0].Name;
                    var pkValue = entry.Property(pkName).CurrentValue;

                    // ✨ إذا كان السجل مضافاً حديثاً، المفتاح لا يزال غير موجود (غالباً 0 أو Null)
                    // سنقوم بتعيين RecordId إلى Null بدلاً من 0 ليكون أكثر دقة أرشيفياً
                    if (entry.State == EntityState.Added)
                    {
                        auditEntry.RecordId = null; // أو يمكنك تركه يحول الـ 0 إذا أردت
                    }
                    else
                    {
                        // للسجلات المعدلة أو المحذوفة، المفتاح موجود بالفعل
                        auditEntry.RecordId = pkValue != null ? Convert.ToInt32(pkValue) : null;
                    }
                }

                // 5. تسجيل القيم القديمة والجديدة (مع استبعاد حقوق التدقيق لتقليل الحجم)
                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();

                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.IsPrimaryKey() || AuditFields.Contains(property.Metadata.Name))
                        continue;

                    string propertyName = property.Metadata.Name;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            newValues[propertyName] = property.CurrentValue;
                            break;
                        case EntityState.Deleted: // الحذف الفعلي من قاعدة البيانات
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

                auditEntry.OldValues = oldValues.Count == 0 ? null : JsonSerializer.Serialize(oldValues, JsonOptions);
                auditEntry.NewValues = newValues.Count == 0 ? null : JsonSerializer.Serialize(newValues, JsonOptions);

                if (!auditEntry.TableName.Equals("AuditLogs", StringComparison.OrdinalIgnoreCase))
                {
                    auditEntries.Add(auditEntry);
                }
            }

            if (auditEntries.Any())
            {
                TryAddAuditLogs(context, auditEntries);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }


        /// <summary>
        /// محاولة تسجيل التغييرات في جدول AuditLogs بأمان.
        /// إذا فشل التسجيل، لا يمنع العملية الأساسية بل يُسجّل الخطأ في Debug Output.
        /// </summary>
        private void TryAddAuditLogs(DbContext context, List<AuditLog> entries)
        {
            try
            {
                context.Set<AuditLog>().AddRange(entries);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"⚠️ فشل تسجيل التدقيق (Audit)");
                System.Diagnostics.Debug.WriteLine($"السبب: {ex.GetBaseException().Message}");
                System.Diagnostics.Debug.WriteLine($"عدد السجلات المتأثرة: {entries.Count}");
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");
            }
        }
    }
}
