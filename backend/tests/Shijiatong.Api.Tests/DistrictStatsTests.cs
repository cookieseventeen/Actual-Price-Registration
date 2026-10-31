using Shijiatong.Api.Domain.Entities;
using Shijiatong.Api.Features.LvrImport;
using Xunit;

namespace Shijiatong.Api.Tests;

/// <summary>LvrTransformService.ComputeDistrictStats 的純邏輯測試（不需 DB）。</summary>
public sealed class DistrictStatsTests
{
    private static readonly DateOnly Latest = new(2026, 9, 30);

    private static IEnumerable<Transaction> Txs(int count, decimal unit, DateOnly date, string trade = "成屋", bool outlier = false) =>
        Enumerable.Range(0, count).Select(i => new Transaction
        {
            Id = $"{trade}-{date:yyyyMMdd}-{unit}-{i}-{outlier}",
            DistrictId = "xitun", Road = "", Type = "住宅大樓", Floor = "1/1", Layout = "", Source = "內政部",
            Parking = "無", Unit = unit, Date = date, Trade = trade, IsOutlier = outlier,
        });

    private static District Run(params IEnumerable<Transaction>[] groups)
    {
        var d = new District { Id = "xitun", Name = "西屯區" };
        LvrTransformService.ComputeDistrictStats([d], groups.SelectMany(g => g).ToList());
        return d;
    }

    [Fact]
    public void ComparableCoverage_ComputesChange()
    {
        var d = Run(Txs(20, 40m, Latest), Txs(20, 50m, Latest.AddMonths(-18)));
        Assert.Equal(20, d.Volume);
        Assert.Equal(40m, d.AvgUnitPrice);
        Assert.Equal(-20m, d.ChangePct);
    }

    [Fact]
    public void PriorWindowUnderCovered_ChangeIsNull()
    {
        // 實測情境：近 12 月大量、前 12 月只有零星遲延登記
        var d = Run(Txs(100, 37m, Latest), Txs(20, 54m, Latest.AddMonths(-18)));
        Assert.Equal(100, d.Volume);
        Assert.Null(d.ChangePct);
    }

    [Fact]
    public void TooFewPriorRows_ChangeIsNull()
    {
        var d = Run(Txs(5, 40m, Latest), Txs(5, 50m, Latest.AddMonths(-18)));
        Assert.Null(d.ChangePct);
    }

    [Fact]
    public void PresaleAndOutliers_ExcludedFromStats()
    {
        var d = Run(
            Txs(10, 40m, Latest),
            Txs(10, 90m, Latest, trade: "預售屋"),
            Txs(3, 500m, Latest, outlier: true));
        Assert.Equal(10, d.Volume);
        Assert.Equal(40m, d.AvgUnitPrice);
    }

    [Fact]
    public void NoData_ResetsToZeroAndNull()
    {
        var d = new District { Id = "xitun", Name = "西屯區", AvgUnitPrice = 42.8m, ChangePct = 3.2m, Volume = 1284 };
        var window = LvrTransformService.ComputeDistrictStats([d], []);
        Assert.Null(window);
        Assert.Equal(0, d.Volume);
        Assert.Equal(0m, d.AvgUnitPrice);
        Assert.Null(d.ChangePct);
    }
}
