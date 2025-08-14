using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Extensions;

public static class SecurityExtensions
{
    public static IServiceCollection AddSecurity(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Configure Identity
        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            ConfigurePasswordOptions(options, environment);
            ConfigureLockoutOptions(options);
            ConfigureUserOptions(options);
            ConfigureSignInOptions(options, environment);
            ConfigureTokenOptions(options);
        })
        .AddEntityFrameworkStores<XpertSphereDbContext>()
        .AddDefaultTokenProviders();

        // Configure JWT Authentication
        services.AddJwtAuthentication(configuration, environment);

        // Configure Authorization Policies
        services.AddAuthorizationPolicies();

        // Configure Security Settings
        services.AddSecurityConfiguration(configuration, environment);

        return services;
    }

    private static void ConfigurePasswordOptions(IdentityOptions options, IWebHostEnvironment environment)
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = environment.IsDevelopment() ? 6 : 8;
        options.Password.RequiredUniqueChars = 1;
    }

    private static void ConfigureLockoutOptions(IdentityOptions options)
    {
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    }

    private static void ConfigureUserOptions(IdentityOptions options)
    {
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = true;
    }

    private static void ConfigureSignInOptions(IdentityOptions options, IWebHostEnvironment environment)
    {
        options.SignIn.RequireConfirmedEmail = !environment.IsDevelopment();
        options.SignIn.RequireConfirmedAccount = false;
    }

    private static void ConfigureTokenOptions(IdentityOptions options)
    {
        options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
        options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        var jwtKey = GetJwtKey(configuration);
        
        var jwtIssuer = configuration.GetSection("Jwt:Issuer").Value;
        var jwtAudience = configuration.GetSection("Jwt:Audience").Value;

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = !environment.IsDevelopment();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromMinutes(5),
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            options.Events = ConfigureJwtEvents();
        });

        return services;
    }

    private static string GetJwtKey(IConfiguration configuration)
    {
        
        var jwtKey = configuration["jwt-key"] ?? // Key Vault secret
                     Environment.GetEnvironmentVariable("JWT_KEY"); // Env var dev
        
        return jwtKey ?? throw new InvalidOperationException("No JSON Web Token Key found");
    }

    private static JwtBearerEvents ConfigureJwtEvents()
    {
        return new JwtBearerEvents
        {
            OnAuthenticationFailed = HandleAuthenticationFailed,
            OnTokenValidated = _ => Task.CompletedTask,
            OnChallenge = HandleChallenge
        };
    }

    private static async Task HandleAuthenticationFailed(AuthenticationFailedContext context)
    {
        if (context.Exception == null)
            return;

        context.Response.Headers.Append("Authentication-Failed", "true");

        var result = context.Exception switch
        {
            SecurityTokenExpiredException => CreateTokenExpiredResult(context.Response),
            _ => ServiceResult.Unauthorized("Authentication failed")
        };

        await WriteServiceResultToResponse(context.Response, result);
    }

    private static async Task HandleChallenge(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        
        if (!context.Response.HasStarted)
        {
            var result = ServiceResult.Unauthorized("You are not authorized to access this resource");
            await WriteServiceResultToResponse(context.Response, result);
        }
    }

    private static ServiceResult CreateTokenExpiredResult(HttpResponse response)
    {
        response.Headers.Append("Token-Expired", "true");
        return ServiceResult.Unauthorized("Token has expired");
    }

    private static async Task WriteServiceResultToResponse(HttpResponse response, ServiceResult result)
    {
        response.StatusCode = result.StatusCode ?? 401;
        response.ContentType = "application/json";
        
        var errorResponse = new
        {
            success = result.IsSuccess,
            message = result.Message,
            errors = result.Errors,
            timestamp = DateTime.UtcNow
        };
        
        await response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
    }

    private static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Role-based policies
            
            options.AddPolicy("RequireRecruiterRole", policy =>
                policy.RequireRole(Roles.Recruiter.Name));

            options.AddPolicy("RequireManagerRole", policy =>
                policy.RequireRole(Roles.Manager.Name));

            options.AddPolicy("RequireOrganizationAdminRole", policy =>
                policy.RequireRole(Roles.OrganizationAdmin.Name));

            options.AddPolicy("RequireInternalUser", policy =>
                policy.RequireRole(Roles.InternalRoles));
            
            options.AddPolicy("RequireOrganisationRole", policy =>
                policy.RequireRole(Roles.OrganizationRoles));
            
            options.AddPolicy("RequirePlatformRole", policy =>
                policy.RequireRole(Roles.PlatformRoles));
            
            options.AddPolicy("RequirePlatformSuperAdminRole", policy =>
                policy.RequireRole(Roles.PlatformSuperAdmin.Name));
            
            options.AddPolicy("RequireCandidateRole", policy =>
                policy.RequireRole(Roles.Candidate.Name));

            
            // Organization-based policies
            options.AddPolicy("SameOrganization", policy =>
                policy.RequireAssertion(context =>
                {
                    var userOrgClaim = context.User.FindFirst("OrganizationId");
                    return userOrgClaim != null;
                }));
            
            options.AddPolicy("OrganizationAccess", policy =>
                policy.RequireAssertion(context =>
                {
                    if (context.User.IsInRole(Roles.PlatformAdmin.Name) || 
                        context.User.IsInRole(Roles.PlatformSuperAdmin.Name))
                    {
                        return true;
                    }
                    
                    var userOrgClaim = context.User.FindFirst("OrganizationId");
                    if (userOrgClaim != null && Guid.TryParse(userOrgClaim.Value, out var userOrgId))
                    {
                        var httpContext = context.Resource as DefaultHttpContext;
                        if (httpContext?.Request.RouteValues.TryGetValue("organizationId", out var orgIdValue) == true ||
                            httpContext?.Request.RouteValues.TryGetValue("id", out orgIdValue) == true)
                        {
                            if (Guid.TryParse(orgIdValue?.ToString(), out var requestedOrgId))
                            {
                                return userOrgId == requestedOrgId;
                            }
                        }
                    }

                    return false;
                }));
            
            options.AddPolicy("CandidateOwnDataAccess", policy =>
                policy.RequireAssertion(context =>
                {
                    if (context.User.IsInRole(Roles.PlatformAdmin.Name) || 
                        context.User.IsInRole(Roles.PlatformSuperAdmin.Name))
                    {
                        return true;
                    }
                    
                    if (Roles.OrganizationRoles.Any(role => context.User.IsInRole(role)))
                    {
                        return true;
                    }
                    
                    if (context.User.IsInRole(Roles.Candidate.Name))
                    {
                        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                        {
                            var httpContext = context.Resource as DefaultHttpContext;
                            if (httpContext?.Request.RouteValues.TryGetValue("userId", out var userIdValue) == true ||
                                httpContext?.Request.RouteValues.TryGetValue("id", out userIdValue) == true)
                            {
                                if (Guid.TryParse(userIdValue?.ToString(), out var requestedUserId))
                                {
                                    return userId == requestedUserId;
                                }
                            }
                        }
                    }

                    return false;
                }));
        });

        return services;
    }

    private static IServiceCollection AddSecurityConfiguration(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        // JWT Settings
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        // Token provider options
        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromDays(1);
        });

        // Cookie policy for GDPR compliance
        services.Configure<CookiePolicyOptions>(options =>
        {
            options.CheckConsentNeeded = context => !environment.IsDevelopment();
            options.MinimumSameSitePolicy = SameSiteMode.Strict;
            options.Secure = environment.IsDevelopment() ?
                CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });

        // HSTS for production
        if (!environment.IsDevelopment())
        {
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });
        }

        return services;
    }
}

// JWT Settings class
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; }
    public int RefreshTokenExpirationDays { get; set; }
    public bool RequireHttpsMetadata { get; set; } = true;
    public bool SaveToken { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
}