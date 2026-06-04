using Microsoft.EntityFrameworkCore;
using Shijiatong.Api.Contracts;
using Shijiatong.Api.Domain.Entities;
using Shijiatong.Api.Infrastructure;

namespace Shijiatong.Api.Features.Transactions;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        // 對應前端 filterTransactions / sortTransactions（src/lib/query.ts）
        app.MapGet("/api/transactions", async (
            AppDbContext db,
            string? district, string? q, string? type, string? layout, string? trade,
            string? sort, string? dir, int page = 1, int pageSize = 20) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var query = db.Transactions.AsNoTracking().Include(t => t.District).AsQueryable();

            if (!string.IsNullOrWhiteSpace(district))
                query = query.Where(t => t.District!.Name == district || t.DistrictId == district);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var k = q.Trim();
                query = query.Where(t =>
                    t.Community.Contains(k) || t.Road.Contains(k) ||
                    t.Section.Contains(k) || t.District!.Name.Contains(k));
            }

            if (!string.IsNullOrWhiteSpace(type)) query = query.Where(t => t.Type == type);
            if (!string.IsNullOrWhiteSpace(layout)) query = query.Where(t => t.Layout == layout);
            if (!string.IsNullOrWhiteSpace(trade)) query = query.Where(t => t.Trade == trade);

            var desc = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);
            query = (sort?.ToLowerInvariant()) switch
            {
                "total" => desc ? query.OrderByDescending(t => t.Total) : query.OrderBy(t => t.Total),
                "unit" => desc ? query.OrderByDescending(t => t.Unit) : query.OrderBy(t => t.Unit),
                "ping" => desc ? query.OrderByDescending(t => t.Ping) : query.OrderBy(t => t.Ping),
                "age" => desc ? query.OrderByDescending(t => t.Age) : query.OrderBy(t => t.Age),
                "date" => desc ? query.OrderByDescending(t => t.Date) : query.OrderBy(t => t.Date),
                _ => query.OrderByDescending(t => t.Date),
            };

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(t => TransactionDto.From(t))
                .ToListAsync();

            return Results.Ok(new PagedResult<TransactionDto>(items, total, page, pageSize));
        })
        .WithName("ListTransactions")
        .WithSummary("成交查詢（篩選 / 排序 / 分頁）");

        app.MapGet("/api/transactions/{id}", async (string id, AppDbContext db) =>
        {
            var tx = await db.Transactions.AsNoTracking().Include(t => t.District)
                .FirstOrDefaultAsync(t => t.Id == id);
            return tx is null
                ? Results.Problem(statusCode: 404, title: "找不到成交物件")
                : Results.Ok(TransactionDto.From(tx));
        })
        .WithName("GetTransaction")
        .WithSummary("單筆物件詳情");

        return app;
    }
}
