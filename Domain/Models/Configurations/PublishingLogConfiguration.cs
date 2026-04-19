using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Models.Configurations;

public class PublishingLogConfiguration : IEntityTypeConfiguration<PublishingLog>
{
    public void Configure(EntityTypeBuilder<PublishingLog> builder)
    {
        // 1. المفتاح الأساسي (إزالة الاسم القبيح المولد آلياً)
        builder.HasKey(e => e.PublishingLogId);

        // 2. إعدادات الخصائص (Properties)
        builder.Property(e => e.YouTubeUrl)
               .HasMaxLength(500);

        builder.Property(e => e.SoundCloudUrl)
               .HasMaxLength(500);

        builder.Property(e => e.FacebookUrl)
               .HasMaxLength(500);

        builder.Property(e => e.TwitterUrl)
               .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.PublishedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.IsActive)
               .HasDefaultValue(true);

        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // 3. العلاقات (Relationships)

        // علاقة الحلقة (Episode)
        // ✨ سجل النشر هو أرشيف. إذا حُذفت الحلقة، لا نريد حذف سجل النشر أو أن يكون EpisodeId = Null.
        // Restrict يضمن بقاء السجل التاريخي كما هو، ويمنع حذف الحلقة ما لم تُحذف سجلات نشرها يدوياً.
        builder.HasOne(d => d.Episode)
              .WithMany(p => p.PublishingLogs)
              .HasForeignKey(d => d.EpisodeId)
              .OnDelete(DeleteBehavior.Restrict);

        // علاقة "نُشر بواسطة" (PublishedByUser)
        // ✨ نفس المنطق: إذا حُذف حساب الموظف، يظل سجل النشر محتفظاً بمعرف الموظف الذي قام بالنشر.
        builder.HasOne(d => d.PublishedByUser)
              .WithMany(p => p.PublishingLogs)
              .HasForeignKey(d => d.PublishedByUserId)
              .OnDelete(DeleteBehavior.Restrict);
    }
}