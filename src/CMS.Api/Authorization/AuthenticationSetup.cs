using System.Text;
using CMS.Application.Identity;
using CMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CMS.Api.Authorization;

// Official bearer validation checks cryptography before consulting durable session revocation.
public static class AuthenticationSetup
{
    // Missing signing configuration prevents the HTTP host from becoming ready.
    public static IServiceCollection AddCmsAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>().Validate(options => Encoding.UTF8.GetByteCount(options.SigningKey) >= 32
            && !string.IsNullOrWhiteSpace(options.Issuer) && !string.IsNullOrWhiteSpace(options.Audience),
            "Jwt signing key, issuer and audience are required; use a random key of at least 32 bytes.").ValidateOnStart();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, configured) =>
            {
                var jwt = configured.Value;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(15),
                    RoleClaimType = "role"
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var principal = context.Principal!;
                        if (!Guid.TryParse(principal.FindFirst("tenant_id")?.Value, out var tenantId)
                            || !Guid.TryParse(principal.FindFirst("sub")?.Value, out var userId)
                            || !Guid.TryParse(principal.FindFirst("sid")?.Value, out var sessionId)
                            || !await context.HttpContext.RequestServices.GetRequiredService<IIdentityService>()
                                .IsSessionActiveAsync(tenantId, userId, sessionId, context.HttpContext.RequestAborted))
                        {
                            context.Fail("Session is not active.");
                            return;
                        }
                        var identity = (System.Security.Claims.ClaimsIdentity)principal.Identity!;
                        foreach (var claim in identity.FindAll("permissions").ToArray()) identity.RemoveClaim(claim);
                        var permissions = await context.HttpContext.RequestServices.GetRequiredService<IIdentityService>()
                            .GetPermissionsAsync(tenantId, userId, context.HttpContext.RequestAborted);
                        identity.AddClaims(permissions.Select(permission => new System.Security.Claims.Claim("permissions", permission)));
                    }
                };
            });
        services.AddAuthorization(options =>
        {
            foreach (var permission in CMS.Domain.Identity.Roles.Role.KnownPermissions)
                options.AddPolicy(permission, policy => policy.RequireAuthenticatedUser().RequireClaim("permissions", permission));
        });
        return services;
    }
}
