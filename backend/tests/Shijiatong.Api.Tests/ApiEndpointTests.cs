using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Shijiatong.Api.Tests;

[Collection(IntegrationCollection.Name)]
public sealed class ApiEndpointTests(IntegrationTestFixture fx)
{
    private readonly HttpClient _client = fx.Client;

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var res = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task HealthReady_WhenDbAndRedisUp_ReturnsOk()
    {
        var res = await _client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ready", body.GetProperty("status").GetString());
        Assert.Equal("ok", body.GetProperty("checks").GetProperty("postgres").GetString());
        Assert.Equal("ok", body.GetProperty("checks").GetProperty("redis").GetString());
    }

    [Fact]
    public async Task Districts_ReturnsNonEmptyArray()
    {
        var res = await _client.GetAsync("/api/districts");
        res.EnsureSuccessStatusCode();
        var items = await res.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        Assert.False(string.IsNullOrEmpty(items[0].GetProperty("id").GetString()));
        Assert.False(string.IsNullOrEmpty(items[0].GetProperty("name").GetString()));
    }

    [Fact]
    public async Task Transactions_Pagination_ReturnsPagedShape()
    {
        var res = await _client.GetAsync("/api/transactions?page=1&pageSize=5");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("page").GetInt32());
        Assert.Equal(5, body.GetProperty("pageSize").GetInt32());
        Assert.True(body.GetProperty("total").GetInt32() > 0);
        var items = body.GetProperty("items");
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.True(items.GetArrayLength() <= 5);
    }

    [Fact]
    public async Task Transactions_FilterByDistrict_ReturnsSubset()
    {
        var res = await _client.GetAsync("/api/transactions?district=西屯區&pageSize=200");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(items);
        Assert.All(items, i => Assert.Equal("西屯區", i.GetProperty("district").GetString()));
    }

    [Fact]
    public async Task Transactions_FilterByTypeLayoutTradeAndQ()
    {
        var res = await _client.GetAsync(
            "/api/transactions?type=住宅大樓&layout=4房2廳&trade=成屋&q=聯聚");
        res.EnsureSuccessStatusCode();
        var items = (await res.Content.ReadFromJsonAsync<JsonElement>())!
            .GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(items);
        Assert.Contains(items, i => i.GetProperty("id").GetString() == "T0001");
    }

    [Fact]
    public async Task Transactions_SortByUnitDesc()
    {
        var res = await _client.GetAsync("/api/transactions?sort=unit&dir=desc&pageSize=5");
        res.EnsureSuccessStatusCode();
        var units = (await res.Content.ReadFromJsonAsync<JsonElement>())!
            .GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("unit").GetDecimal())
            .ToList();
        Assert.Equal(units.OrderByDescending(u => u), units);
    }

    [Fact]
    public async Task TransactionById_Existing_ReturnsOk()
    {
        var res = await _client.GetAsync("/api/transactions/T0001");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("T0001", body.GetProperty("id").GetString());
        Assert.Equal("聯聚理仁", body.GetProperty("community").GetString());
    }

    [Fact]
    public async Task TransactionById_Missing_Returns404()
    {
        var res = await _client.GetAsync("/api/transactions/does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Analysis_PriceDistribution_ReturnsBucketArray()
    {
        var res = await _client.GetAsync("/api/analysis/price-distribution");
        res.EnsureSuccessStatusCode();
        var buckets = await res.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(buckets);
        Assert.True(buckets.Length >= 5);
        Assert.False(string.IsNullOrEmpty(buckets[0].GetProperty("range").GetString()));
        Assert.True(buckets[0].GetProperty("count").GetInt32() >= 0);
    }

    [Fact]
    public async Task Analysis_CityTrend_ReturnsTrendShape()
    {
        var res = await _client.GetAsync("/api/analysis/city-trend");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.GetProperty("months").ValueKind);
        Assert.Equal(JsonValueKind.Array, body.GetProperty("unit").ValueKind);
        Assert.Equal(JsonValueKind.Array, body.GetProperty("volume").ValueKind);
        Assert.Equal(
            body.GetProperty("months").GetArrayLength(),
            body.GetProperty("unit").GetArrayLength());
    }

    [Fact]
    public async Task CrawlTasks_ReturnsNonEmpty()
    {
        var res = await _client.GetAsync("/api/crawl-tasks");
        res.EnsureSuccessStatusCode();
        var tasks = await res.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(tasks);
        Assert.NotEmpty(tasks);
        Assert.False(string.IsNullOrEmpty(tasks[0].GetProperty("id").GetString()));
    }

    [Fact]
    public async Task Members_ReturnsNonEmpty()
    {
        var res = await _client.GetAsync("/api/members");
        res.EnsureSuccessStatusCode();
        var members = await res.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(members);
        Assert.NotEmpty(members);
    }

    [Fact]
    public async Task Members_PatchStatus_Valid200_Invalid400()
    {
        var listRes = await _client.GetAsync("/api/members");
        listRes.EnsureSuccessStatusCode();
        var members = (await listRes.Content.ReadFromJsonAsync<JsonElement[]>())!;
        var pending = members.First(m => m.GetProperty("status").GetString() == "pending");
        var id = pending.GetProperty("id").GetString();

        var okRes = await _client.PatchAsJsonAsync($"/api/members/{id}/status", new { status = "active" });
        Assert.Equal(HttpStatusCode.OK, okRes.StatusCode);
        var updated = await okRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("active", updated.GetProperty("status").GetString());

        var badRes = await _client.PatchAsJsonAsync($"/api/members/{id}/status", new { status = "not-a-status" });
        Assert.Equal(HttpStatusCode.BadRequest, badRes.StatusCode);
    }

    [Fact]
    public async Task Members_PatchPlan_Valid200_Invalid400()
    {
        var listRes = await _client.GetAsync("/api/members");
        listRes.EnsureSuccessStatusCode();
        var members = (await listRes.Content.ReadFromJsonAsync<JsonElement[]>())!;
        var member = members.First(m => m.GetProperty("plan").GetString() == "free");
        var id = member.GetProperty("id").GetString();

        var okRes = await _client.PatchAsJsonAsync($"/api/members/{id}/plan", new { plan = "pro" });
        Assert.Equal(HttpStatusCode.OK, okRes.StatusCode);
        Assert.Equal("pro", (await okRes.Content.ReadFromJsonAsync<JsonElement>())!.GetProperty("plan").GetString());

        var badRes = await _client.PatchAsJsonAsync($"/api/members/{id}/plan", new { plan = "not-a-plan" });
        Assert.Equal(HttpStatusCode.BadRequest, badRes.StatusCode);
    }
}
