using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using XpertSphere.MonolithApi.Config;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Extensions;

public static class SecurityExtensions
{
    public static IServiceCollection AddSecurity(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment, bool useEntraId = false)
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

        // Configure Authentication (JWT + Entra ID)
        services.AddMultiModeAuthentication(configuration, environment, useEntraId);

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

    private static IServiceCollection AddMultiModeAuthentication(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment, bool useEntraId = false)
    {
        // Force pure JWT authentication if Entra ID is disabled or in development
        if (environment.IsDevelopment() || !useEntraId)
        {
            services.AddJwtAuthentication(configuration, environment);
            return services;
        }

        var entraIdSettings = configuration.GetSection("EntraId").Get<EntraIdSettings>();
        var hasEntraId = entraIdSettings != null && !string.IsNullOrEmpty(entraIdSettings.TenantId);

        if (hasEntraId && useEntraId)
        {
            services.AddEntraIdAuthentication(configuration, environment);
        }
        else
        {
            services.AddJwtAuthentication(configuration, environment);
        }

        return services;
    }

    private static IServiceCollection AddEntraIdAuthentication(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        var entraIdSettings = configuration.GetSection("EntraId").Get<EntraIdSettings>();
        if (entraIdSettings == null)
        {
            throw new InvalidOperationException("EntraId configuration is required but not found");
        }

        services.Configure<EntraIdSettings>(configuration.GetSection("EntraId"));

        var authBuilder = services.AddAuthentication(options =>
            {
                options.DefaultScheme = "MultiScheme";
                options.DefaultChallengeScheme = "MultiScheme";
            })
            .AddPolicyScheme("MultiScheme", "Multi Auth Scheme", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                    if (string.IsNullOrEmpty(authHeader))
                        return null;

                    var token = authHeader.Replace("Bearer ", "");

                    if (IsB2BToken(token))
                        return "B2B";
                    if (IsB2CToken(token))
                        return "B2C";

                    return JwtBearerDefaults.AuthenticationScheme;
                };
            });

        // Configure B2B Authentication (Internal Users)
        if (HasB2BConfiguration(entraIdSettings))
        {
            authBuilder.AddMicrosoftIdentityWebApi(
                configuration.GetSection("EntraId:B2B"),
                "B2B");

            services.Configure<JwtBearerOptions>("B2B",
                options => { options.Events = ConfigureB2BEvents(entraIdSettings); });
        }

        // Configure B2C Authentication (External Candidates)
        if (HasB2CConfiguration(entraIdSettings))
        {
            authBuilder.AddMicrosoftIdentityWebApi(
                configuration.GetSection("EntraId:B2C"),
                "B2C");

            services.Configure<JwtBearerOptions>("B2C",
                options => { options.Events = ConfigureB2CEvents(entraIdSettings); });
        }

        // Configure JWT Authentication (Development fallback)
        authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme,
            options => { ConfigureJwtBearerOptions(options, configuration, environment); });

        return services;
    }

    private static bool HasB2BConfiguration(EntraIdSettings settings)
    {
        return settings.B2B != null &&
               !string.IsNullOrEmpty(settings.B2B.Authority) &&
               !string.IsNullOrEmpty(settings.B2B.ClientId);
    }

    private static bool HasB2CConfiguration(EntraIdSettings settings)
    {
        return settings.B2C != null &&
               !string.IsNullOrEmpty(settings.B2C.Authority) &&
               !string.IsNullOrEmpty(settings.B2C.ClientId);
    }

    private static bool IsB2BToken(string token)
    {
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
                return false;

            var jsonToken = handler.ReadJwtToken(token);
            var issuer = jsonToken?.Issuer ?? string.Empty;

            return issuer.Contains("login.microsoftonline.com");
        }
        catch
        {
            return false;
        }
    }

    private static bool IsB2CToken(string token)
    {
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
                return false;

            var jsonToken = handler.ReadJwtToken(token);
            var issuer = jsonToken?.Issuer ?? string.Empty;

            return issuer.Contains(".b2clogin.com");
        }
        catch
        {
            return false;
        }
    }

    private static JwtBearerEvents ConfigureB2BEvents(EntraIdSettings entraIdSettings)
    {
        return new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                if (context.Principal?.Identity?.IsAuthenticated == true)
                {
                    await EnrichB2BClaims(context, entraIdSettings);
                }
            },
            OnAuthenticationFailed = HandleAuthenticationFailed,
            OnChallenge = HandleChallenge
        };
    }

    private static JwtBearerEvents ConfigureB2CEvents(EntraIdSettings entraIdSettings)
    {
        return new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                if (context.Principal?.Identity?.IsAuthenticated == true)
                {
                    await EnrichB2CClaims(context, entraIdSettings);
                }
            },
            OnAuthenticationFailed = HandleAuthenticationFailed,
            OnChallenge = HandleChallenge
        };
    }

    private static async Task EnrichB2BClaims(TokenValidatedContext context, EntraIdSettings entraIdSettings)
    {
        var identity = context.Principal?.Identity as ClaimsIdentity;
        if (identity == null) return;

        // Add authentication type claim
        identity.AddClaim(new Claim("auth_type", "B2B"));

        // Extract user information from Entra ID token
        var userObjectId = context.Principal?.FindFirst("oid")?.Value;
        var userEmail = context.Principal?.FindFirst("preferred_username")?.Value ??
                        context.Principal?.FindFirst("email")?.Value;
        var userName = context.Principal?.FindFirst("name")?.Value;

        if (!string.IsNullOrEmpty(userObjectId))
        {
            identity.AddClaim(new Claim("entra_user_id", userObjectId));
        }

        if (!string.IsNullOrEmpty(userEmail))
        {
            identity.AddClaim(new Claim(ClaimTypes.Email, userEmail));
        }

        if (!string.IsNullOrEmpty(userName))
        {
            identity.AddClaim(new Claim(ClaimTypes.Name, userName));
        }

        // Add groups claims if available
        var groupsClaim = context.Principal?.FindFirst("groups");
        if (groupsClaim != null && entraIdSettings.B2B.EnableGroupClaims)
        {
            var groups = System.Text.Json.JsonSerializer.Deserialize<string[]>(groupsClaim.Value);
            foreach (var group in groups ?? Array.Empty<string>())
            {
                identity.AddClaim(new Claim("group", group));
            }
        }

        await Task.CompletedTask;
    }

    private static async Task EnrichB2CClaims(TokenValidatedContext context, EntraIdSettings entraIdSettings)
    {
        var identity = context.Principal?.Identity as ClaimsIdentity;
        if (identity == null) return;

        // Add authentication type claim
        identity.AddClaim(new Claim("auth_type", "B2C"));

        // Extract user information from B2C token
        var userObjectId = context.Principal?.FindFirst("oid")?.Value ??
                           context.Principal?.FindFirst("sub")?.Value;
        var userEmail = context.Principal?.FindFirst("emails")?.Value ??
                        context.Principal?.FindFirst("email")?.Value;
        var firstName = context.Principal?.FindFirst("given_name")?.Value;
        var lastName = context.Principal?.FindFirst("family_name")?.Value;

        if (!string.IsNullOrEmpty(userObjectId))
        {
            identity.AddClaim(new Claim("entra_user_id", userObjectId));
        }

        if (!string.IsNullOrEmpty(userEmail))
        {
            identity.AddClaim(new Claim(ClaimTypes.Email, userEmail));
        }

        var fullName = $"{firstName} {lastName}".Trim();
        if (!string.IsNullOrEmpty(fullName))
        {
            identity.AddClaim(new Claim(ClaimTypes.Name, fullName));
        }

        // Add candidate role for B2C users
        identity.AddClaim(new Claim(ClaimTypes.Role, Roles.Candidate.Name));

        await Task.CompletedTask;
    }

    private static void ConfigureJwtBearerOptions(JwtBearerOptions options,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        var jwtKey = GetJwtKey(configuration);
        var jwtIssuer = configuration.GetSection("Jwt:Issuer").Value;
        var jwtAudience = configuration.GetSection("Jwt:Audience").Value;


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
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options => { ConfigureJwtBearerOptions(options, configuration, environment); });

        return services;
    }

    private static string GetJwtKey(IConfiguration configuration)
    {
        // Try to get from configuration first (includes Key Vault for staging/prod)
        var jwtKey = configuration["Jwt:Key"];
        
        // Fallback to environment variable if not found
        if (string.IsNullOrEmpty(jwtKey))
        {
            jwtKey = Environment.GetEnvironmentVariable("JWT__KEY");
        }

        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("JWT Key is not configured. Please configure Jwt:Key");
        }

        return jwtKey;
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

            // Entra ID Authentication Type Policies
            options.AddPolicy("RequireB2BAuthentication", policy =>
                policy.RequireAssertion(context =>
                {
                    var authTypeClaim = context.User.FindFirst("auth_type");
                    return authTypeClaim?.Value == "B2B";
                }));

            options.AddPolicy("RequireB2CAuthentication", policy =>
                policy.RequireAssertion(context =>
                {
                    var authTypeClaim = context.User.FindFirst("auth_type");
                    return authTypeClaim?.Value == "B2C";
                }));

            options.AddPolicy("RequireEntraIdAuthentication", policy =>
                policy.RequireAssertion(context =>
                {
                    var authTypeClaim = context.User.FindFirst("auth_type");
                    return authTypeClaim?.Value is "B2B" or "B2C";
                }));

            // B2B Group-based policies
            options.AddPolicy("RequireXpertSphereGroup", policy =>
                policy.RequireAssertion(context =>
                {
                    return context.User.HasClaim("group", "Org-XpertSphere") ||
                           context.User.HasClaim("group", "XpertSphere");
                }));

            options.AddPolicy("RequireExpertimeGroup", policy =>
                policy.RequireAssertion(context =>
                {
                    return context.User.HasClaim("group", "Org-Expertime") ||
                           context.User.HasClaim("group", "Expertime");
                }));


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
                        if (httpContext?.Request.RouteValues.TryGetValue("organizationId", out var orgIdValue) ==
                            true ||
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

            // Critical authorization rules for organization isolation
            options.AddPolicy("CanCreateUsers", policy =>
                policy.RequireAssertion(context =>
                {
                    // Platform SuperAdmin and Admin can create users
                    if (context.User.IsInRole(Roles.PlatformSuperAdmin.Name) ||
                        context.User.IsInRole(Roles.PlatformAdmin.Name))
                    {
                        return true;
                    }

                    // SuperAdmin from XpertSphere can create users

                    var organizationClaim = context.User.FindFirst("OrganizationName");
                    if (organizationClaim?.Value == "XpertSphere" &&
                        context.User.IsInRole(Roles.PlatformSuperAdmin.Name))
                    {
                        return true;
                    }

                    // Users with XpertSphere group can create users
                    if (context.User.HasClaim("group", "Org-XpertSphere") ||
                        context.User.HasClaim("group", "XpertSphere"))
                    {
                        return true;
                    }


                    // Admin in client organization can create users only for their own organization
                    if (organizationClaim?.Value != "XpertSphere" &&
                        context.User.IsInRole(Roles.OrganizationAdmin.Name))
                    {
                        // Verify that the admin is creating for their own organization
                        var userOrgIdClaim = context.User.FindFirst("OrganizationId");
                        if (userOrgIdClaim != null && Guid.TryParse(userOrgIdClaim.Value, out var userOrgId))
                        {
                            var httpContext = context.Resource as DefaultHttpContext;
                            if (httpContext?.Request.RouteValues.TryGetValue("organizationId", out var orgIdValue) ==
                                true)
                            {
                                if (Guid.TryParse(orgIdValue?.ToString(), out var requestedOrgId))
                                {
                                    return userOrgId == requestedOrgId;
                                }
                            }
                        }
                    }

                    return false;
                }));

            options.AddPolicy("CanCreateAdminsForOtherOrganizations", policy =>
                policy.RequireAssertion(context =>
                {
                    // Platform SuperAdmin and Admin can create Admins for other organizations
                    if (context.User.IsInRole(Roles.PlatformSuperAdmin.Name) || 
                        context.User.IsInRole(Roles.PlatformAdmin.Name))
                    {
                        return true;
                    }
                    
                    // XpertSphere users can create Admins for other organizations
                    var organizationClaim = context.User.FindFirst("OrganizationName");
                    if (organizationClaim?.Value == "XpertSphere" &&
                        (context.User.IsInRole(Roles.PlatformSuperAdmin.Name) ||
                         context.User.IsInRole(Roles.PlatformAdmin.Name)))
                    {
                        return true;
                    }
                    
                    // Users with XpertSphere group can create Admins for other organizations
                    if (context.User.HasClaim("group", "Org-XpertSphere") ||
                        context.User.HasClaim("group", "XpertSphere"))
                    {
                        return true;
                    }
                    
                    return false;
                }));

            options.AddPolicy("OrganizationIsolation", policy =>
                policy.RequireAssertion(context =>
                {
                    // Platform SuperAdmin and Admin can access all organizations
                    if (context.User.IsInRole(Roles.PlatformSuperAdmin.Name) ||
                        context.User.IsInRole(Roles.PlatformAdmin.Name))
                    {
                        return true;
                    }

                    // XpertSphere users can access all organizations
                    var organizationClaim = context.User.FindFirst("OrganizationName");
                    if (organizationClaim?.Value == "XpertSphere")
                    {
                        return true;
                    }

                    // Users with XpertSphere group can access all organizations
                    if (context.User.HasClaim("group", "Org-XpertSphere") ||
                        context.User.HasClaim("group", "XpertSphere"))
                    {
                        return true;
                    }

                    // Others can only access their own organization data
                    var userOrgIdClaim = context.User.FindFirst("OrganizationId");
                    if (userOrgIdClaim != null && Guid.TryParse(userOrgIdClaim.Value, out var userOrgId))
                    {
                        var httpContext = context.Resource as DefaultHttpContext;
                        if (httpContext?.Request.RouteValues.TryGetValue("organizationId", out var orgIdValue) == true)
                        {
                            if (Guid.TryParse(orgIdValue?.ToString(), out var requestedOrgId))
                            {
                                return userOrgId == requestedOrgId;
                            }
                        }
                    }

                    return false;
                }));

            options.AddPolicy("CanManageJobOffers", policy =>
                policy.RequireAssertion(context =>
                {
                    // Only recruiters can manage job offers
                    return context.User.IsInRole(Roles.Recruiter.Name);
                }));

            options.AddPolicy("CanAssignEvaluators", policy =>
                policy.RequireAssertion(context =>
                {
                    // Only recruiters can assign evaluators
                    return context.User.IsInRole(Roles.Recruiter.Name);
                }));

            options.AddPolicy("CanResetPasswords", policy =>
                policy.RequireAssertion(context =>
                {
                    // Platform SuperAdmin and Admin can reset any password
                    if (context.User.IsInRole(Roles.PlatformSuperAdmin.Name) || 
                        context.User.IsInRole(Roles.PlatformAdmin.Name))
                    {
                        return true;
                    }
                    
                    // XpertSphere users can reset any password
                    var organizationClaim = context.User.FindFirst("OrganizationName");
                    if (organizationClaim?.Value == "XpertSphere")
                    {
                        return true;
                    }
                    
                    // Users with XpertSphere group can reset any password
                    if (context.User.HasClaim("group", "Org-XpertSphere") ||
                        context.User.HasClaim("group", "XpertSphere"))
                    {
                        return true;
                    }
                    
                    // Organization admins can reset passwords for users in their organization
                    if (context.User.IsInRole(Roles.OrganizationAdmin.Name))
                    {
                        return true; // Organization-specific validation will be done in the service
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
        services.Configure<JwtSettings>(options =>
        {
            var jwtSection = configuration.GetSection("Jwt");
            jwtSection.Bind(options);

            // Set the Key from environment or configuration
            options.Key = GetJwtKey(configuration);
        });

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
            options.Secure = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
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