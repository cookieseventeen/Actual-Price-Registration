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

        var jwt = configuration.GetSection($"{AuthOptions.SectionName}:Jwt").Get<JwtOptions>() ?? new JwtOptions();

        if (AdminAuthorizationExtensions.IsAdminProtectionActive(configuration))
        {
            if (string.IsNullOrWhiteSpace(jwt.SigningKey))
            {
                throw new InvalidOperationException(
                    "Auth:Jwt:SigningKey must be set when Auth:Enabled and Auth:ProtectAdminEndpoints are both true.");
            }

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // 保留 JWT 短 claim 名稱（如 role），與 Stage 2/3 契約一致
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

            services.AddAuthorization(options =>
            {
                options.AddPolicy(PolicyNames.Admin, policy => policy.RequireRole("admin"));
            });
        }
        else
        {
            // Stage 1 占位：Enabled 但未保護 admin 端點時，不驗證簽章
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

            services.AddAuthorization();
        }

        return services;
    }
}
