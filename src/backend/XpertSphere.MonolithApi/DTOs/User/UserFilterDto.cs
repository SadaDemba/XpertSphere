using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.User;

public class UserFilterDto
{
    public string? Search { get; set; } // Search in name, email
    public UserType? UserType { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? Department { get; set; }
    public bool? IsActive { get; set; }
    public int? MinExperience { get; set; }
    public int? MaxExperience { get; set; }
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public string? Skills { get; set; }
    
    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    // Sorting
    public string? SortBy { get; set; } = "CreatedAt";
    public string? SortDirection { get; set; } = "desc"; // asc, desc
}
