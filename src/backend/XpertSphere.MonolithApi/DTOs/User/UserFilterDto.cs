using XpertSphere.MonolithApi.DTOs.Base;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.DTOs.User;

public class UserFilterDto: Filter
{
    public Guid? OrganizationId { get; set; }
    public string? Department { get; set; }
    public bool? IsActive { get; set; }
    public int? MinExperience { get; set; }
    public int? MaxExperience { get; set; }
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public string? Skills { get; set; }
    public string? Role { get; set; }
}
