using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shijiatong.Api.Domain.Entities;
using Shijiatong.Api.Infrastructure;
using Shijiatong.Api.Infrastructure.Auth;
using Shijiatong.Lvr;

namespace Shijiatong.Api.Features.LvrImport;

// 實價登錄開放資料匯入（第 2 段：原始列進 staging，手動觸發）。
// 只在 Lvr:ImportEnabled=true 時註冊路由；Auth 保護開啟時需 admin JWT。
public static class LvrImportEndpoints
{
    public static IServiceCollection AddLvrImport(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LvrImportOptions>(configuration.GetSection(LvrImportOptions.SectionName));
        services.AddSingleton<LvrImportGate>();
        services.AddSingleton<ILvrFileSource>(sp =>
        {
            var o = sp.GetRequiredService<IOptions<LvrImportOptions>>().Value;
            return new LvrDownloader(LvrDownloader.CreateHttpClient(), o.CacheDir, TimeSpan.FromSeconds(o.MinIntervalSeconds));
        });
        services.AddScoped<LvrImportService>();
        services.AddScoped<LvrTransformService>();
        return services;
    }

    public static IEndpointRouteBuilder MapLvrImportEndpoints(this IEndpointRouteBuilder app, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>($"{LvrImportOptions.SectionName}:ImportEnabled"))
            return app;

        var group = app.MapGroup("/api/admin/lvr").RequireAdminWhenProtected(configuration);

        // 同步執行（台中一季約 1 萬列，數秒內完成）。不綁 RequestAborted：用戶端斷線也讓本次匯入跑完，避免 run 卡在 running。
        group.MapPost("/import", async (ImportRequest? req, LvrImportService svc, LvrImportGate gate) =>
        {
            var hasSeason = !string.IsNullOrWhiteSpace(req?.Season);
            var current = req?.Current ?? false;
            if (hasSeason == current)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["season"] = ["season 與 current 須擇一：{\"season\":\"115S3\"} 或 {\"current\":true}"],
                });
            if (hasSeason && !LvrSources.IsValidSeason(req!.Season))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["season"] = [$"季別格式應為 115S3（民國年 + S1~S4），收到 '{req.Season}'"],
                });

            var file = hasSeason
                ? LvrSources.Season(req!.Season!)
                : LvrSources.Current(LvrSources.TaipeiToday(DateTime.UtcNow));

            if (!await gate.Semaphore.WaitAsync(0))
                return Results.Problem(statusCode: 409, title: "已有匯入正在執行，請稍後再試");
            try
            {
                var run = await svc.ImportAsync(file, req?.Refresh ?? false);
                return Results.Ok(LvrImportRunDto.From(run));
            }
            finally
            {
                gate.Semaphore.Release();
            }
        }).WithSummary("手動匯入一個季別或本期的台中不動產買賣開放資料到 staging");

        // 第 3 段：staging → transactions 全量重建 + 重算 districts 統計（與匯入共用同一把鎖）
        group.MapPost("/rebuild", async (LvrTransformService svc, LvrImportGate gate) =>
        {
            if (!await gate.Semaphore.WaitAsync(0))
                return Results.Problem(statusCode: 409, title: "已有匯入或重建正在執行，請稍後再試");
            try
            {
                return Results.Ok(await svc.RebuildAsync());
            }
            finally
            {
                gate.Semaphore.Release();
            }
        }).WithSummary("由 staging 重建成交資料（取代既有 transactions）並重算行政區統計");

        group.MapGet("/runs", async (int? limit, AppDbContext db) =>
        {
            var take = Math.Clamp(limit ?? 20, 1, 100);
            var runs = await db.LvrImportRuns.AsNoTracking()
                .OrderByDescending(r => r.StartedAt)
                .Take(take)
                .ToListAsync();
            return Results.Ok(runs.Select(LvrImportRunDto.From));
        }).WithSummary("最近的匯入紀錄");

        group.MapGet("/staging/summary", async (AppDbContext db) =>
        {
            var total = await db.LvrStagingRecords.CountAsync();
            var bySource = await db.LvrStagingRecords.AsNoTracking()
                .GroupBy(x => x.FirstSource)
                .Select(g => new CountItem(g.Key, g.Count()))
                .ToListAsync();
            var byDistrict = await db.LvrStagingRecords.AsNoTracking()
                .GroupBy(x => x.District)
                .Select(g => new CountItem(g.Key, g.Count()))
                .ToListAsync();
            return Results.Ok(new StagingSummary(
                total,
                bySource.OrderBy(x => x.Key, StringComparer.Ordinal).ToList(),
                byDistrict.OrderByDescending(x => x.Count).ToList()));
        }).WithSummary("staging 筆數（依首次來源、行政區）");

        return app;
    }

    public record ImportRequest(string? Season, bool Current, bool Refresh);
    public record CountItem(string Key, int Count);
    public record StagingSummary(int Total, IReadOnlyList<CountItem> BySource, IReadOnlyList<CountItem> ByDistrict);
}

public record LvrImportRunDto(
    string Id, string Source, string Status, string StartedAt, string? FinishedAt, double? DurationSeconds,
    bool FromCache, int RawRows, int Malformed, int Inserted, int Updated, int Unchanged, string? Error)
{
    public static LvrImportRunDto From(LvrImportRun r) => new(
        r.Id.ToString(), r.Source, JsonNamingPolicy.CamelCase.ConvertName(r.Status.ToString()),
        r.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"), r.FinishedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
        r.FinishedAt is { } f ? Math.Round((f - r.StartedAt).TotalSeconds, 1) : null,
        r.FromCache, r.RawRows, r.Malformed, r.Inserted, r.Updated, r.Unchanged, r.Error);
}
