using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.DTOs.Organization
{
    public class OrganizationFilterDto: Filter
    {
        public bool? IsActive { get; set; }
        public OrganizationSize OrganizationSize { get; set; }
    }
}
