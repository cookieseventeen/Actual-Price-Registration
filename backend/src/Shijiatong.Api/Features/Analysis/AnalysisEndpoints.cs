using Microsoft.EntityFrameworkCore;
using Shijiatong.Api.Domain.Entities;
using Shijiatong.Api.Infrastructure;

namespace Shijiatong.Api.Features.Analysis;

public record PingPricePoint(decimal Ping, decimal Unit);

public record DistrictSummaryItem(
    string DistrictId,
    string District,
    decimal AvgUnit,
    int Count,
    decimal? ChangePct);

public record TrendPoint(string Period, decimal AvgUnit, int Volume);

public record CityTrend(IReadOnlyList<string> Months, IReadOnlyList<decimal> Unit, IReadOnlyList<int> Volume);

// 所有統計一律排除單價異常值（Transaction.IsOutlier）；列表 / 詳情仍照常顯示。
// trade 參數（成屋 / 預售屋）可篩掉預售屋交屋——其交易日是簽約日，會把舊價格帶進時間序列。
public static class AnalysisEndpoints
{
    // 單價區間（萬/坪）→ 由 DB 交易資料即時計算，對應前端 PRICE_DISTRIBUTION
    private static readonly (string Range, decimal Lo, decimal Hi)[] Buckets =
    [
        ("~20", 0, 20), ("20-30", 20, 30), ("30-40", 30, 40), ("40-50", 40, 50),
        ("50-60", 50, 60), ("60-80", 60, 80), ("80+", 80, decimal.MaxValue),
    ];

    public static IEndpointRouteBuilder MapAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/analysis/price-distribution", async (AppDbContext db, string? district, string? trade) =>
        {
            var units = await FilterTransactions(db, district, type: null, trade).Select(t => t.Unit).ToListAsync();
            var dist = Buckets.Select(b => new
            {
                range = b.Range,
                count = units.Count(u => u >= b.Lo && u < b.Hi),
            });
            return Results.Ok(dist);
        })
        .WithName("PriceDistribution")
        .WithSummary("單價區間分布（即時計算，排除異常值）");

        // 全市月趨勢：以資料中最新交易月往回 12 個月（無成交的月份補 0）
        app.MapGet("/api/analysis/city-trend", async (AppDbContext db, string? trade) =>
        {
            var query = FilterTransactions(db, district: null, type: null, trade);
            var latest = await query.MaxAsync(t => (DateOnly?)t.Date);
            if (latest is null) return Results.Ok(new CityTrend([], [], []));

            var start = new DateOnly(latest.Value.Year, latest.Value.Month, 1).AddMonths(-11);
            var rows = await query
                .Where(t => t.Date >= start)
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Avg = g.Average(t => t.Unit), Count = g.Count() })
                .ToListAsync();

            var months = new List<string>(12);
            var unit = new List<decimal>(12);
            var volume = new List<int>(12);
            for (var m = start; m <= latest.Value; m = m.AddMonths(1))
            {
                var row = rows.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month);
                months.Add($"{m.Month}月");
                unit.Add(row is null ? 0 : Math.Round(row.Avg, 1));
                volume.Add(row?.Count ?? 0);
            }
            return Results.Ok(new CityTrend(months, unit, volume));
        })
        .WithName("CityTrend")
        .WithSummary("全市月趨勢（最近 12 個月，排除異常值）");

        app.MapGet("/api/analysis/ping-price", async (
            AppDbContext db, string? district, string? type, string? trade) =>
        {
            var points = await FilterTransactions(db, district, type, trade)
                .OrderBy(t => t.Ping)
                .Select(t => new PingPricePoint(t.Ping, t.Unit))
                .ToListAsync();
            return Results.Ok(new { count = points.Count, points });
        })
        .WithName("PingPrice")
        .WithSummary("區域坪數×單價散點（地圖探索，排除異常值）");

        app.MapGet("/api/analysis/district-summary", async (AppDbContext db, string? trade) =>
        {
            var year = DateTime.UtcNow.Year;
            var thisYear = new DateOnly(year, 1, 1);
            var lastYear = thisYear.AddYears(-1);

            var rows = await FilterTransactions(db, district: null, type: null, trade)
                .GroupBy(t => new { t.DistrictId, DistrictName = t.District!.Name })
                .Select(g => new
                {
                    g.Key.DistrictId,
                    g.Key.DistrictName,
                    AvgUnit = g.Average(t => t.Unit),
                    Count = g.Count(),
                    ThisYearAvg = g.Where(t => t.Date >= thisYear).Average(t => (decimal?)t.Unit),
                    LastYearAvg = g.Where(t => t.Date >= lastYear && t.Date < thisYear)
                        .Average(t => (decimal?)t.Unit),
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var items = rows.Select(x => new DistrictSummaryItem(
                x.DistrictId,
                x.DistrictName,
                Math.Round(x.AvgUnit, 1),
                x.Count,
                x.LastYearAvg is > 0 and var ly && x.ThisYearAvg is { } ty
                    ? Math.Round((ty - ly) / ly * 100, 1)
                    : null)).ToList();

            return Results.Ok(items);
        })
        .WithName("DistrictSummary")
        .WithSummary("各區彙總（均價、件數、年增率，排除異常值）");

        app.MapGet("/api/analysis/trend", async (AppDbContext db, string? district, string? trade) =>
        {
            var points = await FilterTransactions(db, district, type: null, trade)
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new TrendPoint(
                    $"{g.Key.Year}-{g.Key.Month:D2}",
                    Math.Round(g.Average(t => t.Unit), 1),
                    g.Count()))
                .ToListAsync();

            return Results.Ok(points);
        })
        .WithName("DistrictTrend")
        .WithSummary("區域月趨勢（成交日均價，排除異常值）");

        return app;
    }

    /// <summary>統計用查詢：排除異常值，可依區 / 型態 / 交易類別篩選。</summary>
    private static IQueryable<Transaction> FilterTransactions(
        AppDbContext db, string? district, string? type, string? trade)
    {
        var query = db.Transactions.AsNoTracking().Where(t => !t.IsOutlier);

        if (!string.IsNullOrWhiteSpace(district))
        {
            var d = district.Trim();
            query = query.Where(t => t.District!.Name == d || t.DistrictId == d);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var ty = type.Trim();
            query = query.Where(t => t.Type == ty);
        }

        if (!string.IsNullOrWhiteSpace(trade))
        {
            var tr = trade.Trim();
            query = query.Where(t => t.Trade == tr);
        }

        return query;
    }
}
