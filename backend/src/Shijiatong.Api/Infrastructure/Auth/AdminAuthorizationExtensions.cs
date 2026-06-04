namespace Shijiatong.Api.Infrastructure.Auth;

public static class AdminAuthorizationExtensions
{
    /// <summary>
    /// 僅在 <c>Auth:Enabled</c> 且 <c>Auth:ProtectAdminEndpoints</c> 皆為 true 時，對該 group 要求 Admin policy。
    /// </summary>
    public static RouteGroupBuilder RequireAdminWhenProtected(
        this RouteGroupBuilder group,
        IConfiguration configuration)
    {
        if (IsAdminProtectionActive(configuration))
            group.RequireAuthorization(PolicyNames.Admin);

        return group;
    }

    internal static bool IsAdminProtectionActive(IConfiguration configuration) =>
        configuration.GetValue<bool>($"{AuthOptions.SectionName}:Enabled")
        && configuration.GetValue<bool>($"{AuthOptions.SectionName}:ProtectAdminEndpoints");
}
