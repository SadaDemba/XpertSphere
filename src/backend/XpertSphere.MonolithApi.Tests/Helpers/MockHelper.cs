using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Tests.Helpers;

public static class MockHelper
{
    public static Mock<UserManager<User>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        var userManager = new Mock<UserManager<User>>(
            store.Object, null, null, null, null, null, null, null, null);
        
        return userManager;
    }

    public static Mock<SignInManager<User>> CreateMockSignInManager(Mock<UserManager<User>> userManager)
    {
        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
        
        var signInManager = new Mock<SignInManager<User>>(
            userManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null, null, null, null);
            
        return signInManager;
    }

    public static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    public static IOptions<T> CreateMockOptions<T>(T value) where T : class
    {
        return Options.Create(value);
    }
}