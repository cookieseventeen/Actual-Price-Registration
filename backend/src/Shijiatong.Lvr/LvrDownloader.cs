using System.Text;
using System.Text.RegularExpressions;

namespace Shijiatong.Lvr;

/// <summary>一個要下載的開放資料檔。</summary>
public sealed record LvrFileRef(string Label, Uri Url, string CacheFileName);

/// <summary>
/// 內政部不動產交易實價查詢服務網「開放資料」下載位址。
/// 檔名規則：{縣市代碼}_lvr_land_{類別}.csv；台中市 = b；類別 a = 不動產買賣、b = 預售屋、c = 租賃。
/// </summary>
public static partial class LvrSources
{
    public const string BaseUrl = "https://plvr.land.moi.gov.tw/";
    public const string Taichung = "b";
    public const string Sales = "a";

    /// <summary>本期資料（官方每月 1、11、21 日更新）。快取檔名帶日期，同一天只抓一次。</summary>
    public static LvrFileRef Current(DateOnly today, string county = Taichung, string kind = Sales)
    {
        var file = FileName(county, kind);
        return new LvrFileRef($"本期({today:yyyy-MM-dd})", new Uri($"{BaseUrl}Download?fileName={file}"),
            $"current_{today:yyyyMMdd}_{file}");
    }

    /// <summary>歷史季別，例：115S2 = 民國 115 年第 2 季「發布」的資料（成交日可能更早）。</summary>
    public static LvrFileRef Season(string season, string county = Taichung, string kind = Sales)
    {
        season = season.Trim().ToUpperInvariant();
        if (!SeasonRegex().IsMatch(season)) throw new ArgumentException($"季別格式應為 115S2，收到「{season}」", nameof(season));
        var file = FileName(county, kind);
        return new LvrFileRef(season, new Uri($"{BaseUrl}DownloadSeason?season={season}&fileName={file}"),
            $"{season}_{file}");
    }

    private static string FileName(string county, string kind) => $"{county}_lvr_land_{kind}.csv";

    [GeneratedRegex(@"^\d{2,3}S[1-4]$")]
    private static partial Regex SeasonRegex();
}

public enum LvrDownloadStatus { Ok, Unavailable }

public sealed record LvrDownloadResult(LvrFileRef File, LvrDownloadStatus Status, string? LocalPath, bool FromCache, string? Detail);

/// <summary>
/// 禮貌下載：本機快取優先（季別檔不會變，抓過就不再抓）、請求間隔節流、明確 User-Agent。
/// 無資料的季別官方會回 HTML 頁，判為 Unavailable 且不寫入快取。
/// </summary>
public sealed class LvrDownloader(HttpClient http, string cacheDir, TimeSpan minInterval)
{
    public const string UserAgent = "ShijiatongLvrImport/0.1 (+https://shijiatong.flint-forge.com)";

    private DateTime _lastRequestUtc = DateTime.MinValue;

    public static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return client;
    }

    public async Task<LvrDownloadResult> GetAsync(LvrFileRef file, bool refresh = false, CancellationToken ct = default)
    {
        Directory.CreateDirectory(cacheDir);
        var path = Path.Combine(cacheDir, file.CacheFileName);
        if (!refresh && File.Exists(path)) return new(file, LvrDownloadStatus.Ok, path, FromCache: true, null);

        var wait = _lastRequestUtc + minInterval - DateTime.UtcNow;
        if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
        _lastRequestUtc = DateTime.UtcNow;

        using var resp = await http.GetAsync(file.Url, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode)
            return new(file, LvrDownloadStatus.Unavailable, null, false, $"HTTP {(int)resp.StatusCode}");

        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
        var firstLine = FirstLine(bytes);
        if (!LvrCsv.LooksLikeLvrCsv(firstLine))
            return new(file, LvrDownloadStatus.Unavailable, null, false,
                $"非 CSV 內容（{resp.Content.Headers.ContentType?.MediaType ?? "?"}，{bytes.Length} bytes）");

        // 先寫暫存檔再改名，避免中斷留下半個檔被當成快取
        var tmp = path + ".part";
        await File.WriteAllBytesAsync(tmp, bytes, ct);
        File.Move(tmp, path, overwrite: true);
        return new(file, LvrDownloadStatus.Ok, path, FromCache: false, null);
    }

    private static string FirstLine(byte[] bytes)
    {
        var len = Math.Min(bytes.Length, 512);
        var head = Encoding.UTF8.GetString(bytes, 0, len);
        var nl = head.IndexOfAny(['\r', '\n']);
        return nl >= 0 ? head[..nl] : head;
    }
}
