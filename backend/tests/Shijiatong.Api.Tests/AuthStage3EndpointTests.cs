using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shijiatong.Api.Tests.Auth;
using Xunit;

namespace Shijiatong.Api.Tests;

[Collection(IntegrationCollection.Name)]
public sealed class AuthStage3EndpointTests(IntegrationTestFixture fx)
{
    [Fact]
    public async Task Auth_AdminSso_CanAccessMembers_WhenAllFlagsOn()
    {
        using var client = fx.CreateStage3Client();

        var ssoRes = await client.PostAsync("/api/auth/sso/admin", null);
        ssoRes.EnsureSuccessStatusCode();
        var sso = await ssoRes.Content.ReadFromJsonAsync<JsonElement>();
        var token = sso.GetProperty("token").GetString();
        Assert.False(string.IsNullOrEmpty(token));

        using var authed = fx.CreateStage3Client(token);
        var membersRes = await authed.GetAsync("/api/members");
        Assert.Equal(HttpStatusCode.OK, membersRes.StatusCode);
    }

    [Fact]
    public async Task Auth_MemberSso_CannotAccessMembers_Returns403()
    {
        using var client = fx.CreateStage3Client();

        var ssoRes = await client.PostAsync("/api/auth/sso/member", null);
        ssoRes.EnsureSuccessStatusCode();
        var token = (await ssoRes.Content.ReadFromJsonAsync<JsonElement>())!.GetProperty("token").GetString();

        using var authed = fx.CreateStage3Client(token);
        Assert.Equal(HttpStatusCode.Forbidden, (await authed.GetAsync("/api/members")).StatusCode);
    }

    [Fact]
    public async Task Auth_Register_CreatesPendingMember_AndReturnsToken()
    {
        using var client = fx.CreateStage3Client();
        var email = $"new.user.{Guid.NewGuid():N}@gmail.com";

        var res = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            name = "新使用者",
            avatar = "新",
            provider = "google",
            plan = "free",
            purpose = "自住購屋",
        });

        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
        Assert.Equal("pending", body.GetProperty("member").GetProperty("status").GetString());
        Assert.Equal(email, body.GetProperty("member").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Auth_Me_WithValidToken_ReturnsMember()
    {
        using var client = fx.CreateStage3Client();

        var ssoRes = await client.PostAsync("/api/auth/sso/admin", null);
        var token = (await ssoRes.Content.ReadFromJsonAsync<JsonElement>())!.GetProperty("token").GetString();

        using var authed = fx.CreateStage3Client(token);
        var meRes = await authed.GetAsync("/api/auth/me");
        meRes.EnsureSuccessStatusCode();
        var me = await meRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(TestAuthSettings.Stage3AdminEmail, me.GetProperty("email").GetString());
        Assert.False(me.TryGetProperty("role", out _));
    }

    [Fact]
    public async Task Auth_Me_WithoutToken_Returns401()
    {
        using var client = fx.CreateStage3Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Auth_MockSso_Google_ReturnsExpectedProfile()
    {
        using var client = fx.CreateStage3Client();
        var res = await client.PostAsync("/api/auth/sso/google", null);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("demo.user@gmail.com", body.GetProperty("email").GetString());
        Assert.Equal("示範使用者", body.GetProperty("name").GetString());
        Assert.Equal("示", body.GetProperty("avatar").GetString());
        Assert.Equal("google", body.GetProperty("provider").GetString());
        Assert.False(body.TryGetProperty("token", out _));
    }

    [Fact]
    public async Task Auth_Endpoints_WhenMockSsoDisabled_Return404()
    {
        using var client = fx.CreateAdminProtectedClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/auth/sso/google", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PostAsJsonAsync("/api/auth/register", new { email = "x@test.com", name = "x", avatar = "x", provider = "google", plan = "free", purpose = "x" }))
            .StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/auth/me")).StatusCode);
    }
}
