using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Shijiatong.Api.Infrastructure;

namespace Shijiatong.Api.Features.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Liveness：行程活著就回 200
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("Liveness").ExcludeFromDescription();

        // Readiness：DB 可連線 + Redis 可讀寫，供 compose / 監控判斷可服務
        app.MapGet("/health/ready", async (AppDbContext db, IDistributedCache cache) =>
        {
            var checks = new Dictionary<string, string>();
            var healthy = true;

            try { healthy &= await db.Database.CanConnectAsync(); checks["postgres"] = "ok"; }
            catch (Exception ex) { healthy = false; checks["postgres"] = ex.GetType().Name; }

            try
            {
                await cache.SetStringAsync("health:ping", "1",
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10) });
                _ = await cache.GetStringAsync("health:ping");
                checks["redis"] = "ok";
            }
            catch (Exception ex) { healthy = false; checks["redis"] = ex.GetType().Name; }

            return healthy
                ? Results.Ok(new { status = "ready", checks })
                : Results.Json(new { status = "unready", checks }, statusCode: 503);
        }).WithName("Readiness").ExcludeFromDescription();

        return app;
    }
}
