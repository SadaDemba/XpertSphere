namespace XpertSphere.MonolithApi.DTOs.User;

/// <summary>
/// DTO for bulk update operations
/// </summary>
public class BulkUpdateUsersDto
{
    public required List<Guid> UserIds { get; set; }
    public required UpdateUserDto Updates { get; set; }
}