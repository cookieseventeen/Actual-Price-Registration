using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Shijiatong.Api.Infrastructure.Auth;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddShijiatongAuth(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>($"{AuthOptions.SectionName}:Enabled"))
            return services;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Stage 1 占位：尚未啟用簽章驗證（Stage 2/3 再依 Jwt 設定啟用）
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                };
            });

        services.AddAuthorization();

        return services;
    }
}
