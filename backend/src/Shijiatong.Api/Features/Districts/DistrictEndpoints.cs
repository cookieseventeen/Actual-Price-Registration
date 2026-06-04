using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Shijiatong.Api.Common.Caching;
using Shijiatong.Api.Contracts;
using Shijiatong.Api.Infrastructure;

namespace Shijiatong.Api.Features.Districts;

public static class DistrictEndpoints
{
    public static IEndpointRouteBuilder MapDistrictEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/districts", async (AppDbContext db, IDistributedCache cache) =>
        {
            var result = await cache.GetOrSetAsync("districts:all", TimeSpan.FromMinutes(5), async () =>
                await db.Districts.AsNoTracking()
                    .OrderByDescending(d => d.Volume)
                    .Select(d => DistrictDto.From(d))
                    .ToListAsync());
            return Results.Ok(result);
        })
        .WithName("ListDistricts")
        .WithSummary("行政區統計列表（Redis 快取 5 分鐘）");

        app.MapGet("/api/districts/{id}", async (string id, AppDbContext db) =>
        {
            var district = await db.Districts.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
            return district is null
                ? Results.Problem(statusCode: 404, title: "找不到行政區")
                : Results.Ok(DistrictDto.From(district));
        })
        .WithName("GetDistrict")
        .WithSummary("單一行政區詳情");

        return app;
    }
}
