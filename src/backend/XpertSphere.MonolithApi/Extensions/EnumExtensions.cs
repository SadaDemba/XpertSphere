using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.Extensions;

public static class EnumExtensions
{
    public static string ToStringValue(this OrganizationSize size)
    {
        return size switch
        {
            OrganizationSize.Startup => "STARTUP",
            OrganizationSize.Small => "SMALL",
            OrganizationSize.Medium => "MEDIUM",
            OrganizationSize.Large => "LARGE",
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    public static OrganizationSize ToOrganizationSize(this string value)
    {
        return value?.ToUpper() switch
        {
            "STARTUP" => OrganizationSize.Startup,
            "SMALL" => OrganizationSize.Small,
            "MEDIUM" => OrganizationSize.Medium,
            "LARGE" => OrganizationSize.Large,
            _ => throw new ArgumentException($"Invalid OrganizationSize value: {value}", nameof(value))
        };
    }

    public static string ToStringValue(this PermissionAction action)
    {
        return action switch
        {
            PermissionAction.Create => "Create",
            PermissionAction.Read => "Read",
            PermissionAction.Update => "Update",
            PermissionAction.Delete => "Delete",
            PermissionAction.Manage => "Manage",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }

    public static PermissionAction ToPermissionAction(this string value)
    {
        return value switch
        {
            "Create" => PermissionAction.Create,
            "Read" => PermissionAction.Read,
            "Update" => PermissionAction.Update,
            "Delete" => PermissionAction.Delete,
            "Manage" => PermissionAction.Manage,
            _ => throw new ArgumentException($"Invalid PermissionAction value: {value}", nameof(value))
        };
    }

    public static string ToStringValue(this PermissionScope scope)
    {
        return scope switch
        {
            PermissionScope.All => "All",
            PermissionScope.Own => "Own",
            PermissionScope.Organization => "Organization",
            // I will use it in the next version of the platform
            PermissionScope.Department => "Department",
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null)
        };
    }

    public static PermissionScope ToPermissionScope(this string value)
    {
        return value switch
        {
            "All" => PermissionScope.All,
            "Own" => PermissionScope.Own,
            "Organization" => PermissionScope.Organization,
            // I will use it in the next version of the platform (just for biggest companies)
            "Department" => PermissionScope.Department,
            _ => throw new ArgumentException($"Invalid PermissionScope value: {value}", nameof(value))
        };
    }

    public static string ToStringValue(this WorkMode workMode)
    {
        return workMode switch
        {
            WorkMode.OnSite => "On-Site",
            WorkMode.Hybrid => "Hybrid",
            WorkMode.FullRemote => "Full Remote",
            _ => throw new ArgumentOutOfRangeException(nameof(workMode), workMode, null)
        };
    }

    public static WorkMode ToWorkMode(this string value)
    {
        return value switch
        {
            "On-Site" => WorkMode.OnSite,
            "Hybrid" => WorkMode.Hybrid,
            "Full Remote" => WorkMode.FullRemote,
            _ => throw new ArgumentException($"Invalid WorkMode value: {value}", nameof(value))
        };
    }

    public static string ToStringValue(this ContractType contractType)
    {
        return contractType switch
        {
            ContractType.FullTime => "Full-Time",
            ContractType.PartTime => "Part-Time",
            ContractType.Contract => "Contract",
            ContractType.Freelance => "Freelance",
            ContractType.Internship => "Internship",
            ContractType.Temporary => "Temporary",
            _ => throw new ArgumentOutOfRangeException(nameof(contractType), contractType, null)
        };
    }

    public static ContractType ToContractType(this string value)
    {
        return value switch
        {
            "Full-Time" => ContractType.FullTime,
            "Part-Time" => ContractType.PartTime,
            "Contract" => ContractType.Contract,
            "Freelance" => ContractType.Freelance,
            "Internship" => ContractType.Internship,
            "Temporary" => ContractType.Temporary,
            _ => throw new ArgumentException($"Invalid ContractType value: {value}", nameof(value))
        };
    }

    public static string ToStringValue(this JobOfferStatus status)
    {
        return status switch
        {
            JobOfferStatus.Draft => "Draft",
            JobOfferStatus.Published => "Published",
            JobOfferStatus.Closed => "Closed",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    public static JobOfferStatus ToJobOfferStatus(this string value)
    {
        return value switch
        {
            "Draft" => JobOfferStatus.Draft,
            "Published" => JobOfferStatus.Published,
            "Closed" => JobOfferStatus.Closed,
            _ => throw new ArgumentException($"Invalid JobOfferStatus value: {value}", nameof(value))
        };
    }

    public static string ToStringValue(this ApplicationStatus status)
    {
        return status switch
        {
            ApplicationStatus.Applied => "Application Submitted",
            ApplicationStatus.Reviewed => "Under Review",
            ApplicationStatus.PhoneScreening => "Phone Screening",
            ApplicationStatus.TechnicalTest => "Technical Assessment",
            ApplicationStatus.TechnicalInterview => "Technical Interview",
            ApplicationStatus.FinalInterview => "Final Interview",
            ApplicationStatus.OfferMade => "Offer Extended",
            ApplicationStatus.Accepted => "Offer Accepted",
            ApplicationStatus.Rejected => "Application Rejected",
            ApplicationStatus.Withdrawn => "Application Withdrawn",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    public static ApplicationStatus ToApplicationStatus(this string value)
    {
        return value switch
        {
            "Application Submitted" => ApplicationStatus.Applied,
            "Under Review" => ApplicationStatus.Reviewed,
            "Phone Screening" => ApplicationStatus.PhoneScreening,
            "Technical Assessment" => ApplicationStatus.TechnicalTest,
            "Technical Interview" => ApplicationStatus.TechnicalInterview,
            "Final Interview" => ApplicationStatus.FinalInterview,
            "Offer Extended" => ApplicationStatus.OfferMade,
            "Offer Accepted" => ApplicationStatus.Accepted,
            "Application Rejected" => ApplicationStatus.Rejected,
            "Application Withdrawn" => ApplicationStatus.Withdrawn,
            _ => throw new ArgumentException($"Invalid ApplicationStatus value: {value}", nameof(value))
        };
    }
}
