namespace XpertSphere.MonolithApi.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
}