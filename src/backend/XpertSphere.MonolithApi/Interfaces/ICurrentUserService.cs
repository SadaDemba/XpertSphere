using System.Security.Claims;

namespace XpertSphere.MonolithApi.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? OrganizationId { get; }
    ClaimsPrincipal? User { get; }
}