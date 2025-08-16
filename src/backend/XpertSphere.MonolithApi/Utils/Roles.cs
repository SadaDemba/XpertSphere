namespace XpertSphere.MonolithApi.Utils;

/// <summary>
/// Constants for user roles as defined in Azure Entra ID
/// </summary>
public static class Roles
{
    /// <summary>
    /// Platform super administrator with access to everything across all organizations
    /// </summary>
    public static readonly RoleDefinition PlatformSuperAdmin =
        new(
            "XpertSphere.SuperAdmin",
            "The super administrator of XpertSphere Platform",
            "XpertSphere super administrator"
        );

    /// <summary>
    /// Platform administrator with access to everything across all organizations except the platform users management
    /// </summary>
    public static readonly RoleDefinition PlatformAdmin =
        new(
            "XpertSphere.Admin",
            "The administrator of XpertSphere Platform",
            "XpertSphere admin"
        );

    /// <summary>
    /// Organization administrator with full access within their organization
    /// </summary>
    public static readonly RoleDefinition OrganizationAdmin =
        new(
            "Organization.Admin",
            "A given client organization administrator",
            "Client organization administrator"
        );

    /// <summary>
    /// Manager who can approve candidates and manage recruitment processes
    /// </summary>
    public static readonly RoleDefinition Manager =
        new(
            "Organization.Manager",
            "A given client organization  manager",
            "Client organization manager"
        );

    /// <summary>
    /// Recruiter who can create job postings and manage candidates
    /// </summary>
    public static readonly RoleDefinition Recruiter = new("Organization.Recruiter",
        "A given client organization recruiter", "Client organization recruiter");

    /// <summary>
    /// Technical evaluator who can assess candidates technically (also a collaborator)
    /// </summary>
    public static readonly RoleDefinition TechnicalEvaluator = new("Organization.TechnicalEvaluator",
        "A given client organization technical evaluator", "Client organization technical evaluator");

    /// <summary>
    /// Candidate who can postulate on an offer
    /// </summary>
    public static readonly RoleDefinition Candidate = new("Candidate", "A candidate", "Candidate");

    public static readonly string[] PlatformRoles =
    [
        PlatformAdmin.Name,
        PlatformSuperAdmin.Name
    ];

    /// <summary>
    /// All organization roles (excluding platform ones)
    /// </summary>
    public static readonly string[] OrganizationRoles =
    [
        OrganizationAdmin.Name,
        Manager.Name,
        Recruiter.Name,
        TechnicalEvaluator.Name
    ];

    /// <summary>
    /// All internal user roles (including SuperAdmin)
    /// </summary>
    public static readonly string[] InternalRoles =
    [
        PlatformSuperAdmin.Name,
        PlatformAdmin.Name,
        OrganizationAdmin.Name,
        Manager.Name,
        Recruiter.Name,
        TechnicalEvaluator.Name
    ];

    /// <summary>
    /// Roles that can manage other users
    /// </summary>
    public static readonly string[] ManagementRoles =
    [
        PlatformSuperAdmin.Name,
        OrganizationAdmin.Name,
        Manager.Name
    ];

    /// <summary>
    /// Roles that can recruit (create job postings, manage candidates)
    /// </summary>
    public static readonly string[] RecruitmentRoles =
    [
        OrganizationAdmin.Name,
        Manager.Name,
        Recruiter.Name
    ];

    /// <summary>
    /// Roles that can evaluate candidates
    /// </summary>
    public static readonly string[] EvaluationRoles =
    [
        OrganizationAdmin.Name,
        Manager.Name,
        Recruiter.Name,
        TechnicalEvaluator.Name
    ];
}

public class RoleDefinition(string name, string description, string displayName)
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public string DisplayName { get; } = displayName;
}