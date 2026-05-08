namespace DataAccess.DTOs
{
    public record PermissionDto(
        int PermissionId,
        string SystemName,
        string DisplayName,
        string Module);

    public record PermissionUpsertDto(
        string SystemName,
        string DisplayName,
        string Module);
}
