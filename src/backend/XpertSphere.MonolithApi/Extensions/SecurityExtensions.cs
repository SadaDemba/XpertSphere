using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Data;

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
        
        var jwtIssuer = configuration.GetSection("Jwt:Issuer").Value ?? "XpertSphere";
        var jwtAudience = configuration.GetSection("Jwt:Audience").Value ?? "XpertSphere-Users";

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
        return configuration.GetSection("Jwt:Key").Value ??
               Environment.GetEnvironmentVariable("JWT_KEY") ??
               "your-super-secret-jwt-key-that-is-at-least-32-characters-long-for-development";
    }

    private static JwtBearerEvents ConfigureJwtEvents()
    {
        return new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception != null)
                {
                    context.Response.Headers.Append("Authentication-Failed", "true");
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                // Add custom validation logic here
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // Custom challenge response
                return Task.CompletedTask;
            }
        };
    }

    private static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Role-based policies
            options.AddPolicy("RequireRecruiterRole", policy =>
                policy.RequireClaim("UserType", "Recruiter"));

            options.AddPolicy("RequireManagerRole", policy =>
                policy.RequireClaim("UserType", "Manager"));

            options.AddPolicy("RequireAdminRole", policy =>
                policy.RequireClaim("UserType", "Admin"));

            options.AddPolicy("RequireInternalUser", policy =>
                policy.RequireClaim("UserType", "Recruiter", "Manager", "Admin", "TechnicalEvaluator"));

            // Organization-based policy
            options.AddPolicy("SameOrganization", policy =>
                policy.RequireAssertion(context =>
                {
                    var userOrgClaim = context.User.FindFirst("OrganizationId");
                    return userOrgClaim != null;
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
    public string Issuer { get; set; } = "XpertSphere";
    public string Audience { get; set; } = "XpertSphere-Users";
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public bool RequireHttpsMetadata { get; set; } = true;
    public bool SaveToken { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
}