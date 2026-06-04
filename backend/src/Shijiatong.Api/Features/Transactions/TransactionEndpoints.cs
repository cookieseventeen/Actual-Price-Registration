using Microsoft.EntityFrameworkCore;
using Shijiatong.Api.Contracts;
using Shijiatong.Api.Domain.Entities;
using Shijiatong.Api.Infrastructure;

namespace Shijiatong.Api.Features.Transactions;

public static class TransactionEndpoints
{
    private static readonly HashSet<string> AllowedSortFields =
        new(StringComparer.OrdinalIgnoreCase) { "date", "total", "unit", "ping", "age" };

    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        // 對應前端 filterTransactions / sortTransactions（src/lib/query.ts）
        app.MapGet("/api/transactions", async (
            AppDbContext db,
            string? district, string? q, string? type, string? layout, string? trade,
            int? minTotal, int? maxTotal, decimal? minUnit, decimal? maxUnit,
            decimal? minPing, decimal? maxPing, int? minAge, int? maxAge,
            string? dateFrom, string? dateTo,
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

            if (minTotal is not null) query = query.Where(t => t.Total >= minTotal);
            if (maxTotal is not null) query = query.Where(t => t.Total <= maxTotal);
            if (minUnit is not null) query = query.Where(t => t.Unit >= minUnit);
            if (maxUnit is not null) query = query.Where(t => t.Unit <= maxUnit);
            if (minPing is not null) query = query.Where(t => t.Ping >= minPing);
            if (maxPing is not null) query = query.Where(t => t.Ping <= maxPing);
            if (minAge is not null) query = query.Where(t => t.Age >= minAge);
            if (maxAge is not null) query = query.Where(t => t.Age <= maxAge);

            if (TryParseDate(dateFrom, out var from)) query = query.Where(t => t.Date >= from);
            if (TryParseDate(dateTo, out var to)) query = query.Where(t => t.Date <= to);

            var desc = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);
            var sortKey = AllowedSortFields.Contains(sort ?? "") ? sort!.ToLowerInvariant() : "date";
            query = sortKey switch
            {
                "total" => desc ? query.OrderByDescending(t => t.Total) : query.OrderBy(t => t.Total),
                "unit" => desc ? query.OrderByDescending(t => t.Unit) : query.OrderBy(t => t.Unit),
                "ping" => desc ? query.OrderByDescending(t => t.Ping) : query.OrderBy(t => t.Ping),
                "age" => desc ? query.OrderByDescending(t => t.Age) : query.OrderBy(t => t.Age),
                "date" => desc ? query.OrderByDescending(t => t.Date) : query.OrderBy(t => t.Date),
                _ => query.OrderByDescending(t => t.Date),
            };

            var total = await query.CountAsync();
            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
            var items = await query
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(t => TransactionDto.From(t))
                .ToListAsync();

            return Results.Ok(new TransactionListResponse(
                items, total, page, pageSize, totalPages, page < totalPages));
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

    private static bool TryParseDate(string? value, out DateOnly date) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", out date);
}

public record TransactionListResponse(
    IReadOnlyList<TransactionDto> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasNext);
