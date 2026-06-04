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

        return app;
    }
}
