using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shijiatong.Api.Domain.Entities;

namespace Shijiatong.Api.Infrastructure.Auth;

public sealed class JwtTokenService(IOptions<AuthOptions> options) : IJwtTokenService
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.Jwt.SigningKey);

    public string CreateAccessToken(Member member)
    {
        var jwt = options.Value.Jwt;
        if (string.IsNullOrWhiteSpace(jwt.SigningKey))
        {
            throw new InvalidOperationException(
                "Auth:Jwt:SigningKey must be set before issuing access tokens.");
        }

        var claims = new[]
        {
            new Claim(ShijiatongClaimTypes.Sub, member.Id.ToString()),
            new Claim(ShijiatongClaimTypes.Email, member.Email),
            new Claim(ShijiatongClaimTypes.Role, ToRoleClaim(member.Role)),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(jwt.AccessTokenMinutes > 0 ? jwt.AccessTokenMinutes : 60);

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    internal static string ToRoleClaim(MemberRole role) =>
        role == MemberRole.Admin ? "admin" : "member";
}
