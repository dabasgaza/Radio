using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Models.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // 1. المفتاح الأساسي
        builder.HasKey(e => e.UserId);

        // 2. الفهارس (Indexes)
        // ✨ فهرس فريد لاسم المستخدم (تم إعطاء اسم نظيف)
        builder.HasIndex(e => e.Username, "UQ_Users_Username")
              .IsUnique();

        // 3. إعدادات الخصائص
        builder.Property(e => e.FullName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(e => e.Username)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.PasswordHash)
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(e => e.EmailAddress)
               .HasMaxLength(255);

        builder.Property(e => e.PhoneNumber)
               .HasMaxLength(20);

        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.UpdatedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.IsActive)
               .HasDefaultValue(true);

        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // 4. العلاقات (Relationships)

        // علاقة الدور الوظيفي (Role)
        // ✨ إذا حُذف الدور، لا نريد حذف المستخدمين التابعين له، ولا نريد كسر الحقل إذا كان مطلوباً (Required)
        // Restrict يمنع حذف الدور ما دام هناك مستخدمين مرتبطين به.
        builder.HasOne(d => d.Role)
              .WithMany(p => p.Users)
              .HasForeignKey(d => d.RoleId)
              .OnDelete(DeleteBehavior.Restrict);

        // علاقة "أنشأ بواسطة" (Self-Referencing)
        // ✨ لا يمكن استخدام Cascade هنا أبداً! لا يمكن لحذف مستخدم أن يؤدي لحذف جميع المستخدمين الذين أنشأهم!
        builder.HasOne(d => d.CreatedByUser)
              .WithMany(p => p.InverseCreatedByUser) // تغيير الاسم من InverseCreatedByUser ليكون معبراً
              .HasForeignKey(d => d.CreatedByUserId)
              .OnDelete(DeleteBehavior.Restrict);

        // علاقة "عدّل بواسطة" (Self-Referencing)
        builder.HasOne(d => d.UpdatedByUser)
              .WithMany(p => p.InverseUpdatedByUser) // تغيير الاسم من InverseUpdatedByUser ليكون معبراً
              .HasForeignKey(d => d.UpdatedByUserId)
              .OnDelete(DeleteBehavior.Restrict);
    }
}