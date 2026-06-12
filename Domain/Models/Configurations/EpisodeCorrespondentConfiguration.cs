using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Models.Configurations;

public class EpisodeCorrespondentConfiguration : IEntityTypeConfiguration<EpisodeCorrespondent>
{
    public void Configure(EntityTypeBuilder<EpisodeCorrespondent> builder)
    {
        // 1. المفتاح الأساسي
        builder.HasKey(e => e.EpisodeCorrespondentId);

        // 2. إعدادات الخصائص
        builder.Property(e => e.Topic)
               .HasMaxLength(500);

        builder.Property(e => e.HostingTime)
               .HasColumnType("time")
               .IsRequired(false);

        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.IsActive)
               .HasDefaultValue(true);

        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // 3. العلاقات
        // علاقة الحلقة (Episode) — Cascade عند الحذف
        builder.HasOne(d => d.Episode)
               .WithMany(p => p.EpisodeCorrespondents)
               .HasForeignKey(d => d.EpisodeId)
               .OnDelete(DeleteBehavior.Cascade);

        // علاقة المراسل (Correspondent) — Restrict لمنع الحذف العشوائي للمراسلين
        builder.HasOne(d => d.Correspondent)
               .WithMany()
               .HasForeignKey(d => d.CorrespondentId)
               .OnDelete(DeleteBehavior.Restrict);

        // 4. الفهارس (Indexes)
        // منع إضافة نفس المراسل لنفس الحلقة مرتين وتأمين الاستعلامات
        builder.HasIndex(e => new { e.EpisodeId, e.CorrespondentId }, "UQ_EpisodeCorrespondents")
               .IsUnique();

        // فهرس لتسريع البحث والاستعلام حسب المراسل
        builder.HasIndex(e => e.CorrespondentId)
               .HasDatabaseName("IX_EpisodeCorrespondents_CorrespondentId");

        // 5. فلتر الحذف المنطقي (Soft Delete)
        // ✨ تم إزالة HasQueryFilter — الفلتر يُطبّق مركزياً عبر GenerateSoftDeleteFilter في BroadcastWorkflowDBContext
        // EpisodeCorrespondent يرث من BaseEntity لذلك يغطيه الفلتر الديناميكي تلقائياً
    }
}
