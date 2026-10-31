using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Shijiatong.Api.Domain.Entities;
using Shijiatong.Api.Features.Districts;
using Shijiatong.Api.Infrastructure;
using Shijiatong.Lvr;

namespace Shijiatong.Api.Features.LvrImport;

public sealed record LvrRebuildResult(
    int StagingRows,
    int Kept,
    IReadOnlyDictionary<string, int> Excluded,
    int UnknownDistrict,
    int OutOfRange,
    int Outliers,
    int PresaleTransfers,
    int DistrictsWithTransactions,
    string? StatsWindow,
    double DurationSeconds);

/// <summary>
/// 第 3 段：lvr_staging_records（台中買賣）→ 正規化 → <b>全量取代</b> transactions，並重算 districts 統計。
/// 全量重建（約 3 萬列、數秒）比增量簡單且必然一致；刪除與寫入在同一交易，失敗整批回滾。
/// </summary>
public sealed class LvrTransformService(AppDbContext db, IDistributedCache cache, ILogger<LvrTransformService> logger)
{
    public const string SourceName = "內政部";
    public const string TradeExisting = "成屋";
    public const string TradePresale = "預售屋";

    /// <summary>前 12 個月少於此筆數時不計算年變化（避免小樣本跳動）。</summary>
    internal const int MinPriorCountForChange = 10;

    /// <summary>
    /// 前 12 個月筆數至少要達近 12 個月的此比例，才視為兩期涵蓋度可比。
    /// 實測（2026-10，只匯入 115 年季別）：西屯近 12 月 1,489 筆、前 12 月僅 50 筆且多為遲延登記，
    /// 硬算會得到 -23% 的假跌幅；回補 114 年季別後兩期涵蓋度才可比。
    /// </summary>
    internal const decimal MinPriorCoverageRatio = 0.5m;

    public async Task<LvrRebuildResult> RebuildAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var districtsByName = await db.Districts.ToDictionaryAsync(d => d.Name, ct);

        var excluded = Enum.GetValues<LvrExclusion>()
            .ToDictionary(e => JsonNamingPolicy.CamelCase.ConvertName(e.ToString()), _ => 0);
        int stagingRows = 0, unknownDistrict = 0, outOfRange = 0;
        var txs = new List<Transaction>();

        var staging = db.LvrStagingRecords.AsNoTracking()
            .Where(x => x.County == LvrSources.Taichung && x.Kind == LvrSources.Sales)
            .Select(x => new { x.Fields, x.UpdatedAt })
            .AsAsyncEnumerable();

        await foreach (var s in staging.WithCancellation(ct))
        {
            stagingRows++;
            var fields = JsonSerializer.Deserialize<Dictionary<string, string>>(s.Fields)!;
            var result = LvrNormalizer.Normalize(new LvrRawRecord(stagingRows, fields));
            if (result.Value is not { } t)
            {
                excluded[JsonNamingPolicy.CamelCase.ConvertName(result.Excluded!.Value.ToString())]++;
                continue;
            }
            if (!districtsByName.TryGetValue(t.District, out var district)) { unknownDistrict++; continue; }
            // 超出欄位精度 numeric(6,2) / numeric(7,2)：寫入會整批失敗，先擋掉
            if (t.Unit >= 10000m || t.Ping >= 100000m) { outOfRange++; continue; }

            txs.Add(Map(t, district, s.UpdatedAt));
        }

        var window = ComputeDistrictStats(districtsByName.Values, txs);

        await using (var dbTx = await db.Database.BeginTransactionAsync(ct))
        {
            await db.Transactions.ExecuteDeleteAsync(ct);
            db.Transactions.AddRange(txs);
            await db.SaveChangesAsync(ct);
            await dbTx.CommitAsync(ct);
        }
        db.ChangeTracker.Clear();
        await cache.RemoveAsync(DistrictEndpoints.ListCacheKey, ct);

        var res = new LvrRebuildResult(
            stagingRows, txs.Count, excluded, unknownDistrict, outOfRange,
            txs.Count(t => t.IsOutlier), txs.Count(t => t.Trade == TradePresale),
            txs.Select(t => t.DistrictId).Distinct().Count(), window,
            Math.Round(sw.Elapsed.TotalSeconds, 1));

        logger.LogInformation(
            "成交資料重建完成：staging {Staging} → transactions {Kept}（異常值 {Outliers}、預售屋交屋 {Presale}、未知行政區 {Unknown}），{Seconds}s",
            res.StagingRows, res.Kept, res.Outliers, res.PresaleTransfers, res.UnknownDistrict, res.DurationSeconds);
        return res;
    }

    private static Transaction Map(LvrTransaction t, District d, DateTime crawledAt) => new()
    {
        Id = t.SerialNo,
        Community = null,                 // 買賣資料無社區名
        DistrictId = d.Id,
        Road = t.Road ?? "",
        Address = t.Address,
        Section = null,                   // 資料無重劃區 / 生活圈
        Type = t.BuildingType,
        Total = t.Total,
        Unit = t.Unit,
        Ping = t.Ping,
        Floor = t.FloorLabel,
        Age = t.AgeYears,
        Layout = t.Layout,
        Rooms = t.Rooms,
        Trade = t.IsPresaleTransfer ? TradePresale : TradeExisting,
        Date = t.TradeDate,
        Source = SourceName,
        CrawledAt = DateTime.SpecifyKind(crawledAt, DateTimeKind.Utc),
        Lat = d.Lat,                      // 先用行政區中心點
        Lng = d.Lng,
        Parking = t.ParkingType ?? "無",
        IsOutlier = t.IsOutlier,
    };

    /// <summary>
    /// 以資料中最新交易日為基準：近 12 個月平均單價 / 筆數，與前 12 個月比較的變化 %。
    /// 只用一般成屋、排除異常值——預售屋交屋的交易日是簽約日，放進時間窗會把舊的新案價格混進前期。
    /// 前期樣本不足（筆數或涵蓋度）時變化為 null。回傳統計期間字串；無資料時全部歸零並回 null。
    /// </summary>
    internal static string? ComputeDistrictStats(IEnumerable<District> districts, IReadOnlyList<Transaction> txs)
    {
        var valid = txs.Where(t => !t.IsOutlier && t.Trade == TradeExisting).ToList();
        if (valid.Count == 0)
        {
            foreach (var d in districts) { d.AvgUnitPrice = 0; d.ChangePct = null; d.Volume = 0; }
            return null;
        }

        var latest = valid.Max(t => t.Date);
        var curStart = latest.AddYears(-1);    // 近 12 個月 = (curStart, latest]
        var prevStart = latest.AddYears(-2);   // 前 12 個月 = (prevStart, curStart]
        var byDistrict = valid.GroupBy(t => t.DistrictId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var d in districts)
        {
            var list = byDistrict.GetValueOrDefault(d.Id) ?? [];
            var cur = list.Where(t => t.Date > curStart).Select(t => t.Unit).ToList();
            var prev = list.Where(t => t.Date > prevStart && t.Date <= curStart).Select(t => t.Unit).ToList();

            d.Volume = cur.Count;
            d.AvgUnitPrice = cur.Count > 0 ? Math.Round(cur.Average(), 1) : 0;
            var comparable = cur.Count > 0
                && prev.Count >= MinPriorCountForChange
                && prev.Count >= cur.Count * MinPriorCoverageRatio;
            d.ChangePct = comparable
                ? Math.Clamp(Math.Round((cur.Average() - prev.Average()) / prev.Average() * 100, 1), -999m, 999m)
                : null;
        }
        return $"{curStart.AddDays(1):yyyy-MM-dd} ~ {latest:yyyy-MM-dd}";
    }
}
