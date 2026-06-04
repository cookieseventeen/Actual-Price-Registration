using System.Net;
using Xunit;

namespace Shijiatong.Api.Tests;

[Collection(IntegrationCollection.Name)]
public sealed class AuthSkeletonTests(IntegrationTestFixture fx)
{
    [Fact]
    public async Task Auth_Skeleton_WhenEnabled_StillAllowsPublicEndpoints()
    {
        using var client = fx.CreateAuthEnabledClient();
        var res = await client.GetAsync("/api/districts");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
