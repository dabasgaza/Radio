namespace DataAccess.DTOs
{
    public class PermissionViewModel
    {
        public int PermissionId { get; set; }
        public required string DisplayName { get; set; }
        public required string Module { get; set; }
        public bool IsAssigned { get; set; } = false;
    }
}
