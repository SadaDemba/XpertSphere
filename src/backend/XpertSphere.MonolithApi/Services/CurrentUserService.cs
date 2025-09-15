using System.Security.Claims;
using XpertSphere.MonolithApi.Interfaces;

namespace XpertSphere.MonolithApi.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim?.Value, out var userId) ? userId : null;
        }
    }

    public Guid? OrganizationId
    {
        get
        {
            var orgIdClaim = User?.FindFirst("OrganizationId");
            return Guid.TryParse(orgIdClaim?.Value, out var orgId) ? orgId : null;
        }
    }
}