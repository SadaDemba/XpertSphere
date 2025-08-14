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
}
