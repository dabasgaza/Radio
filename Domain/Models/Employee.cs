namespace Domain.Models;

public class Employee : BaseEntity
{
    public int EmployeeId { get; set; }
    
    public string FullName { get; set; } = null!;
    
    public int? StaffRoleId { get; set; }

    public string? Notes { get; set; }

    public virtual StaffRole? StaffRole { get; set; }
    public virtual ICollection<EpisodeEmployee> EpisodeEmployees { get; set; } = new List<EpisodeEmployee>();
}
