using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Shijiatong.Api.Tests.Auth;
using Xunit;

namespace Shijiatong.Api.Tests;

[Collection(IntegrationCollection.Name)]
public sealed class AuthAdminEndpointTests(IntegrationTestFixture fx)
{
    [Fact]
    public async Task Members_WhenProtectionOn_WithoutToken_Returns401()
    {
        using var client = fx.CreateAdminProtectedClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/members")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PatchAsJsonAsync($"/api/members/{Guid.NewGuid()}/status", new { status = "active" }))
            .StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PatchAsJsonAsync($"/api/members/{Guid.NewGuid()}/plan", new { plan = "pro" }))
            .StatusCode);
    }

    [Fact]
    public async Task Members_WhenProtectionOn_NonAdminToken_Returns403()
    {
        using var client = fx.CreateAdminProtectedClient(TestJwtFactory.CreateToken("member"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/members")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PatchAsJsonAsync($"/api/members/{Guid.NewGuid()}/status", new { status = "active" }))
            .StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PatchAsJsonAsync($"/api/members/{Guid.NewGuid()}/plan", new { plan = "pro" }))
            .StatusCode);
    }

    [Fact]
    public async Task Members_WhenProtectionOn_WithAdminToken_ReturnsExpectedContracts()
    {
        using var client = fx.CreateAdminProtectedClient(TestJwtFactory.CreateToken("admin"));

        var listRes = await client.GetAsync("/api/members");
        listRes.EnsureSuccessStatusCode();
        var members = (await listRes.Content.ReadFromJsonAsync<JsonElement[]>())!;
        Assert.NotEmpty(members);

        var pending = members.First(m => m.GetProperty("status").GetString() == "pending");
        var id = pending.GetProperty("id").GetString();

        var okStatus = await client.PatchAsJsonAsync($"/api/members/{id}/status", new { status = "active" });
        Assert.Equal(HttpStatusCode.OK, okStatus.StatusCode);
        Assert.Equal("active", (await okStatus.Content.ReadFromJsonAsync<JsonElement>())!.GetProperty("status")
            .GetString());

        var badStatus = await client.PatchAsJsonAsync($"/api/members/{id}/status", new { status = "not-a-status" });
        Assert.Equal(HttpStatusCode.BadRequest, badStatus.StatusCode);

        var member = members.First(m => m.GetProperty("plan").GetString() == "free");
        var planId = member.GetProperty("id").GetString();

        var okPlan = await client.PatchAsJsonAsync($"/api/members/{planId}/plan", new { plan = "pro" });
        Assert.Equal(HttpStatusCode.OK, okPlan.StatusCode);
        Assert.Equal("pro", (await okPlan.Content.ReadFromJsonAsync<JsonElement>())!.GetProperty("plan").GetString());

        var badPlan = await client.PatchAsJsonAsync($"/api/members/{planId}/plan", new { plan = "not-a-plan" });
        Assert.Equal(HttpStatusCode.BadRequest, badPlan.StatusCode);

        var missing = await client.PatchAsJsonAsync($"/api/members/{Guid.NewGuid()}/status", new { status = "active" });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Districts_WhenProtectionOn_StillPublic()
    {
        using var client = fx.CreateAdminProtectedClient();
        var res = await client.GetAsync("/api/districts");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
