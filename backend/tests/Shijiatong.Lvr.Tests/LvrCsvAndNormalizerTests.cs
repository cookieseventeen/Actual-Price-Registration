using Shijiatong.Lvr;

namespace Shijiatong.Lvr.Tests;

public class LvrCsvAndNormalizerTests
{
    // 真實表頭（33 欄）＋英文表頭（截短不影響，只看第一欄）＋開放資料原樣的幾列
    private const string Header =
        "﻿鄉鎮市區,交易標的,土地位置建物門牌,土地移轉總面積平方公尺,都市土地使用分區,非都市土地使用分區,非都市土地使用編定,交易年月日,交易筆棟數,移轉層次,總樓層數,建物型態,主要用途,主要建材,建築完成年月,建物移轉總面積平方公尺,建物現況格局-房,建物現況格局-廳,建物現況格局-衛,建物現況格局-隔間,有無管理組織,總價元,單價元平方公尺,車位類別,車位移轉總面積平方公尺,車位總價元,備註,編號,主建物面積,附屬建物面積,陽台面積,電梯,移轉編號\n" +
        "The villages and towns urban district,a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z,aa,bb,cc,dd,ee,ff\n";

    private const string RowApartment =
        "南區,房地(土地+建物),臺中市南區新榮街４１號,16.94,住,,,1150904,土地1建物1車位0,一層,七層,華廈(10層含以下有電梯),住商用,鋼筋混凝土造,0710216,80.61,4,1,2,有,有,7600000,94281,,0.0,0,,RPQNMLMLRHLGFAB97DA,59.9,0.0,0.0,有,";
    private const string RowLand =
        "西區,土地,麻園頭段930地號,22.0,住,,,1150903,土地2建物0車位0,,,其他,,,,0.0,0,0,0,無,無,3500000,159091,,0.0,0,親友、員工、共有人或其他特殊關係間之交易；未登記建物；,RPSNMLOLRHLGFAB38DA,0.0,0.0,0.0,無,";
    private const string RowWithParking =
        "沙鹿區,房地(土地+建物)+車位,臺中市沙鹿區臺灣大道七段７３７號十樓之７,16.27,住,,,1150906,土地1建物1車位1,十層,十一層,住宅大樓(11層含以上有電梯),住家用,鋼筋混凝土造,1140302,120.57,2,2,1,有,有,10380000,110174,坡道平面,39.97,1500000,其他增建；,RPTNMLQJRHLGFFB98DA,48.98,0.0,4.45,有,0003";
    private const string RowFamily =
        "北屯區,房地(土地+建物),臺中市北屯區崇德路１號,10.0,住,,,1150101,土地1建物1車位0,五層,十二層,住宅大樓(11層含以上有電梯),住家用,鋼筋混凝土造,1000101,100.0,3,2,2,有,有,5000000,50000,,0.0,0,親友、員工、共有人或其他特殊關係間之交易；,X1,70.0,0.0,0.0,有,";
    private const string RowShop =
        "西屯區,房地(土地+建物),臺中市西屯區市政路１號,10.0,商,,,1150101,土地1建物1車位0,一層,三層,店面(店鋪),商業用,鋼筋混凝土造,1000101,100.0,0,0,1,無,無,20000000,200000,,0.0,0,,X2,90.0,0.0,0.0,無,";

    private static LvrCsvResult Read(params string[] rows) =>
        LvrCsv.Read(new StringReader(Header + string.Join("\r\n", rows) + "\r\n"));

    [Fact]
    public void Read_skips_both_headers_and_maps_columns()
    {
        var csv = Read(RowApartment, RowLand);
        Assert.Equal(33, csv.Header.Count);
        Assert.Equal("鄉鎮市區", csv.Header[0]);       // BOM 已去除
        Assert.Equal(2, csv.Records.Count);
        Assert.Equal(0, csv.MalformedRows);
        Assert.Equal("RPQNMLMLRHLGFAB97DA", csv.Records[0][LvrColumns.SerialNo]);
    }

    [Fact]
    public void Read_counts_rows_with_wrong_column_count_as_malformed()
    {
        var csv = Read(RowApartment, "南區,太短");
        Assert.Single(csv.Records);
        Assert.Equal(1, csv.MalformedRows);
    }

    [Fact]
    public void ParseRows_handles_quotes_commas_and_newlines()
    {
        var rows = LvrCsv.ParseRows(new StringReader("a,\"b,c\",\"say \"\"hi\"\"\"\n\"multi\nline\",x")).ToList();
        Assert.Equal(new[] { "a", "b,c", "say \"hi\"" }, rows[0]);
        Assert.Equal(new[] { "multi\nline", "x" }, rows[1]);
    }

    [Fact]
    public void Read_rejects_non_lvr_content() =>
        Assert.Throws<FormatException>(() => LvrCsv.Read(new StringReader("<html>no data</html>")));

    [Theory]
    [InlineData("﻿鄉鎮市區,交易標的", true)]
    [InlineData("<!DOCTYPE html>", false)]
    public void LooksLikeLvrCsv(string firstLine, bool expected) =>
        Assert.Equal(expected, LvrCsv.LooksLikeLvrCsv(firstLine));

    [Fact]
    public void Normalize_apartment_converts_units_and_formats()
    {
        var t = LvrNormalizer.Normalize(Read(RowApartment).Records[0]).Value!;
        Assert.Equal("南區", t.District);
        Assert.Equal("新榮街", t.Road);
        Assert.Equal("臺中市南區新榮街41號", t.Address);   // 全形數字已轉半形
        Assert.Equal("華廈", t.BuildingType);
        Assert.Equal(760, t.Total);
        Assert.Equal(31.17m, t.Unit);
        Assert.False(t.UnitDerived);
        Assert.Equal(24.38m, t.Ping);
        Assert.Equal("1/7", t.FloorLabel);
        Assert.Equal(1, t.Floor);
        Assert.Equal(7, t.TotalFloors);
        Assert.Equal(new DateOnly(2026, 9, 4), t.TradeDate);
        Assert.Equal(44, t.AgeYears);
        Assert.Equal("4房1廳", t.Layout);
        Assert.Null(t.ParkingType);
        Assert.True(t.HasElevator);
    }

    [Fact]
    public void Normalize_with_parking_keeps_parking_fields_and_notes()
    {
        var t = LvrNormalizer.Normalize(Read(RowWithParking).Records[0]).Value!;
        Assert.Equal("住宅大樓", t.BuildingType);
        Assert.Equal("臺灣大道七段", t.Road);
        Assert.Equal("10/11", t.FloorLabel);
        Assert.Equal("坡道平面", t.ParkingType);
        Assert.Equal(1500000, t.ParkingPriceNtd);
        Assert.Equal(1, t.AgeYears);
        Assert.Equal(new[] { "其他增建" }, t.Notes);
    }

    [Theory]
    [InlineData(RowLand, LvrExclusion.NotBuildingTarget)]
    [InlineData(RowFamily, LvrExclusion.SpecialRelation)]
    [InlineData(RowShop, LvrExclusion.NonResidentialType)]
    public void Normalize_excludes(string row, LvrExclusion expected)
    {
        var r = LvrNormalizer.Normalize(Read(row).Records[0]);
        Assert.Null(r.Value);
        Assert.Equal(expected, r.Excluded);
    }

    [Fact]
    public void Normalize_derives_unit_price_when_official_blank()
    {
        // 官方單價空白：(1000 萬 - 0) / 100 ㎡ = 100000 元/㎡ ≈ 33.06 萬/坪
        var row = RowFamily.Replace("親友、員工、共有人或其他特殊關係間之交易；", "").Replace(",5000000,50000,", ",10000000,,");
        var t = LvrNormalizer.Normalize(Read(row).Records[0]).Value!;
        Assert.True(t.UnitDerived);
        Assert.Equal(33.06m, t.Unit);
    }

    [Theory]
    [InlineData("全", new int[0], new int[0], 4, "全/4")]
    [InlineData("一層，二層", new[] { 1, 2 }, new[] { 1, 2 }, 5, "1-2/5")]
    [InlineData("地下一層", new int[0], new[] { -1 }, 12, "B1/12")]
    [InlineData("", new int[0], new int[0], null, "?/?")]
    public void FloorLabel_formats(string raw, int[] above, int[] all, int? total, string expected) =>
        Assert.Equal(expected, LvrNormalizer.FloorLabel(raw, above, all, total));

    [Fact]
    public void Season_validates_format_and_builds_url()
    {
        var f = LvrSources.Season("115s2");
        Assert.Equal("https://plvr.land.moi.gov.tw/DownloadSeason?season=115S2&fileName=b_lvr_land_a.csv", f.Url.ToString());
        Assert.Equal("115S2_b_lvr_land_a.csv", f.CacheFileName);
        Assert.Throws<ArgumentException>(() => LvrSources.Season("2026Q2"));
    }
}
