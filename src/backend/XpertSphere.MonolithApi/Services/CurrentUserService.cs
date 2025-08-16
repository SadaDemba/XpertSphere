using System.Security.Claims;
using XpertSphere.MonolithApi.Interfaces;

namespace XpertSphere.MonolithApi.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim?.Value, out var userId) ? userId : null;
        }
    }
}