using Shijiatong.Lvr;

namespace Shijiatong.Lvr.Tests;

public class LvrParsingTests
{
    [Theory]
    [InlineData("1150904", 2026, 9, 4)]
    [InlineData("1050329", 2016, 3, 29)]
    [InlineData("991231", 2010, 12, 31)]   // 民國 100 年前 6 碼
    [InlineData("1150900", 2026, 9, 1)]    // 日為 00 → 1 日
    public void ParseRocDate_valid(string s, int y, int m, int d) =>
        Assert.Equal(new DateOnly(y, m, d), LvrParsing.ParseRocDate(s));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("11513xx")]
    [InlineData("1151301")]  // 13 月
    [InlineData("1150231")]  // 2/31
    public void ParseRocDate_invalid(string? s) => Assert.Null(LvrParsing.ParseRocDate(s));

    [Theory]
    [InlineData("0710216", 1982, 2, 16)]
    [InlineData("1140302", 2025, 3, 2)]
    [InlineData("06205", 1973, 5, 1)]   // 5 碼民國年月
    public void ParseRocCompletion_valid(string s, int y, int m, int d) =>
        Assert.Equal(new DateOnly(y, m, d), LvrParsing.ParseRocCompletion(s));

    [Theory]
    [InlineData("")]
    [InlineData("0710")]
    public void ParseRocCompletion_invalid(string s) => Assert.Null(LvrParsing.ParseRocCompletion(s));

    [Fact]
    public void AgeYears_counts_full_years_and_clamps_presale_to_zero()
    {
        Assert.Equal(44, LvrParsing.AgeYears(new DateOnly(2026, 9, 4), new DateOnly(1982, 2, 16)));
        Assert.Equal(43, LvrParsing.AgeYears(new DateOnly(2026, 2, 15), new DateOnly(1982, 2, 16)));
        Assert.Equal(0, LvrParsing.AgeYears(new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1)));
        Assert.Null(LvrParsing.AgeYears(new DateOnly(2026, 1, 1), null));
    }

    [Theory]
    [InlineData("一", 1)]
    [InlineData("十", 10)]
    [InlineData("十一", 11)]
    [InlineData("二十四", 24)]
    [InlineData("三十六", 36)]
    [InlineData("八十五", 85)]
    [InlineData("一百零一", 101)]
    [InlineData("１２", 12)]
    [InlineData("7", 7)]
    public void ParseChineseNumber_valid(string s, int expected) =>
        Assert.Equal(expected, LvrParsing.ParseChineseNumber(s));

    [Theory]
    [InlineData("")]
    [InlineData("騎樓")]
    public void ParseChineseNumber_invalid(string s) => Assert.Null(LvrParsing.ParseChineseNumber(s));

    [Theory]
    [InlineData("十八層", new[] { 18 })]
    [InlineData("地下一層", new[] { -1 })]
    [InlineData("地下層", new[] { -1 })]
    [InlineData("一層，二層，騎樓", new[] { 1, 2 })]
    [InlineData("一層，見其他登記事項", new[] { 1 })]
    [InlineData("全", new int[0])]
    [InlineData("", new int[0])]
    public void ParseFloors(string s, int[] expected) =>
        Assert.Equal(expected, LvrParsing.ParseFloors(s));

    [Theory]
    [InlineData("臺中市南區新榮街４１號", "南區", "新榮街")]
    [InlineData("臺中市西區三民路一段５０巷８號１０樓之３６", "西區", "三民路一段")]
    [InlineData("臺中市沙鹿區臺灣大道七段７３７號十樓之７", "沙鹿區", "臺灣大道七段")]
    [InlineData("臺中市大里區大里路１號", "大里區", "大里路")]
    [InlineData("臺中市西屯區西平里12鄰市政路１號", "西屯區", "市政路")]
    [InlineData("臺中市北屯區大坑十一街５號", "北屯區", "大坑十一街")]
    public void ExtractRoad_valid(string address, string district, string expected) =>
        Assert.Equal(expected, LvrParsing.ExtractRoad(address, district));

    [Theory]
    [InlineData("麻園頭段930地號", "西區")]
    [InlineData("", "西區")]
    public void ExtractRoad_none(string address, string district) =>
        Assert.Null(LvrParsing.ExtractRoad(address, district));

    [Fact]
    public void Unit_conversions()
    {
        Assert.Equal(24.38m, LvrParsing.SquareMetersToPing(80.61m));
        // 94281 元/㎡ ≈ 31.17 萬/坪
        Assert.Equal(31.17m, LvrParsing.NtdPerSqmToWanPerPing(94281m));
    }
}
