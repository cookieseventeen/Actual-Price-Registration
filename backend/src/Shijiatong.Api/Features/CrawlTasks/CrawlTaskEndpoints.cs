using Microsoft.EntityFrameworkCore;
using Shijiatong.Api.Contracts;
using Shijiatong.Api.Infrastructure;

namespace Shijiatong.Api.Features.CrawlTasks;

public static class CrawlTaskEndpoints
{
    public static IEndpointRouteBuilder MapCrawlTaskEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/crawl-tasks", async (AppDbContext db) =>
        {
            var tasks = await db.CrawlTasks.AsNoTracking()
                .OrderBy(c => c.Id)
                .Select(c => CrawlTaskDto.From(c))
                .ToListAsync();
            return Results.Ok(tasks);
        })
        .WithName("ListCrawlTasks")
        .WithSummary("爬蟲任務狀態列表");

        return app;
    }
}
