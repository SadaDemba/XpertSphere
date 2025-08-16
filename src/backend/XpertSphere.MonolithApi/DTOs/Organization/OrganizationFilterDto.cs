using XpertSphere.MonolithApi.DTOs.Base;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.Organization
{
    public class OrganizationFilterDto: Filter
    {
        public bool? IsActive { get; set; }
        public OrganizationSize OrganizationSize { get; set; }
    }
}
