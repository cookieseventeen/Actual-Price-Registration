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
    private const string Header =
        "﻿鄉鎮市區,交易標的,土地位置建物門牌,土地移轉總面積平方公尺,都市土地使用分區,非都市土地使用分區,非都市土地使用編定,交易年月日,交易筆棟數,移轉層次,總樓層數,建物型態,主要用途,主要建材,建築完成年月,建物移轉總面積平方公尺,建物現況格局-房,建物現況格局-廳,建物現況格局-衛,建物現況格局-隔間,有無管理組織,總價元,單價元平方公尺,車位類別,車位移轉總面積平方公尺,車位總價元,備註,編號,主建物面積,附屬建物面積,陽台面積,電梯,移轉編號\n" +
        "The villages and towns urban district,a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z,aa,bb,cc,dd,ee,ff\n";

    // 開放資料原樣的一列；{SERIAL} / {PRICE} 由測試替換（共用 DB，編號需唯一）
    private const string RowTemplate =
        "南區,房地(土地+建物),臺中市南區新榮街４１號,16.94,住,,,1150904,土地1建物1車位0,一層,七層,華廈(10層含以下有電梯),住商用,鋼筋混凝土造,0710216,80.61,4,1,2,有,有,{PRICE},94281,,0.0,0,,{SERIAL},59.9,0.0,0.0,有,";

    private static string Row(string serial, int price = 7600000) =>
        RowTemplate.Replace("{SERIAL}", serial).Replace("{PRICE}", price.ToString());

    private static string Csv(params string[] rows) => Header + string.Join("\r\n", rows) + "\r\n";

    private static string NewSerial() => "T" + Guid.NewGuid().ToString("N")[..18].ToUpperInvariant();

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
        var a = NewSerial();
        var b = NewSerial();
        var source = new FakeLvrFileSource { Csv = Csv(Row(a), Row(b), "南區,欄數不足") };
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
        source.Csv = Csv(Row(a, price: 7700000), Row(b));
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
        var source = new FakeLvrFileSource { Csv = Csv(Row(NewSerial())) };
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
        var source = new FakeLvrFileSource { Csv = Csv(Row(NewSerial())) };
        using var client = fx.CreateLvrImportClient(source);

        var res = await client.PostAsync("/api/admin/lvr/import", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task Import_WhenProtected_RequiresAdmin()
    {
        var source = new FakeLvrFileSource { Csv = Csv(Row(NewSerial())) };

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

    /// <summary>以記憶體 CSV 模擬下載；<see cref="Csv"/> 為 null 代表官方無資料。</summary>
    private sealed class FakeLvrFileSource : ILvrFileSource
    {
        public string? Csv { get; set; }
        public int Calls { get; private set; }

        public Task<LvrDownloadResult> GetAsync(LvrFileRef file, bool refresh = false, CancellationToken ct = default)
        {
            Calls++;
            if (Csv is null)
                return Task.FromResult(new LvrDownloadResult(file, LvrDownloadStatus.Unavailable, null, false, "fake：無資料"));

            var path = Path.Combine(Path.GetTempPath(), $"lvr-test-{Guid.NewGuid():N}.csv");
            File.WriteAllText(path, Csv, new UTF8Encoding(false)); // 內容已含 BOM 字元，與官方檔一致
            return Task.FromResult(new LvrDownloadResult(file, LvrDownloadStatus.Ok, path, false, null));
        }
    }
}
