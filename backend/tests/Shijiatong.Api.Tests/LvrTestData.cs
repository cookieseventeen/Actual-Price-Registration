using System.Globalization;
using System.Text;
using Shijiatong.Lvr;

namespace Shijiatong.Api.Tests;

/// <summary>實價登錄開放資料測試用 CSV（格式與官方 b_lvr_land_a.csv 相同）與假下載來源。</summary>
public static class LvrTestData
{
    public const string Header =
        "﻿鄉鎮市區,交易標的,土地位置建物門牌,土地移轉總面積平方公尺,都市土地使用分區,非都市土地使用分區,非都市土地使用編定,交易年月日,交易筆棟數,移轉層次,總樓層數,建物型態,主要用途,主要建材,建築完成年月,建物移轉總面積平方公尺,建物現況格局-房,建物現況格局-廳,建物現況格局-衛,建物現況格局-隔間,有無管理組織,總價元,單價元平方公尺,車位類別,車位移轉總面積平方公尺,車位總價元,備註,編號,主建物面積,附屬建物面積,陽台面積,電梯,移轉編號\n" +
        "The villages and towns urban district,a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z,aa,bb,cc,dd,ee,ff\n";

    /// <summary>組一列 33 欄的原始資料（預設值取自官方檔的真實一列）。</summary>
    public static string Row(
        string serial,
        string district = "南區",
        string address = "臺中市南區新榮街４１號",
        string target = "房地(土地+建物)",
        string tradeDate = "1150904",
        string floor = "一層",
        string totalFloors = "七層",
        string type = "華廈(10層含以下有電梯)",
        string completed = "0710216",
        decimal areaSqm = 80.61m,
        int rooms = 4,
        int halls = 1,
        long totalNtd = 7600000,
        long unitNtdPerSqm = 94281,
        string parking = "",
        string note = "") =>
        string.Join(",",
            district, target, address, "16.94", "住", "", "", tradeDate, "土地1建物1車位0", floor, totalFloors,
            type, "住家用", "鋼筋混凝土造", completed, areaSqm.ToString(CultureInfo.InvariantCulture),
            rooms.ToString(CultureInfo.InvariantCulture), halls.ToString(CultureInfo.InvariantCulture), "2", "有", "有",
            totalNtd.ToString(CultureInfo.InvariantCulture), unitNtdPerSqm.ToString(CultureInfo.InvariantCulture),
            parking, "0.0", "0", note, serial, "59.9", "0.0", "0.0", "有", "");

    public static string Csv(params string[] rows) => Header + string.Join("\r\n", rows) + "\r\n";

    public static string NewSerial() => "T" + Guid.NewGuid().ToString("N")[..18].ToUpperInvariant();

    // ── 整合測試共用的基礎資料（fixture 啟動時 import → rebuild）──
    public const string XitunExisting = "FXXITUN00000000001";
    public const string XitunPrior = "FXXITUN00000000002";
    public const string XitunPresale = "FXXITUN00000000003";
    public const string XitunOutlier = "FXXITUN00000000004";
    public const string Beitun = "FXBEITUN0000000001";
    public const string Shalu = "FXSHALU00000000001";
    public const string BeitunFamily = "FXFAMILY0000000001";
    public const string XitunLandOnly = "FXLAND000000000001";

    public static readonly string FixtureCsv = Csv(
        // 西屯一般成屋：30,000,000 元、300,000 元/㎡ → 99.17 萬/坪、100 ㎡ = 30.25 坪
        Row(XitunExisting, "西屯區", "臺中市西屯區市政路１號十八樓", tradeDate: "1150501", floor: "十八層",
            totalFloors: "二十六層", type: "住宅大樓(11層含以上有電梯)", completed: "1080101", areaSqm: 100m,
            rooms: 3, halls: 2, totalNtd: 30000000, unitNtdPerSqm: 300000, parking: "坡道平面"),
        // 西屯前一年度
        Row(XitunPrior, "西屯區", "臺中市西屯區惠中路一段２號", tradeDate: "1140301", floor: "九層",
            totalFloors: "十五層", type: "住宅大樓(11層含以上有電梯)", completed: "1050101", areaSqm: 100m,
            rooms: 3, halls: 2, totalNtd: 28000000, unitNtdPerSqm: 280000),
        // 西屯預售屋交屋：交易日是簽約日 2023-11-15
        Row(XitunPresale, "西屯區", "臺中市西屯區河南路三段３號", tradeDate: "1121115", floor: "十層",
            totalFloors: "二十層", type: "住宅大樓(11層含以上有電梯)", completed: "1150301", areaSqm: 100m,
            rooms: 3, halls: 2, totalNtd: 25000000, unitNtdPerSqm: 250000, note: "預售屋、或土地及建物分件登記案件；"),
        // 西屯單價異常（10,000 元/㎡ ≈ 3.31 萬/坪）
        Row(XitunOutlier, "西屯區", "臺中市西屯區文心路二段４號", tradeDate: "1150601", type: "住宅大樓(11層含以上有電梯)",
            areaSqm: 100m, totalNtd: 1000000, unitNtdPerSqm: 10000),
        Row(Beitun, "北屯區", "臺中市北屯區崇德路二段５號", tradeDate: "1150601", floor: "五層", totalFloors: "十層",
            completed: "1000101", totalNtd: 12000000, unitNtdPerSqm: 150000),
        // 新增的 17 區之一，透天「全」
        Row(Shalu, "沙鹿區", "臺中市沙鹿區臺灣大道七段６號", tradeDate: "1150701", floor: "全", totalFloors: "四層",
            type: "透天厝", completed: "1100101", areaSqm: 150m, rooms: 4, halls: 2, totalNtd: 15000000, unitNtdPerSqm: 100000),
        // 應排除：親友交易、純土地
        Row(BeitunFamily, "北屯區", "臺中市北屯區崇德路二段７號", note: "親友、員工、共有人或其他特殊關係間之交易；"),
        Row(XitunLandOnly, "西屯區", "西屯段123地號", target: "土地", type: "其他"));
}

/// <summary>以記憶體 CSV 模擬下載；<see cref="Csv"/> 為 null 代表官方無資料。</summary>
public sealed class FakeLvrFileSource : ILvrFileSource
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
