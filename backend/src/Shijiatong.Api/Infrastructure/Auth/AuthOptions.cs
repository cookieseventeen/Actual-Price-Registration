namespace Shijiatong.Api.Infrastructure.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public bool Enabled { get; set; }

    public bool ProtectAdminEndpoints { get; set; }

    public bool MockSsoEnabled { get; set; }

    /// <summary>種子／升級為 admin 的 Email 清單（由設定注入，程式碼不寫死）。</summary>
    public string[] AdminEmails { get; set; } = [];

    public JwtOptions Jwt { get; set; } = new();
}

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "shijiatong";

    public string Audience { get; set; } = "shijiatong-web";

    public string SigningKey { get; set; } = "";

    public int AccessTokenMinutes { get; set; } = 60;
}
