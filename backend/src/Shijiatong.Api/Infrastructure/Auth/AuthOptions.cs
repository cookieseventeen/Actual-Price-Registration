namespace Shijiatong.Api.Infrastructure.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public bool Enabled { get; set; }

    public bool ProtectAdminEndpoints { get; set; }

    public bool MockSsoEnabled { get; set; }

    public JwtOptions Jwt { get; set; } = new();
}

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "shijiatong";

    public string Audience { get; set; } = "shijiatong-web";

    public string SigningKey { get; set; } = "";

    public int AccessTokenMinutes { get; set; } = 60;
}
