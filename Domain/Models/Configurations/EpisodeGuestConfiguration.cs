using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Models.Configurations;

public class EpisodeGuestConfiguration : IEntityTypeConfiguration<EpisodeGuest>
{
    public void Configure(EntityTypeBuilder<EpisodeGuest> builder)
    {
        // 1. المفتاح الأساسي (إزالة الاسم القبيح)
        builder.HasKey(e => e.EpisodeGuestId);

        // 2. الفهارس (Indexes)
        // ✨ فهرس فريد مركب لمنع إضافة نفس الضيف لنفس الحلقة مرتين
        builder.HasIndex(e => new { e.EpisodeId, e.GuestId }, "UQ_EpisodeGuests")
              .IsUnique();

        // 3. إعدادات الخصائص (Properties)
        builder.Property(e => e.Topic)
               .HasMaxLength(500);

        builder.Property(eg => eg.HostingTime)
       .HasColumnType("TIME")              // PostgreSQL: TIME / SQL Server: TIME
       .IsRequired(false);                // اختياري — بعض الضيوف قد لا يوجد وقت محدد


        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.IsActive)
               .HasDefaultValue(true);

        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // 4. العلاقات (Relationships)

        // علاقة "أنشأ بواسطة" (تمنع الحذف المتسلسل لحماية البيانات)
        builder.HasOne(d => d.CreatedByUser)
              .WithMany(p => p.EpisodeGuests)
              .HasForeignKey(d => d.CreatedByUserId)
              .OnDelete(DeleteBehavior.Restrict);

        // علاقة الحلقة (Episode)
        // ✨ جدول الربط عادة يتبع دورة حياة الآباء، لذا Cascade هنا منطقي
        // إذا حُذفت الحلقة فعلياً من قاعدة البيانات، يُحذف سجل الربط
        builder.HasOne(d => d.Episode)
              .WithMany(p => p.EpisodeGuests)
              .HasForeignKey(d => d.EpisodeId)
              .OnDelete(DeleteBehavior.Cascade);

        // علاقة الضيف (Guest)
        builder.HasOne(d => d.Guest)
              .WithMany(p => p.EpisodeGuests)
              .HasForeignKey(d => d.GuestId)
              .OnDelete(DeleteBehavior.Cascade);

        // 5. فلتر الحذف المنطقي (Soft Delete)
        // ✨ إضافته صراحةً لأن هذا الكيان قد لا يرث من BaseEntity أو لأمان إضافي
        builder.HasQueryFilter(eg => eg.IsActive);
    }
}