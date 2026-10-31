using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shijiatong.Api.Domain.Entities;
using Shijiatong.Api.Infrastructure;
using Shijiatong.Lvr;

namespace Shijiatong.Api.Features.LvrImport;

public sealed class LvrImportOptions
{
    public const string SectionName = "Lvr";

    /// <summary>是否開放匯入端點（預設 false；prod 開 admin 保護前不要開）。</summary>
    public bool ImportEnabled { get; set; }

    /// <summary>下載快取目錄（容器內建議掛 volume）。</summary>
    public string CacheDir { get; set; } = ".lvr-cache";

    /// <summary>兩次下載最小間隔（秒）。</summary>
    public int MinIntervalSeconds { get; set; } = 3;
}

/// <summary>同一時間只允許一個匯入：下載節流狀態非執行緒安全，也避免同編號並行寫入。</summary>
public sealed class LvrImportGate
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
}

/// <summary>
/// 下載一個開放資料檔 → 原始列寫入 lvr_staging_records（以編號 upsert，內容雜湊不變則略過）。
/// 每次執行都記一筆 lvr_import_runs。資料寫入在單一 SaveChanges（交易）內，失敗整批不落地。
/// </summary>
public sealed class LvrImportService(AppDbContext db, ILvrFileSource source, ILogger<LvrImportService> logger)
{
    private const int LookupChunk = 2000;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 保留中文欄名可讀
    };

    public async Task<LvrImportRun> ImportAsync(LvrFileRef file, bool refresh, CancellationToken ct = default)
    {
        var run = new LvrImportRun
        {
            Id = Guid.NewGuid(),
            Source = file.Key,
            Url = file.Url.ToString(),
            Status = LvrImportStatus.Running,
            StartedAt = DateTime.UtcNow,
        };
        db.LvrImportRuns.Add(run);
        await db.SaveChangesAsync(ct);

        try
        {
            var dl = await source.GetAsync(file, refresh, ct);
            run.FromCache = dl.FromCache;
            if (dl.Status != LvrDownloadStatus.Ok)
            {
                run.Status = LvrImportStatus.NoData;
                run.Error = dl.Detail;
            }
            else
            {
                await UpsertAsync(run, file, dl.LocalPath!, ct);
                run.Status = LvrImportStatus.Succeeded;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "實價登錄匯入失敗：{Source}", file.Key);
            // 丟掉未寫入的 staging 變更，只更新本次 run 的失敗狀態
            db.ChangeTracker.Clear();
            db.LvrImportRuns.Update(run);
            run.Status = LvrImportStatus.Failed;
            run.Error = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
        }

        run.FinishedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(CancellationToken.None);
        logger.LogInformation(
            "實價登錄匯入 {Source} {Status}：新增 {Inserted}、更新 {Updated}、未變 {Unchanged}、格式錯誤 {Malformed}",
            run.Source, run.Status, run.Inserted, run.Updated, run.Unchanged, run.Malformed);
        return run;
    }

    private async Task UpsertAsync(LvrImportRun run, LvrFileRef file, string path, CancellationToken ct)
    {
        LvrCsvResult csv;
        using (var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            csv = LvrCsv.Read(reader);

        run.RawRows = csv.Records.Count + csv.MalformedRows;
        run.Malformed = csv.MalformedRows;

        // 同檔同編號以最後一列為準
        var incoming = new Dictionary<string, (LvrRawRecord Rec, string Json, string Hash)>(StringComparer.Ordinal);
        foreach (var rec in csv.Records)
        {
            var serial = rec[LvrColumns.SerialNo];
            if (serial.Length is 0 or > 32) { run.Malformed++; continue; }
            var json = ToJson(rec);
            incoming[serial] = (rec, json, Sha256Hex(json));
        }

        var existing = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var chunk in incoming.Keys.Chunk(LookupChunk))
        {
            var rows = await db.LvrStagingRecords.AsNoTracking()
                .Where(x => chunk.Contains(x.SerialNo))
                .Select(x => new { x.SerialNo, x.ContentHash })
                .ToListAsync(ct);
            foreach (var r in rows) existing[r.SerialNo] = r.ContentHash;
        }

        var now = DateTime.UtcNow;
        var changed = new List<string>();
        foreach (var (serial, item) in incoming)
        {
            if (!existing.TryGetValue(serial, out var hash))
            {
                db.LvrStagingRecords.Add(new LvrStagingRecord
                {
                    SerialNo = serial,
                    County = file.County,
                    Kind = file.Kind,
                    District = DistrictOf(item.Rec),
                    Fields = item.Json,
                    ContentHash = item.Hash,
                    FirstSource = file.Key,
                    LastSource = file.Key,
                    FirstImportedAt = now,
                    UpdatedAt = now,
                    LastRunId = run.Id,
                });
                run.Inserted++;
            }
            else if (hash != item.Hash) changed.Add(serial);
            else run.Unchanged++;
        }

        // 官方更正：內容不同才覆寫
        foreach (var chunk in changed.Chunk(LookupChunk))
        {
            var entities = await db.LvrStagingRecords.Where(x => chunk.Contains(x.SerialNo)).ToListAsync(ct);
            foreach (var e in entities)
            {
                var item = incoming[e.SerialNo];
                e.District = DistrictOf(item.Rec);
                e.Fields = item.Json;
                e.ContentHash = item.Hash;
                e.LastSource = file.Key;
                e.UpdatedAt = now;
                e.LastRunId = run.Id;
                run.Updated++;
            }
        }

        // staging 變更與 run 計數同一個 SaveChanges（EF 預設包交易）
        await db.SaveChangesAsync(ct);
    }

    private static string DistrictOf(LvrRawRecord rec)
    {
        var d = rec[LvrColumns.District];
        return d.Length > 16 ? d[..16] : d;
    }

    /// <summary>欄名排序後序列化，確保同內容同雜湊（不受欄位順序影響）。</summary>
    internal static string ToJson(LvrRawRecord rec)
    {
        var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (k, v) in rec.Fields) sorted[k] = v;
        return JsonSerializer.Serialize(sorted, JsonOpts);
    }

    private static string Sha256Hex(string s) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
}
