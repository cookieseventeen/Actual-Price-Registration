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
        app.MapGet("/api/analysis/price-distribution", async (AppDbContext db) =>
        {
            var units = await db.Transactions.AsNoTracking().Select(t => t.Unit).ToListAsync();
            var dist = Buckets.Select(b => new
            {
                range = b.Range,
                count = units.Count(u => u >= b.Lo && u < b.Hi),
            });
            return Results.Ok(dist);
        })
        .WithName("PriceDistribution")
        .WithSummary("單價區間分布（即時計算）");

        // 全市月趨勢：對應前端 CITY_TREND（暫以彙整後的代表值；未來改為月度聚合表）
        app.MapGet("/api/analysis/city-trend", () => Results.Ok(new
        {
            months = new[] { "6月", "7月", "8月", "9月", "10月", "11月", "12月", "1月", "2月", "3月", "4月", "5月" },
            unit = new[] { 30.1, 30.6, 31.2, 31.5, 32.0, 32.4, 32.9, 33.1, 33.5, 34.0, 34.4, 34.8 },
            volume = new[] { 3820, 3640, 3910, 4120, 4350, 4180, 3760, 3290, 3540, 4010, 4280, 4460 },
        }))
        .WithName("CityTrend")
        .WithSummary("全市月趨勢");

        app.MapGet("/api/analysis/ping-price", async (
            AppDbContext db, string? district, string? type) =>
        {
            var query = FilterTransactions(db, district, type);
            var points = await query
                .OrderBy(t => t.Ping)
                .Select(t => new PingPricePoint(t.Ping, t.Unit))
                .ToListAsync();
            return Results.Ok(new { count = points.Count, points });
        })
        .WithName("PingPrice")
        .WithSummary("區域坪數×單價散點（地圖探索）");

        app.MapGet("/api/analysis/district-summary", async (AppDbContext db) =>
        {
            var year = DateTime.UtcNow.Year;
            var thisYear = new DateOnly(year, 1, 1);
            var lastYear = thisYear.AddYears(-1);

            var rows = await db.Transactions.AsNoTracking()
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
        .WithSummary("各區彙總（均價、件數、年增率）");

        app.MapGet("/api/analysis/trend", async (AppDbContext db, string? district) =>
        {
            var query = FilterTransactions(db, district, type: null);
            var points = await query
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
        .WithSummary("區域月趨勢（成交日均價）");

        return app;
    }

    private static IQueryable<Transaction> FilterTransactions(
        AppDbContext db, string? district, string? type)
    {
        var query = db.Transactions.AsNoTracking().Include(t => t.District).AsQueryable();

        if (!string.IsNullOrWhiteSpace(district))
        {
            var d = district.Trim();
            query = query.Where(t => t.District!.Name == d || t.DistrictId == d);
        }

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(t => t.Type == type.Trim());

        return query;
    }
}
