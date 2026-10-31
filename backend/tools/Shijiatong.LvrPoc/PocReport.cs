using System.Globalization;
using System.Text;
using Shijiatong.Lvr;

namespace Shijiatong.LvrPoc;

/// <summary>單一來源檔的解析 + 正規化結果。</summary>
public sealed class SourceResult
{
    public required string Label { get; init; }
    public required string Origin { get; init; }
    public string? UnavailableReason { get; init; }
    public int RawRows { get; init; }
    public int MalformedRows { get; init; }
    public IReadOnlyList<LvrTransaction> Kept { get; init; } = [];
    public IReadOnlyDictionary<LvrExclusion, int> Excluded { get; init; } = new Dictionary<LvrExclusion, int>();
    public IReadOnlyList<string> AllSerials { get; init; } = [];

    public static SourceResult Missing(string label, string why) => new() { Label = label, Origin = "下載", UnavailableReason = why };

    public static SourceResult Load(string label, string path, string origin)
    {
        using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var csv = LvrCsv.Read(reader);
        var kept = new List<LvrTransaction>();
        var excluded = Enum.GetValues<LvrExclusion>().ToDictionary(e => e, _ => 0);
        foreach (var rec in csv.Records)
        {
            var r = LvrNormalizer.Normalize(rec);
            if (r.Value is { } t) kept.Add(t);
            else excluded[r.Excluded!.Value]++;
        }
        return new SourceResult
        {
            Label = label,
            Origin = origin,
            RawRows = csv.Records.Count + csv.MalformedRows,
            MalformedRows = csv.MalformedRows,
            Kept = kept,
            Excluded = excluded,
            AllSerials = csv.Records.Select(x => x[LvrColumns.SerialNo]).ToList(),
        };
    }
}

/// <summary>PoC 統計報告（Markdown）。</summary>
public static class PocReport
{
    // 目前 DbSeeder 種子的 12 個行政區（判斷真實資料有多少落在現有 districts 之外）
    private static readonly HashSet<string> SeededDistricts =
        ["西屯區", "北屯區", "南屯區", "西區", "北區", "東區", "南區", "中區", "大里區", "太平區", "豐原區", "烏日區"];

    public static string Build(IReadOnlyList<SourceResult> sources, DateTime generatedUtc)
    {
        var sb = new StringBuilder();
        void L(string s = "") => sb.AppendLine(s);

        L("# 實價登錄開放資料 PoC 報告（台中市・不動產買賣）");
        L();
        L($"- 產生時間：{generatedUtc.AddHours(8):yyyy-MM-dd HH:mm}（台北）");
        L($"- 來源：內政部不動產交易實價查詢服務網 開放資料 `b_lvr_land_a.csv`");
        L();

        // ── 1. 來源與篩選 ──
        L("## 1. 來源檔與篩選結果");
        L();
        var reasons = Enum.GetValues<LvrExclusion>();
        L("| 檔案 | 取得 | 原始筆數 | 格式錯誤 | **保留** | " + string.Join(" | ", reasons.Select(ReasonLabel)) + " |");
        L("|---|---|--:|--:|--:|" + string.Concat(reasons.Select(_ => "--:|")));
        foreach (var s in sources)
        {
            if (s.UnavailableReason is not null) { L($"| {s.Label} | ✗ {s.UnavailableReason} | – | – | – |" + string.Concat(reasons.Select(_ => " – |"))); continue; }
            L($"| {s.Label} | {s.Origin} | {s.RawRows:N0} | {s.MalformedRows} | **{s.Kept.Count:N0}** ({Pct(s.Kept.Count, s.RawRows)}) | "
              + string.Join(" | ", reasons.Select(r => $"{s.Excluded[r]:N0}")) + " |");
        }
        var loaded = sources.Where(s => s.UnavailableReason is null).ToList();
        var rawTotal = loaded.Sum(s => s.RawRows);
        var keptTotal = loaded.Sum(s => s.Kept.Count);
        L($"| **合計** | | {rawTotal:N0} | {loaded.Sum(s => s.MalformedRows)} | **{keptTotal:N0}** ({Pct(keptTotal, rawTotal)}) | "
          + string.Join(" | ", reasons.Select(r => $"{loaded.Sum(s => s.Excluded[r]):N0}")) + " |");
        L();
        L("排除規則（依序，只記第一個命中）：");
        L("- **非房地**：交易標的為純土地 / 純車位 / 單獨建物");
        L("- **特殊關係**：備註含「親友、員工、共有人或其他特殊關係」");
        L("- **非市場**：備註含政府機關標讓售、協議價購、地清或未辦繼承標售、法拍、僅車位交易");
        L("- **非住宅**：建物型態不是 住宅大樓 / 華廈 / 公寓 / 透天厝 / 套房（店面、辦公、廠房、其他）");
        L("- **欄位無效**：交易日、總價或建物面積無法解析");
        L();
        if (loaded.Count == 0) return sb.ToString();

        // ── 2. 跨檔重複 ──
        L("## 2. 跨檔「編號」重複（判斷能否用編號做重跑不重複寫入的鍵）");
        L();
        var serialCounts = loaded.SelectMany(s => s.AllSerials.Distinct()).GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
        var inFileDup = loaded.Sum(s => s.AllSerials.Count - s.AllSerials.Distinct().Count());
        L($"- 原始列合計 {loaded.Sum(s => s.AllSerials.Count):N0}，不重複編號 {serialCounts.Count:N0}");
        L($"- 單檔內重複：{inFileDup:N0} 筆");
        L($"- 出現在 2 個以上檔案的編號：{serialCounts.Count(kv => kv.Value > 1):N0} 個");
        if (loaded.Count > 1)
        {
            L();
            L("| 檔案 A | 檔案 B | 重疊編號 |");
            L("|---|---|--:|");
            for (var i = 0; i < loaded.Count; i++)
                for (var j = i + 1; j < loaded.Count; j++)
                {
                    var overlap = loaded[i].AllSerials.Intersect(loaded[j].AllSerials).Count();
                    L($"| {loaded[i].Label} | {loaded[j].Label} | {overlap:N0} |");
                }
        }
        L();

        // 去重：同編號以後面的檔案為準
        var kept = new Dictionary<string, LvrTransaction>();
        foreach (var s in loaded) foreach (var t in s.Kept) kept[t.SerialNo] = t;
        var all = kept.Values.ToList();
        if (all.Count == 0) return sb.ToString();

        // ── 3. 成交日分布 ──
        L($"## 3. 保留資料概況（去重後 {all.Count:N0} 筆）");
        L();
        L("### 3.1 成交日分布（季別是「發布」季，成交日會更早）");
        L();
        L("| 檔案 | 最早 | P5 | 中位數 | 最晚 | 成交日早於發布季 1 年以上 |");
        L("|---|---|---|---|---|--:|");
        foreach (var s in loaded.Where(s => s.Kept.Count > 0))
        {
            var dates = s.Kept.Select(t => t.TradeDate).Order().ToList();
            var cutoff = dates[^1].AddYears(-1);
            L($"| {s.Label} | {dates[0]:yyyy-MM-dd} | {Quantile(dates, 0.05):yyyy-MM-dd} | {Quantile(dates, 0.5):yyyy-MM-dd} | {dates[^1]:yyyy-MM-dd} | {dates.Count(d => d < cutoff):N0} |");
        }
        L();
        L("依備註「預售屋、或土地及建物分件登記案件」拆開（交易日 = 簽約日，交屋登記時才進買賣檔）：");
        L();
        L("| 類別 | 筆數 | 成交日中位數 | 屋齡 0 比例 | 成交日 > 1 年前（相對所屬檔最晚成交日） |");
        L("|---|--:|---|--:|--:|");
        var latestBySerial = loaded.SelectMany(s => s.Kept.Select(t => (t.SerialNo, Latest: s.Kept.Max(x => x.TradeDate))))
            .GroupBy(x => x.SerialNo).ToDictionary(g => g.Key, g => g.Last().Latest);
        foreach (var (name, group) in new[]
                 {
                     ("含預售/分件登記備註", all.Where(t => t.IsPresaleTransfer).ToList()),
                     ("一般成屋", all.Where(t => !t.IsPresaleTransfer).ToList()),
                 })
        {
            if (group.Count == 0) continue;
            var dates = group.Select(t => t.TradeDate).Order().ToList();
            var old = group.Count(t => t.TradeDate < latestBySerial[t.SerialNo].AddYears(-1));
            L($"| {name} | {group.Count:N0} ({Pct(group.Count, all.Count)}) | {Quantile(dates, 0.5):yyyy-MM-dd} | {Pct(group.Count(t => t.AgeYears == 0), group.Count)} | {Pct(old, group.Count)} |");
        }
        L();

        // ── 3.2 行政區 ──
        L("### 3.2 各行政區");
        L();
        var byDistrict = all.GroupBy(t => t.District).OrderByDescending(g => g.Count()).ToList();
        L($"共 {byDistrict.Count} 區；現有種子 12 區涵蓋 {Pct(all.Count(t => SeededDistricts.Contains(t.District)), all.Count)} 的筆數。");
        L();
        L("| 行政區 | 筆數 | 單價中位數（萬/坪） | 總價中位數（萬） | 坪數中位數 | 在種子 12 區 |");
        L("|---|--:|--:|--:|--:|:-:|");
        foreach (var g in byDistrict)
            L($"| {g.Key} | {g.Count():N0} | {Median(g.Select(t => t.Unit)):0.0} | {Median(g.Select(t => (decimal)t.Total)):N0} | {Median(g.Select(t => t.Ping)):0.0} | {(SeededDistricts.Contains(g.Key) ? "✓" : "")} |");
        L();

        // ── 3.3 型態 ──
        L("### 3.3 建物型態");
        L();
        L("| 型態 | 筆數 | 單價中位數（萬/坪） |");
        L("|---|--:|--:|");
        foreach (var g in all.GroupBy(t => t.BuildingType).OrderByDescending(g => g.Count()))
            L($"| {g.Key} | {g.Count():N0} | {Median(g.Select(t => t.Unit)):0.0} |");
        L();

        // ── 3.4 欄位完整度 ──
        L("### 3.4 欄位完整度 / 解析成功率");
        L();
        L("| 項目 | 成功 | 比例 | 說明 |");
        L("|---|--:|--:|---|");
        Row("路名抽取", all.Count(t => t.Road is not null), "門牌 → 路/街/大道（含段）");
        Row("屋齡可算", all.Count(t => t.AgeYears is not null), "建築完成年月空白多為預售/分件登記");
        Row("樓層可解析", all.Count(t => t.Floor is not null || t.FloorLabel.StartsWith("全")), "含透天「全」");
        Row("總樓層可解析", all.Count(t => t.TotalFloors is not null), "");
        Row("有房數（房 > 0）", all.Count(t => t.Rooms > 0), "0 房多為開放格局/店面型住宅");
        Row("有車位", all.Count(t => t.ParkingType is not null), "");
        Row("官方單價有值", all.Count(t => !t.UnitDerived), "空白時以 (總價-車位價)/(面積-車位面積) 推算");
        Row("社區名", 0, "**買賣檔無此欄位**（預售屋檔才有建案名稱）");
        Row("經緯度", 0, "**無此欄位**，門牌已去識別化為區間");
        L();

        // ── 3.5 單價 ──
        L("### 3.5 單價分布與異常值（萬/坪，官方單價已扣除單獨計價的車位）");
        L();
        var units = all.Select(t => t.Unit).Order().ToList();
        L($"- 最小 {units[0]:0.00}｜P1 {Quantile(units, 0.01):0.00}｜P50 {Quantile(units, 0.5):0.00}｜P99 {Quantile(units, 0.99):0.00}｜最大 {units[^1]:0.00}");
        L($"- 疑似異常：< {LvrNormalizer.MinReasonableUnit} 萬/坪 {units.Count(u => u < LvrNormalizer.MinReasonableUnit):N0} 筆；> {LvrNormalizer.MaxReasonableUnit} 萬/坪 {units.Count(u => u > LvrNormalizer.MaxReasonableUnit):N0} 筆");
        var pricedParking = all.Where(t => t.ParkingPriceNtd > 0 && t.Ping > 0).ToList();
        var parkingIncluded = all.Count(t => t.ParkingType is not null && t.ParkingPriceNtd == 0);
        L($"- 有車位 {all.Count(t => t.ParkingType is not null):N0} 筆：車位單獨計價 {pricedParking.Count:N0} 筆、車位價併入總價（官方單價無法扣除）{parkingIncluded:N0} 筆");
        if (pricedParking.Count > 0)
        {
            var diff = pricedParking.Select(t => (t.Total / t.Ping - t.Unit) / t.Unit).Order().ToList();
            L($"- 車位單獨計價者，若照 mock 算法「總價 ÷ 坪數」（含車位）會與官方單價差：中位數 {Quantile(diff, 0.5):P1}、P10 {Quantile(diff, 0.1):P1}、P90 {Quantile(diff, 0.9):P1} → **單價應直接用官方欄位**");
        }
        L();

        // ── 3.6 備註 ──
        L("### 3.6 保留資料中常見備註（未排除，但前端可能要標示）");
        L();
        L("| 備註 | 筆數 |");
        L("|---|--:|");
        foreach (var g in all.SelectMany(t => t.Notes).GroupBy(n => n.Length > 30 ? n[..30] + "…" : n).OrderByDescending(g => g.Count()).Take(12))
            L($"| {g.Key} | {g.Count():N0} |");
        L();

        // ── 4. 樣本 ──
        L("## 4. 正規化樣本（前端欄位格式）");
        L();
        L("| 編號 | 行政區 | 路名 | 型態 | 總價(萬) | 單價(萬/坪) | 坪數 | 樓層 | 屋齡 | 格局 | 車位 | 成交日 |");
        L("|---|---|---|---|--:|--:|--:|---|--:|---|---|---|");
        foreach (var t in all.OrderBy(t => t.SerialNo, StringComparer.Ordinal).Where((_, i) => i % Math.Max(1, all.Count / 8) == 0).Take(8))
            L($"| {t.SerialNo} | {t.District} | {t.Road ?? "?"} | {t.BuildingType} | {t.Total:N0} | {t.Unit:0.0} | {t.Ping:0.0} | {t.FloorLabel} | {t.AgeYears?.ToString() ?? "?"} | {t.Layout} | {t.ParkingType ?? "—"} | {t.TradeDate:yyyy-MM-dd} |");
        L();

        return sb.ToString();

        void Row(string name, int ok, string note) =>
            L($"| {name} | {ok:N0} | {Pct(ok, all.Count)} | {note} |");
    }

    private static string ReasonLabel(LvrExclusion r) => r switch
    {
        LvrExclusion.NotBuildingTarget => "非房地",
        LvrExclusion.SpecialRelation => "特殊關係",
        LvrExclusion.NonMarketSale => "非市場",
        LvrExclusion.NonResidentialType => "非住宅",
        LvrExclusion.InvalidData => "欄位無效",
        _ => r.ToString(),
    };

    private static string Pct(int part, int whole) =>
        whole == 0 ? "–" : ((double)part / whole).ToString("P1", CultureInfo.InvariantCulture);

    private static decimal Median(IEnumerable<decimal> xs) => Quantile(xs.Order().ToList(), 0.5);

    private static T Quantile<T>(IReadOnlyList<T> sorted, double q) =>
        sorted[(int)Math.Clamp(Math.Round(q * (sorted.Count - 1)), 0, sorted.Count - 1)];
}
