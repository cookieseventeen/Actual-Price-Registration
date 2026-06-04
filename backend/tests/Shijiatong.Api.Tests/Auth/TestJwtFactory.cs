using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Shijiatong.Api.Infrastructure.Auth;

namespace Shijiatong.Api.Tests.Auth;

/// <summary>測試專用 JWT 簽發（與 API 相同 Auth:Jwt 設定）。</summary>
public static class TestJwtFactory
{
    public const string SigningKey = "test-only-min-32-chars-secret-key!!";
    public const string Issuer = "shijiatong";
    public const string Audience = "shijiatong-web";

    public static string CreateToken(string role, string? sub = null, string? email = null)
    {
        var claims = new List<Claim>
        {
            new(ShijiatongClaimTypes.Role, role),
            new(ShijiatongClaimTypes.Sub, sub ?? Guid.NewGuid().ToString()),
            new(ShijiatongClaimTypes.Email, email ?? "admin@example.com"),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
