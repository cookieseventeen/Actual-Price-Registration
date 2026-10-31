using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Shijiatong.Lvr;

/// <summary>開放資料欄位格式的純函式解析（民國日期、中文樓層、單位換算…）。</summary>
public static partial class LvrParsing
{
    /// <summary>1 平方公尺 = 0.3025 坪。</summary>
    public const decimal PingPerSquareMeter = 0.3025m;

    /// <summary>民國年月日（7 碼 1150904；民國 100 年前為 6 碼 991231）→ 西元日期。</summary>
    public static DateOnly? ParseRocDate(string? s)
    {
        s = s?.Trim();
        if (string.IsNullOrEmpty(s) || s.Length is < 6 or > 7 || !s.All(char.IsAsciiDigit)) return null;
        var y = int.Parse(s[..^4], CultureInfo.InvariantCulture) + 1911;
        var m = int.Parse(s[^4..^2], CultureInfo.InvariantCulture);
        var d = int.Parse(s[^2..], CultureInfo.InvariantCulture);
        if (m is < 1 or > 12) return null;
        // 少數資料日為 00（只知年月）→ 取該月 1 日
        if (d == 0) d = 1;
        if (d > DateTime.DaysInMonth(y, m)) return null;
        return new DateOnly(y, m, d);
    }

    /// <summary>
    /// 建築完成年月：通常為 7 碼民國年月日（0710216）；
    /// 也見 5 碼民國年月（06205 = 62 年 05 月）。無法解析回 null。
    /// </summary>
    public static DateOnly? ParseRocCompletion(string? s)
    {
        s = s?.Trim();
        if (string.IsNullOrEmpty(s) || !s.All(char.IsAsciiDigit)) return null;
        if (s.Length == 7) return ParseRocDate(s);
        if (s.Length == 5)
        {
            var y = int.Parse(s[..3], CultureInfo.InvariantCulture) + 1911;
            var m = int.Parse(s[3..], CultureInfo.InvariantCulture);
            return m is >= 1 and <= 12 ? new DateOnly(y, m, 1) : null;
        }
        return null;
    }

    /// <summary>屋齡（足歲）：交易日 - 建築完成日；完成日晚於交易日（預售/新成屋）回 0。</summary>
    public static int? AgeYears(DateOnly tradeDate, DateOnly? completedOn)
    {
        if (completedOn is not { } c) return null;
        var years = tradeDate.Year - c.Year;
        if (tradeDate < c.AddYears(years)) years--;
        return Math.Max(0, years);
    }

    /// <summary>
    /// 中文數字（一～九十九、含「百」）→ 整數。例：十 → 10、二十四 → 24、一百零一 → 101。
    /// 也接受阿拉伯/全形數字。無法解析回 null。
    /// </summary>
    public static int? ParseChineseNumber(string? s)
    {
        s = NormalizeDigits(s?.Trim() ?? "");
        if (s.Length == 0) return null;
        if (s.All(char.IsAsciiDigit)) return int.Parse(s, CultureInfo.InvariantCulture);

        int total = 0, current = 0;
        foreach (var ch in s)
        {
            var digit = "零一二三四五六七八九".IndexOf(ch);
            if (ch == '兩') digit = 2;
            if (digit >= 0) { current = digit; continue; }
            var unit = ch switch { '十' => 10, '百' => 100, _ => 0 };
            if (unit == 0) return null;
            total += (current == 0 ? 1 : current) * unit;
            current = 0;
        }
        return total + current;
    }

    /// <summary>
    /// 移轉層次 / 總樓層數 → 樓層數字清單。
    /// 「十八層」→ [18]；「地下一層」→ [-1]；「地下層」→ [-1]；「一層，二層，騎樓」→ [1,2]（非樓層字樣略過）；
    /// 「全」或空白 → []。
    /// </summary>
    public static IReadOnlyList<int> ParseFloors(string? s)
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(s)) return result;
        foreach (var raw in s.Split(['，', ',', '、'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!raw.EndsWith('層')) continue;      // 騎樓、夾層、見其他登記事項…
            var body = raw[..^1];
            var basement = body.StartsWith("地下", StringComparison.Ordinal);
            if (basement) body = body[2..];
            if (body.Length == 0) { if (basement) result.Add(-1); continue; }
            if (ParseChineseNumber(body) is { } n && n > 0) result.Add(basement ? -n : n);
        }
        return result;
    }

    /// <summary>全形數字 ０-９ → 半形。</summary>
    public static string NormalizeDigits(string s)
    {
        if (!s.Any(ch => ch is >= '０' and <= '９')) return s;
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s) sb.Append(ch is >= '０' and <= '９' ? (char)('0' + (ch - '０')) : ch);
        return sb.ToString();
    }

    public static decimal ParseDecimal(string? s) =>
        decimal.TryParse(s?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0m;

    public static long ParseLong(string? s) =>
        long.TryParse(s?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0L;

    public static int ParseInt(string? s) =>
        int.TryParse(s?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

    /// <summary>平方公尺 → 坪（四捨五入至 0.01）。</summary>
    public static decimal SquareMetersToPing(decimal sqm) => Math.Round(sqm * PingPerSquareMeter, 2);

    /// <summary>元/平方公尺 → 萬/坪（四捨五入至 0.01）。</summary>
    public static decimal NtdPerSqmToWanPerPing(decimal ntdPerSqm) =>
        Math.Round(ntdPerSqm / PingPerSquareMeter / 10000m, 2);

    /// <summary>
    /// 從門牌抽出路段：「臺中市西區三民路一段５０巷８號」→「三民路一段」；
    /// 「臺中市沙鹿區臺灣大道七段７３７號」→「臺灣大道七段」。抽不到回 null（地號、段名等）。
    /// </summary>
    public static string? ExtractRoad(string? address, string? district)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        var s = NormalizeDigits(address.Trim());
        foreach (var prefix in new[] { "臺中市", "台中市" })
            if (s.StartsWith(prefix, StringComparison.Ordinal)) { s = s[prefix.Length..]; break; }
        if (!string.IsNullOrEmpty(district) && s.StartsWith(district, StringComparison.Ordinal)) s = s[district.Length..];
        s = VillagePrefixRegex().Replace(s, "");
        var m = RoadRegex().Match(s);
        return m.Success ? m.Value : null;
    }

    // 門牌前的「○○里」「12鄰」前綴（「大里路」這種路名不可誤刪，故里後不得緊接路/街）。
    [GeneratedRegex(@"^(?:[^\d路街]{1,5}里(?![路街]))?(?:\d+鄰)?")]
    private static partial Regex VillagePrefixRegex();

    // 路名以 路/街/大道 結尾，後接可選「X段」。
    [GeneratedRegex(@"^[^\d\s]{1,12}?(?:大道|路|街)(?:[一二三四五六七八九十]+段)?")]
    private static partial Regex RoadRegex();
}
