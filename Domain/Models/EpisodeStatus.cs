using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models
{
    public class EpisodeStatus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        // لأننا سندخل الـ ID يدوياً (0, 1, 2, 3)
        public byte StatusId { get; set; }

        [Required, MaxLength(50)]
        public string StatusName { get; set; } = null!; // System Name (e.g., Planned)[Required, MaxLength(100)]
        public string DisplayName { get; set; } = null!; // Arabic Display Name (مجدولة)

        public byte SortOrder { get; set; }
    }
}
