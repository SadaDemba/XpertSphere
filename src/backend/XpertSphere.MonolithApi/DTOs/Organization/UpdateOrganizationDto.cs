using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.DTOs.Organization;

public class UpdateOrganizationDto
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string? Industry { get; set; }
    public OrganizationSize? Size { get; set; }
    public Address? Address { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Website { get; set; }
    public bool? IsActive { get; set; }
}