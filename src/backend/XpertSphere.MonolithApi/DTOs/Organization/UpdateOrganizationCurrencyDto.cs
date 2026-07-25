using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.Organization;

public class UpdateOrganizationCurrencyDto
{
    [Required] public Currency Currency { get; set; }
}
