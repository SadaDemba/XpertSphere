using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Models;

public class Organization : AuditableEntity
{
    [Required] [MaxLength(200)] public required string Name { get; set; }

    [Required] [MaxLength(50)] public required string Code { get; set; }

    public Address Address { get; set; } = new();

    [EmailAddress] [MaxLength(255)] public string? ContactEmail { get; set; }

    [MaxLength(20)] public string? ContactPhone { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(100)] public string? Industry { get; set; }

    public OrganizationSize? Size { get; set; }

    [MaxLength(255)] public string? Website { get; set; }

    // Devise appliquée aux offres d'emploi créées par cette organisation à partir du moment où
    // elle est configurée (via PUT /api/organizations/me/currency). Nullable : une organisation
    // n'ayant pas encore configuré sa devise n'en a "pas encore" une (voir
    // configurable-salary-currency.md, décision 3). Ne s'applique jamais rétroactivement : ce
    // champ n'est lu qu'au moment de la création d'une offre (snapshot figé sur
    // JobOffer.SalaryCurrency), jamais relu ensuite.
    public Currency? Currency { get; set; }

    // Navigation properties
    [JsonIgnore] public virtual ICollection<User> Users { get; set; } = [];
}