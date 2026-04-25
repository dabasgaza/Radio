using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Models.Configurations;

public class GuestConfiguration : IEntityTypeConfiguration<Guest>
{
    public void Configure(EntityTypeBuilder<Guest> builder)
    {
        // 1. المفتاح الأساسي (إزالة الاسم القبيح المولد آلياً)
        builder.HasKey(e => e.GuestId);

        // 2. إعدادات الخصائص (Properties)
        builder.Property(e => e.FullName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(e => e.PhoneNumber)
               .HasMaxLength(20);

        builder.Property(e => e.EmailAddress)
               .HasMaxLength(255);

        builder.Property(e => e.Organization)
               .HasMaxLength(200);

        // إعدادات BaseEntity
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

        // علاقة "أنشأ بواسطة"
        builder.HasOne(d => d.CreatedByUser)
              .WithMany(p => p.GuestCreatedByUsers)
              .HasForeignKey(d => d.CreatedByUserId)
              .OnDelete(DeleteBehavior.Restrict); // ✨ حاسم لمنع Multiple Cascade Paths

        // علاقة "عدّل بواسطة"
        builder.HasOne(d => d.UpdatedByUser)
              .WithMany(p => p.GuestUpdatedByUsers)
              .HasForeignKey(d => d.UpdatedByUserId)
              .OnDelete(DeleteBehavior.Restrict); // ✨ حاسم لمنع Multiple Cascade Paths

        // ملاحظة: علاقة الضيف مع الحلقات (EpisodeGuests) و التغطيات (CorrespondentCoverages) 
        // يتم تعريفها من الجانب الآخر (في EpisodeGuestConfiguration و CorrespondentCoverageConfiguration)
        // كقاعدة أفضل ممارسة في EF Core: قم بتعريف العلاقة في كيان واحد فقط لتجنب التكرار والتعارض.
    }
}