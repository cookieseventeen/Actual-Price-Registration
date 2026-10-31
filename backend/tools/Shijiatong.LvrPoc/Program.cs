using System.Text;
using Shijiatong.Lvr;
using Shijiatong.LvrPoc;

// 實價登錄開放資料 PoC：下載台中「不動產買賣」CSV → 解析 → 正規化 → 輸出統計報告（Markdown）。
// 不連 DB、不改任何現有資料。
//
// 用法：
//   dotnet run --project tools/Shijiatong.LvrPoc -- --season 115S1 --season 115S2 --current --out report.md
//   dotnet run --project tools/Shijiatong.LvrPoc -- --file ./b_lvr_land_a.csv
// 選項：
//   --season <115S2>   歷史季別（可重複）
//   --current          本期資料
//   --file <path>      直接讀本機 CSV（可重複，不經網路）
//   --cache-dir <dir>  下載快取目錄（預設 ./.lvr-cache）
//   --refresh          忽略快取重新下載
//   --interval <秒>    兩次下載最小間隔（預設 3）
//   --out <path>       報告輸出檔（預設只印到 stdout）

Console.OutputEncoding = Encoding.UTF8;

var seasons = new List<string>();
var files = new List<string>();
var current = false;
var cacheDir = ".lvr-cache";
var refresh = false;
var interval = 3;
string? outPath = null;

for (var i = 0; i < args.Length; i++)
{
    string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} 需要參數值");
    switch (args[i])
    {
        case "--season": seasons.Add(Next()); break;
        case "--current": current = true; break;
        case "--file": files.Add(Next()); break;
        case "--cache-dir": cacheDir = Next(); break;
        case "--refresh": refresh = true; break;
        case "--interval": interval = int.Parse(Next()); break;
        case "--out": outPath = Next(); break;
        default:
            Console.Error.WriteLine($"未知參數：{args[i]}");
            return 2;
    }
}

if (seasons.Count == 0 && files.Count == 0 && !current)
{
    Console.Error.WriteLine("請至少指定 --season / --current / --file 其中之一。");
    return 2;
}

var sources = new List<SourceResult>();

// 本機檔
foreach (var f in files)
    sources.Add(SourceResult.Load(Path.GetFileName(f), f, "本機檔"));

// 網路下載（季別 + 本期）
if (seasons.Count > 0 || current)
{
    using var http = LvrDownloader.CreateHttpClient();
    var downloader = new LvrDownloader(http, cacheDir, TimeSpan.FromSeconds(interval));
    var refs = seasons.Select(s => LvrSources.Season(s)).ToList();
    if (current) refs.Add(LvrSources.Current(LvrSources.TaipeiToday(DateTime.UtcNow)));

    foreach (var r in refs)
    {
        Console.Error.WriteLine($"[下載] {r.Label} ← {r.Url}");
        var res = await downloader.GetAsync(r, refresh);
        if (res.Status != LvrDownloadStatus.Ok)
        {
            Console.Error.WriteLine($"  → 無資料：{res.Detail}");
            sources.Add(SourceResult.Missing(r.Label, res.Detail ?? "無資料"));
            continue;
        }
        Console.Error.WriteLine($"  → {(res.FromCache ? "使用快取" : "已下載")}：{res.LocalPath}");
        sources.Add(SourceResult.Load(r.Label, res.LocalPath!, res.FromCache ? "快取" : "下載"));
    }
}

var report = PocReport.Build(sources, DateTime.UtcNow);
Console.WriteLine(report);
if (outPath is not null)
{
    await File.WriteAllTextAsync(outPath, report, new UTF8Encoding(false));
    Console.Error.WriteLine($"[報告] 已寫入 {outPath}");
}
return 0;
