using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Shijiatong.Api.Tests.Auth;
using Shijiatong.Lvr;
using Xunit;

namespace Shijiatong.Api.Tests;

[Collection(IntegrationCollection.Name)]
public sealed class LvrImportEndpointTests(IntegrationTestFixture fx)
{
    [Fact]
    public async Task Import_WhenFlagOff_EndpointsNotMapped()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await fx.Client.GetAsync("/api/admin/lvr/runs")).StatusCode);
        var res = await fx.Client.PostAsJsonAsync("/api/admin/lvr/import", new { season = "115S3" });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Import_InsertsThenUnchangedThenUpdatesOnCorrection()
    {
        var a = LvrTestData.NewSerial();
        var b = LvrTestData.NewSerial();
        var source = new FakeLvrFileSource { Csv = LvrTestData.Csv(LvrTestData.Row(a), LvrTestData.Row(b), "南區,欄數不足") };
        using var client = fx.CreateLvrImportClient(source);

        var first = await ImportAsync(client, new { season = "115S3" });
        Assert.Equal("succeeded", first.GetProperty("status").GetString());
        Assert.Equal("115S3", first.GetProperty("source").GetString());
        Assert.Equal(3, first.GetProperty("rawRows").GetInt32());
        Assert.Equal(1, first.GetProperty("malformed").GetInt32());
        Assert.Equal(2, first.GetProperty("inserted").GetInt32());

        // 同內容重跑 → 全部未變
        var second = await ImportAsync(client, new { season = "115S3" });
        Assert.Equal(0, second.GetProperty("inserted").GetInt32());
        Assert.Equal(2, second.GetProperty("unchanged").GetInt32());

        // 官方更正 a 的總價 → 1 更新 1 未變
        source.Csv = LvrTestData.Csv(LvrTestData.Row(a, totalNtd: 7700000), LvrTestData.Row(b));
        var third = await ImportAsync(client, new { season = "115S3" });
        Assert.Equal(1, third.GetProperty("updated").GetInt32());
        Assert.Equal(1, third.GetProperty("unchanged").GetInt32());

        var runs = await client.GetFromJsonAsync<JsonElement[]>("/api/admin/lvr/runs?limit=100");
        Assert.Contains(runs!, r => r.GetProperty("id").GetString() == third.GetProperty("id").GetString());

        var summary = await client.GetFromJsonAsync<JsonElement>("/api/admin/lvr/staging/summary");
        Assert.True(summary.GetProperty("total").GetInt32() >= 2);
        Assert.Contains(summary.GetProperty("byDistrict").EnumerateArray(), d => d.GetProperty("key").GetString() == "南區");
    }

    [Fact]
    public async Task Import_Current_UsesCurrentSourceKey()
    {
        var source = new FakeLvrFileSource { Csv = LvrTestData.Csv(LvrTestData.Row(LvrTestData.NewSerial())) };
        using var client = fx.CreateLvrImportClient(source);

        var run = await ImportAsync(client, new { current = true });
        Assert.StartsWith("current-", run.GetProperty("source").GetString());
        Assert.Equal(1, run.GetProperty("inserted").GetInt32());
    }

    [Fact]
    public async Task Import_WhenSourceHasNoData_RecordsNoDataRun()
    {
        using var client = fx.CreateLvrImportClient(new FakeLvrFileSource { Csv = null });

        var run = await ImportAsync(client, new { season = "101S1" });
        Assert.Equal("noData", run.GetProperty("status").GetString());
        Assert.Equal(0, run.GetProperty("inserted").GetInt32());
        Assert.NotNull(run.GetProperty("finishedAt").GetString());
    }

    [Fact]
    public async Task Import_WhenFileIsNotLvrCsv_RecordsFailedRun()
    {
        using var client = fx.CreateLvrImportClient(new FakeLvrFileSource { Csv = "<html>oops</html>" });

        var run = await ImportAsync(client, new { season = "115S1" });
        Assert.Equal("failed", run.GetProperty("status").GetString());
        Assert.False(string.IsNullOrEmpty(run.GetProperty("error").GetString()));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"season\":\"2026Q1\"}")]
    [InlineData("{\"season\":\"115S3\",\"current\":true}")]
    public async Task Import_InvalidRequest_Returns400(string body)
    {
        var source = new FakeLvrFileSource { Csv = LvrTestData.Csv(LvrTestData.Row(LvrTestData.NewSerial())) };
        using var client = fx.CreateLvrImportClient(source);

        var res = await client.PostAsync("/api/admin/lvr/import", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task Import_WhenProtected_RequiresAdmin()
    {
        var source = new FakeLvrFileSource { Csv = LvrTestData.Csv(LvrTestData.Row(LvrTestData.NewSerial())) };

        using (var anon = fx.CreateLvrImportClient(source, protect: true))
        {
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await anon.PostAsJsonAsync("/api/admin/lvr/import", new { season = "115S3" })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/admin/lvr/runs")).StatusCode);
        }

        using (var member = fx.CreateLvrImportClient(source, protect: true, TestJwtFactory.CreateToken("member")))
            Assert.Equal(HttpStatusCode.Forbidden,
                (await member.PostAsJsonAsync("/api/admin/lvr/import", new { season = "115S3" })).StatusCode);

        Assert.Equal(0, source.Calls);

        using var admin = fx.CreateLvrImportClient(source, protect: true, TestJwtFactory.CreateToken("admin"));
        var run = await ImportAsync(admin, new { season = "115S3" });
        Assert.Equal("succeeded", run.GetProperty("status").GetString());
    }

    private static async Task<JsonElement> ImportAsync(HttpClient client, object body)
    {
        var res = await client.PostAsJsonAsync("/api/admin/lvr/import", body);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }
}
