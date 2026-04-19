using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Models.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        // 1. المفتاح الأساسي
        builder.HasKey(e => e.RoleId);

        // 2. إعدادات الخصائص
        builder.Property(e => e.RoleName)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(e => e.RoleDescription)
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

        // 3. الفلتر العالمي للحذف المنطقي (لأن Role لا يرث من BaseEntity)
        builder.HasQueryFilter(r => r.IsActive);
    }
}