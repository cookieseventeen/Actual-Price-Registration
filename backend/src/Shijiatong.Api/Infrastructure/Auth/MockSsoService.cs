using Microsoft.Extensions.Options;
using Shijiatong.Api.Domain.Entities;

namespace Shijiatong.Api.Infrastructure.Auth;

/// <summary>模擬 SSO 固定身分（對齊 <c>src/lib/auth.ts</c> 的 SSO_IDENTITIES）。</summary>
public sealed class MockSsoService(IOptions<AuthOptions> options)
{
    public sealed record SsoProfile(string Email, string Name, string Avatar, string Provider);

    private static readonly Dictionary<string, SsoProfile> Profiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["google"] = new("demo.user@gmail.com", "示範使用者", "示", "google"),
        ["apple"] = new("demo.user@icloud.com", "示範使用者", "示", "apple"),
        // 測試用：對應既有種子 active 會員（非 admin）
        ["member"] = new("kuanyu.chen@gmail.com", "陳冠宇", "陳", "google"),
    };

    public bool TryGetProfile(string provider, out SsoProfile profile)
    {
        if (provider.Equals("admin", StringComparison.OrdinalIgnoreCase))
            return TryGetAdminProfile(out profile);

        return Profiles.TryGetValue(provider, out profile!);
    }

    private bool TryGetAdminProfile(out SsoProfile profile)
    {
        var adminEmail = options.Value.AdminEmails
            .Select(e => e.Trim())
            .FirstOrDefault(e => !string.IsNullOrEmpty(e));

        if (adminEmail is null)
        {
            profile = default!;
            return false;
        }

        var local = adminEmail.Split('@')[0];
        var avatar = local.Length > 0 ? local[..1] : "?";
        profile = new SsoProfile(adminEmail, "管理員", avatar, "google");
        return true;
    }

    public Provider ToDomainProvider(string provider) =>
        provider.Equals("apple", StringComparison.OrdinalIgnoreCase) ? Provider.Apple : Provider.Google;
}
