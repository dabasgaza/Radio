using System.ComponentModel.DataAnnotations;

namespace Domain.Models
{
    public class Permission
    {
        [Key]
        public int PermissionId { get; set; }

        [Required, MaxLength(100)]
        public string SystemName { get; set; } = null!; // مثال: GUEST_DELETE

        [Required, MaxLength(200)]
        public string DisplayName { get; set; } = null!; // مثال: حذف ضيف

        [Required, MaxLength(100)]
        public string Module { get; set; } = null!;      // مثال: الضيوف

        // العلاقة مع جدول الربط
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
