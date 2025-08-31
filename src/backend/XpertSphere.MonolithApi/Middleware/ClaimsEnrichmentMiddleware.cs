using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils;

namespace XpertSphere.MonolithApi.Middleware;

public class ClaimsEnrichmentMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ClaimsEnrichmentMiddleware> _logger;

    private static readonly Dictionary<string, string[]> EntraIdGroupToRoleMapping = new()
    {
        // XpertSphere Platform Groups (Our company)
        ["Org-XpertSphere"] = [Roles.PlatformAdmin.Name, Roles.PlatformSuperAdmin.Name],
        ["XpertSphere-SuperAdmin"] = [Roles.PlatformSuperAdmin.Name],
        ["XpertSphere-Admin"] = [Roles.PlatformAdmin.Name],
        
        // Expertime Client Groups
        ["Org-Expertime"] = [Roles.Recruiter.Name, Roles.Manager.Name, Roles.OrganizationAdmin.Name],
        ["Expertime-Admin"] = [Roles.OrganizationAdmin.Name],
        ["Expertime-Manager"] = [Roles.Manager.Name],
        ["Expertime-Recruiter"] = [Roles.Recruiter.Name],
        ["Expertime-TechnicalEvaluator"] = [Roles.TechnicalEvaluator.Name]
    };

    public ClaimsEnrichmentMiddleware(RequestDelegate next, ILogger<ClaimsEnrichmentMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, XpertSphereDbContext dbContext)
    {
        try
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                await EnrichUserClaims(context, dbContext);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while enriching user claims");
        }

        await _next(context);
    }

    private async Task EnrichUserClaims(HttpContext context, XpertSphereDbContext dbContext)
    {
        var user = context.User;
        var authType = user.FindFirst("auth_type")?.Value;

        if (authType == "B2B")
        {
            await EnrichB2BClaims(context, dbContext);
        }
        else if (authType == "B2C")
        {
            await EnrichB2CClaims(context, dbContext);
        }
        else
        {
            await EnrichJwtClaims(context, dbContext);
        }
    }

    private async Task EnrichB2BClaims(HttpContext context, XpertSphereDbContext dbContext)
    {
        var user = context.User;
        var identity = user.Identity as ClaimsIdentity;
        if (identity == null) return;

        var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;
        var entraUserId = user.FindFirst("entra_user_id")?.Value;

        if (string.IsNullOrEmpty(userEmail))
        {
            _logger.LogWarning("B2B user has no email claim");
            return;
        }

        // Find or create user in database
        var dbUser = await FindOrCreateB2BUser(dbContext, userEmail, entraUserId, user);
        if (dbUser == null) return;

        // Add database user ID claim
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, dbUser.Id.ToString()));

        // Add organization claims
        if (dbUser.OrganizationId.HasValue)
        {
            var organization = await dbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == dbUser.OrganizationId.Value);

            if (organization != null)
            {
                identity.AddClaim(new Claim("OrganizationId", organization.Id.ToString()));
                identity.AddClaim(new Claim("OrganizationName", organization.Name));
                identity.AddClaim(new Claim("OrganizationCode", organization.Code));
            }
        }
        else
        {
            // XpertSphere platform users don't have OrganizationId
            identity.AddClaim(new Claim("IsPlatformUser", "true"));
        }

        // Map Entra ID groups to application roles
        await MapEntraIdGroupsToRoles(identity, dbUser, dbContext);

        // Add user permissions based on roles
        await AddUserPermissions(identity, dbUser, dbContext);

        _logger.LogInformation("Enriched B2B claims for user {Email} with {ClaimsCount} claims", 
            userEmail, identity.Claims.Count());
    }

    private async Task EnrichB2CClaims(HttpContext context, XpertSphereDbContext dbContext)
    {
        var user = context.User;
        var identity = user.Identity as ClaimsIdentity;
        if (identity == null) return;

        var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;
        var entraUserId = user.FindFirst("entra_user_id")?.Value;

        if (string.IsNullOrEmpty(userEmail))
        {
            _logger.LogWarning("B2C user has no email claim");
            return;
        }

        // Find or create candidate in database
        var dbUser = await FindOrCreateB2CUser(dbContext, userEmail, entraUserId, user);
        if (dbUser == null) return;

        // Add database user ID claim
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, dbUser.Id.ToString()));

        // B2C users are always candidates
        identity.AddClaim(new Claim(ClaimTypes.Role, Roles.Candidate.Name));

        _logger.LogInformation("Enriched B2C claims for candidate {Email}", userEmail);
    }

    private async Task EnrichJwtClaims(HttpContext context, XpertSphereDbContext dbContext)
    {
        var user = context.User;
        var identity = user.Identity as ClaimsIdentity;
        if (identity == null) return;

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return;

        // Get user from database with roles and organization
        var dbUser = await dbContext.Users
            .Include(u => u.Organization)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (dbUser == null) return;

        // Add organization claims
        if (dbUser.OrganizationId.HasValue && dbUser.Organization != null)
        {
            identity.AddClaim(new Claim("OrganizationId", dbUser.Organization.Id.ToString()));
            identity.AddClaim(new Claim("OrganizationName", dbUser.Organization.Name));
            identity.AddClaim(new Claim("OrganizationCode", dbUser.Organization.Code));
        }
        else
        {
            // Platform users don't have organization
            identity.AddClaim(new Claim("IsPlatformUser", "true"));
        }

        // Add role claims
        foreach (var userRole in dbUser.UserRoles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, userRole.Role.Name));
            
            // Add permissions for each role
            foreach (var rolePermission in userRole.Role.RolePermissions)
            {
                var permission = $"{rolePermission.Permission.Scope}:{rolePermission.Permission.Action}";
                identity.AddClaim(new Claim("permission", permission));
            }
        }

        _logger.LogInformation("Enriched JWT claims for user {UserId} with {RolesCount} roles", 
            userId, dbUser.UserRoles.Count);
    }

    private async Task<User?> FindOrCreateB2BUser(XpertSphereDbContext dbContext, string email, 
        string? entraUserId, ClaimsPrincipal userPrincipal)
    {
        // Try to find existing user by email or Entra ID
        var user = await dbContext.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == email || u.ExternalId == entraUserId);

        if (user != null)
        {
            // Update Entra ID if not set
            if (string.IsNullOrEmpty(user.ExternalId) && !string.IsNullOrEmpty(entraUserId))
            {
                user.ExternalId = entraUserId;
                await dbContext.SaveChangesAsync();
            }
            return user;
        }

        // Create new B2B user
        var userName = userPrincipal.FindFirst(ClaimTypes.Name)?.Value ?? email;
        var nameParts = userName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FirstName = nameParts.FirstOrDefault() ?? "Unknown",
            LastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "User",
            ExternalId = entraUserId,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Determine organization from groups
        var organization = await DetermineOrganizationFromGroups(dbContext, userPrincipal);
        if (organization != null)
        {
            newUser.OrganizationId = organization.Id;
        }
        // XpertSphere platform users don't have OrganizationId (null)

        dbContext.Users.Add(newUser);
        await dbContext.SaveChangesAsync();

        _logger.LogInformation("Created new B2B user {Email} in organization {Organization}", 
            email, organization?.Name ?? "Platform");

        return newUser;
    }

    private async Task<User?> FindOrCreateB2CUser(XpertSphereDbContext dbContext, string email, 
        string? entraUserId, ClaimsPrincipal userPrincipal)
    {
        // Try to find existing user
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email || u.ExternalId == entraUserId);

        if (user != null)
        {
            if (string.IsNullOrEmpty(user.ExternalId) && !string.IsNullOrEmpty(entraUserId))
            {
                user.ExternalId = entraUserId;
                await dbContext.SaveChangesAsync();
            }
            return user;
        }

        // Create new B2C candidate
        var userName = userPrincipal.FindFirst(ClaimTypes.Name)?.Value ?? email;
        var nameParts = userName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FirstName = nameParts.FirstOrDefault() ?? "Unknown",
            LastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "Candidate",
            ExternalId = entraUserId,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OrganizationId = null // Candidates don't belong to organizations
        };

        dbContext.Users.Add(newUser);

        // Assign candidate role
        var candidateRole = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == Roles.Candidate.Name);

        if (candidateRole != null)
        {
            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = newUser.Id,
                RoleId = candidateRole.Id,
                AssignedAt = DateTime.UtcNow
            };
            dbContext.UserRoles.Add(userRole);
        }

        await dbContext.SaveChangesAsync();

        _logger.LogInformation("Created new B2C candidate {Email}", email);
        return newUser;
    }

    private async Task<Organization?> DetermineOrganizationFromGroups(XpertSphereDbContext dbContext, 
        ClaimsPrincipal userPrincipal)
    {
        var groupClaims = userPrincipal.FindAll("group").Select(c => c.Value).ToList();

        // XpertSphere platform users (Org-XpertSphere) don't have an organization in DB
        if (groupClaims.Any(g => g.Contains("XpertSphere")))
        {
            return null; // Platform users have OrganizationId = null
        }

        // Expertime is a client organization
        if (groupClaims.Any(g => g.Contains("Expertime")))
        {
            return await dbContext.Organizations
                .FirstOrDefaultAsync(o => o.Code == "EXPERTIME");
        }

        return null;
    }

    private async Task MapEntraIdGroupsToRoles(ClaimsIdentity identity, User dbUser, 
        XpertSphereDbContext dbContext)
    {
        var groupClaims = identity.FindAll("group").Select(c => c.Value).ToList();
        var rolesToAssign = new List<string>();

        foreach (var group in groupClaims)
        {
            if (EntraIdGroupToRoleMapping.TryGetValue(group, out var roles))
            {
                rolesToAssign.AddRange(roles);
            }
        }

        // Remove duplicates and add role claims
        foreach (var role in rolesToAssign.Distinct())
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        // Sync roles to database
        await SyncUserRolesInDatabase(dbUser, rolesToAssign.Distinct().ToList(), dbContext);
    }

    private async Task SyncUserRolesInDatabase(User dbUser, List<string> roleNames, 
        XpertSphereDbContext dbContext)
    {
        var roles = await dbContext.Roles
            .Where(r => roleNames.Contains(r.Name))
            .ToListAsync();

        var existingUserRoles = await dbContext.UserRoles
            .Where(ur => ur.UserId == dbUser.Id)
            .ToListAsync();

        // Remove roles that are no longer assigned
        var rolesToRemove = existingUserRoles
            .Where(ur => !roles.Any(r => r.Id == ur.RoleId))
            .ToList();

        dbContext.UserRoles.RemoveRange(rolesToRemove);

        // Add new roles
        foreach (var role in roles)
        {
            if (!existingUserRoles.Any(ur => ur.RoleId == role.Id))
            {
                var userRole = new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = dbUser.Id,
                    RoleId = role.Id,
                    AssignedAt = DateTime.UtcNow
                };
                dbContext.UserRoles.Add(userRole);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task AddUserPermissions(ClaimsIdentity identity, User dbUser, 
        XpertSphereDbContext dbContext)
    {
        var userRoles = await dbContext.UserRoles
            .Where(ur => ur.UserId == dbUser.Id)
            .Include(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .ToListAsync();

        var permissions = userRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => $"{rp.Permission.Scope}:{rp.Permission.Action}")
            .Distinct();

        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim("permission", permission));
        }
    }
}