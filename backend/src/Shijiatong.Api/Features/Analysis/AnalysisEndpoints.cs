using Microsoft.EntityFrameworkCore;
using Shijiatong.Api.Infrastructure;

namespace Shijiatong.Api.Features.Analysis;

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

        return app;
    }
}
