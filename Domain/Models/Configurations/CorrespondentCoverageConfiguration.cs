using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Models.Configurations;

public class CorrespondentCoverageConfiguration : IEntityTypeConfiguration<CorrespondentCoverage>
{
    public void Configure(EntityTypeBuilder<CorrespondentCoverage> builder)
    {
        // 1. المفتاح الأساسي (إزالة الاسم القبيح المولد آلياً)
        builder.HasKey(e => e.CoverageId);

        // تحديد اسم الجدول صراحةً (لأن الاسم المفرد والجمع قد يختلفان)
        builder.ToTable("CorrespondentCoverage");

        // 2. إعدادات الخصائص (Properties)
        builder.Property(e => e.Location)
               .HasMaxLength(200);

        builder.Property(e => e.Topic)
               .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.UpdatedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.IsActive)
               .HasDefaultValue(true);

        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // 3. العلاقات (Relationships)

        // علاقة المراسل (Correspondent)
        // ✨ سجل التغطية تاريخي. إذا حُذف المراسل (Soft Delete غالباً)، نحتفظ بالسجل.
        builder.HasOne(d => d.Correspondent)
              .WithMany(p => p.CorrespondentCoverages)
              .HasForeignKey(d => d.CorrespondentId)
              .OnDelete(DeleteBehavior.Restrict);

        // علاقة الضيف (Guest)
        // ✨ نفس المنطق: حذف الضيف لا يجب أن يمسح سجل التغطية التي شارك فيها
        builder.HasOne(d => d.Guest)
              .WithMany(p => p.CorrespondentCoverages)
              .HasForeignKey(d => d.GuestId)
              .OnDelete(DeleteBehavior.Restrict);


    }
}