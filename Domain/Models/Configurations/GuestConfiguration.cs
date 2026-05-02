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



        // ملاحظة: علاقة الضيف مع الحلقات (EpisodeGuests) و التغطيات (CorrespondentCoverages) 
        // يتم تعريفها من الجانب الآخر (في EpisodeGuestConfiguration و CorrespondentCoverageConfiguration)
        // كقاعدة أفضل ممارسة في EF Core: قم بتعريف العلاقة في كيان واحد فقط لتجنب التكرار والتعارض.

        // 4. Seed Data
        builder.HasData(
            new Guest { GuestId = 1, FullName = "د. أحمد العمري", Organization = "جامعة الملك سعود", PhoneNumber = "0500000001", EmailAddress = "ahmed@example.com", CreatedByUserId = 1, CreatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc) },
            new Guest { GuestId = 2, FullName = "أ. سارة القحطاني", Organization = "وزارة الثقافة", PhoneNumber = "0500000002", EmailAddress = "sara@example.com", CreatedByUserId = 1, CreatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc) },
            new Guest { GuestId = 3, FullName = "م. خالد الشهري", Organization = "هيئة الرياضة", PhoneNumber = "0500000003", EmailAddress = "khalid@example.com", CreatedByUserId = 1, CreatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}