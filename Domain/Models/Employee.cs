namespace Domain.Models;

public class Employee : BaseEntity
{
    public int EmployeeId { get; set; }
    
    public string FullName { get; set; } = null!;
    
    public string? Notes { get; set; }

    public virtual ICollection<EpisodeEmployee> EpisodeEmployees { get; set; } = new List<EpisodeEmployee>();
}
