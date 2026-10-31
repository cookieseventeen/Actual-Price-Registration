using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Shijiatong.Api.Tests;

/// <summary>第 3 段：staging → transactions 重建（fixture 已以 LvrTestData.FixtureCsv 匯入並重建）。</summary>
[Collection(IntegrationCollection.Name)]
public sealed class LvrRebuildTests(IntegrationTestFixture fx)
{
    private readonly HttpClient _client = fx.Client;

    private async Task<JsonElement> GetTx(string id)
    {
        var res = await _client.GetAsync($"/api/transactions/{id}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task ExistingHome_MappedToFrontendShape()
    {
        var t = await GetTx(LvrTestData.XitunExisting);
        Assert.Equal("西屯區", t.GetProperty("district").GetString());
        Assert.Equal("住宅大樓", t.GetProperty("type").GetString());
        Assert.Equal("成屋", t.GetProperty("trade").GetString());
        Assert.Equal(3000, t.GetProperty("total").GetInt32());
        Assert.Equal(99.17m, t.GetProperty("unit").GetDecimal()); // 300,000 元/㎡ ÷ 0.3025 ÷ 10,000
        Assert.Equal(30.25m, t.GetProperty("ping").GetDecimal());
        Assert.Equal("18/26", t.GetProperty("floor").GetString());
        Assert.Equal(7, t.GetProperty("age").GetInt32());
        Assert.Equal("3房2廳", t.GetProperty("layout").GetString());
        Assert.Equal("2026-05-01", t.GetProperty("date").GetString());
        Assert.Equal("坡道平面", t.GetProperty("parking").GetString());
        Assert.Equal("臺中市西屯區市政路1號十八樓", t.GetProperty("address").GetString());
        Assert.Equal(JsonValueKind.Null, t.GetProperty("section").ValueKind);
        Assert.False(t.GetProperty("isOutlier").GetBoolean());
        // 座標 = 西屯區中心點
        Assert.Equal(24.1815, t.GetProperty("lat").GetDouble());
        Assert.Equal(120.6265, t.GetProperty("lng").GetDouble());
    }

    [Fact]
    public async Task PresaleTransfer_MarkedAsPresale()
    {
        var t = await GetTx(LvrTestData.XitunPresale);
        Assert.Equal("預售屋", t.GetProperty("trade").GetString());
        Assert.Equal("2023-11-15", t.GetProperty("date").GetString()); // 簽約日
        Assert.Equal(0, t.GetProperty("age").GetInt32());
    }

    [Fact]
    public async Task Outlier_KeptButFlagged_AndExcludedFromStats()
    {
        var t = await GetTx(LvrTestData.XitunOutlier);
        Assert.True(t.GetProperty("isOutlier").GetBoolean());

        // 列表照常顯示（含異常值），統計排除
        var list = await _client.GetFromJsonAsync<JsonElement>("/api/transactions?district=西屯區&pageSize=200");
        var listTotal = list.GetProperty("total").GetInt32();
        var dist = await _client.GetFromJsonAsync<JsonElement[]>("/api/analysis/price-distribution?district=西屯區");
        var distTotal = dist!.Sum(b => b.GetProperty("count").GetInt32());
        Assert.Equal(listTotal - 1, distTotal);
    }

    [Theory]
    [InlineData(LvrTestData.BeitunFamily)]
    [InlineData(LvrTestData.XitunLandOnly)]
    public async Task ExcludedRecords_NotInTransactions(string id)
    {
        var res = await _client.GetAsync($"/api/transactions/{id}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task DemoTransactions_AreGone()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/transactions/T0001")).StatusCode);
    }

    [Fact]
    public async Task Districts_HaveRealStatsAndCentroids()
    {
        var districts = await _client.GetFromJsonAsync<JsonElement[]>("/api/districts");
        Assert.Equal(29, districts!.Length);

        var xitun = districts.Single(d => d.GetProperty("id").GetString() == "xitun");
        Assert.True(xitun.GetProperty("vol").GetInt32() >= 1);
        Assert.True(xitun.GetProperty("avg").GetDecimal() > 0);
        Assert.Equal("七期 / 單元二", xitun.GetProperty("zone").GetString());
        // fixture 前 12 個月只有 1 筆成屋 → 樣本不足，變化為 null（不是 0%）
        Assert.Equal(JsonValueKind.Null, xitun.GetProperty("change").ValueKind);

        // 新增的區：有資料、無生活圈說明
        var shalu = districts.Single(d => d.GetProperty("id").GetString() == "shalu");
        Assert.Equal("沙鹿區", shalu.GetProperty("name").GetString());
        Assert.True(shalu.GetProperty("vol").GetInt32() >= 1);
        Assert.Equal(JsonValueKind.Null, shalu.GetProperty("zone").ValueKind);
        Assert.True(shalu.GetProperty("lat").GetDouble() > 24);

        var shaluTx = await GetTx(LvrTestData.Shalu);
        Assert.Equal("全/4", shaluTx.GetProperty("floor").GetString());
        Assert.Equal("透天厝", shaluTx.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Trend_CanExcludePresale()
    {
        var all = await _client.GetFromJsonAsync<JsonElement[]>("/api/analysis/trend?district=西屯區");
        var existingOnly = await _client.GetFromJsonAsync<JsonElement[]>("/api/analysis/trend?district=西屯區&trade=成屋");
        Assert.Contains(all!, p => p.GetProperty("period").GetString() == "2023-11");
        Assert.DoesNotContain(existingOnly!, p => p.GetProperty("period").GetString() == "2023-11");
    }

    [Fact]
    public async Task CityTrend_ComputedFromRealData_TwelveMonths()
    {
        var body = await _client.GetFromJsonAsync<JsonElement>("/api/analysis/city-trend");
        Assert.Equal(12, body.GetProperty("months").GetArrayLength());
        Assert.Equal(12, body.GetProperty("unit").GetArrayLength());
        Assert.True(body.GetProperty("volume").EnumerateArray().Sum(v => v.GetInt32()) > 0);
    }

    [Fact]
    public async Task Rebuild_ReportsCountsAndIsIdempotent()
    {
        using var lvr = fx.CreateLvrImportClient(new FakeLvrFileSource());
        var first = await (await lvr.PostAsync("/api/admin/lvr/rebuild", null)).Content.ReadFromJsonAsync<JsonElement>();
        var second = await (await lvr.PostAsync("/api/admin/lvr/rebuild", null)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(first.GetProperty("kept").GetInt32() >= 6);
        Assert.True(first.GetProperty("excluded").GetProperty("specialRelation").GetInt32() >= 1);
        Assert.True(first.GetProperty("excluded").GetProperty("notBuildingTarget").GetInt32() >= 1);
        Assert.True(first.GetProperty("outliers").GetInt32() >= 1);
        Assert.True(first.GetProperty("presaleTransfers").GetInt32() >= 1);
        Assert.Equal(0, first.GetProperty("unknownDistrict").GetInt32());
        Assert.Equal(first.GetProperty("kept").GetInt32(), second.GetProperty("kept").GetInt32());

        var list = await _client.GetFromJsonAsync<JsonElement>("/api/transactions?pageSize=1");
        Assert.Equal(second.GetProperty("kept").GetInt32(), list.GetProperty("total").GetInt32());
    }
}
