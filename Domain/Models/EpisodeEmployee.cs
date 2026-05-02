namespace Domain.Models;

public class EpisodeEmployee : BaseEntity
{
    public int EpisodeEmployeeId { get; set; }
    
    public int EpisodeId { get; set; }
    public int EmployeeId { get; set; }
    public int StaffRoleId { get; set; }

    public virtual Episode Episode { get; set; } = null!;
    public virtual Employee Employee { get; set; } = null!;
    public virtual StaffRole StaffRole { get; set; } = null!;
}
