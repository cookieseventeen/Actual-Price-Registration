using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Shijiatong.Api.Infrastructure.Auth;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddShijiatongAuth(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>($"{AuthOptions.SectionName}:Enabled"))
            return services;

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<MockSsoService>();

        var jwt = configuration.GetSection($"{AuthOptions.SectionName}:Jwt").Get<JwtOptions>() ?? new JwtOptions();
        var protectAdmin = AdminAuthorizationExtensions.IsAdminProtectionActive(configuration);
        var mockSso = configuration.GetValue<bool>($"{AuthOptions.SectionName}:MockSsoEnabled");

        if (protectAdmin || mockSso)
        {
            if (protectAdmin && string.IsNullOrWhiteSpace(jwt.SigningKey))
            {
                throw new InvalidOperationException(
                    "Auth:Jwt:SigningKey must be set when Auth:Enabled and Auth:ProtectAdminEndpoints are both true.");
            }

            if (!string.IsNullOrWhiteSpace(jwt.SigningKey))
            {
                AddValidatedJwtBearer(services, jwt);
                services.AddAuthorization(options =>
                {
                    if (protectAdmin)
                        options.AddPolicy(PolicyNames.Admin, policy => policy.RequireRole("admin"));
                });
            }
            else
            {
                AddPlaceholderJwtBearer(services);
                services.AddAuthorization();
            }
        }
        else
        {
            AddPlaceholderJwtBearer(services);
            services.AddAuthorization();
        }

        return services;
    }

    private static void AddValidatedJwtBearer(IServiceCollection services, JwtOptions jwt)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    RoleClaimType = ShijiatongClaimTypes.Role,
                };
            });
    }

    private static void AddPlaceholderJwtBearer(IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                };
            });
    }
}
