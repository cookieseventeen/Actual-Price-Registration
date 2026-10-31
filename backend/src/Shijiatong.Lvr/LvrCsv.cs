using System.Text;

namespace Shijiatong.Lvr;

/// <summary>開放資料 CSV 的一列原始資料（依中文欄名索引）。</summary>
public sealed class LvrRawRecord(int lineNo, IReadOnlyDictionary<string, string> fields)
{
    /// <summary>檔案中的資料列序號（1 起算，不含兩列表頭）。</summary>
    public int LineNo { get; } = lineNo;
    public IReadOnlyDictionary<string, string> Fields { get; } = fields;

    /// <summary>取欄位值（去頭尾空白）；欄位不存在回空字串。</summary>
    public string this[string column] => Fields.TryGetValue(column, out var v) ? v.Trim() : "";
}

public sealed record LvrCsvResult(
    IReadOnlyList<string> Header,
    IReadOnlyList<LvrRawRecord> Records,
    int MalformedRows);

/// <summary>
/// 讀取 plvr.land.moi.gov.tw 的 *_lvr_land_*.csv。
/// 格式：UTF-8（含 BOM）、第 1 列中文欄名、第 2 列英文欄名、之後為資料。
/// </summary>
public static class LvrCsv
{
    /// <summary>判斷內容是否為實價登錄 CSV（無資料的季別會回 HTML 錯誤頁）。</summary>
    public static bool LooksLikeLvrCsv(string firstLine) =>
        firstLine.TrimStart('﻿').StartsWith(LvrColumns.District + ",", StringComparison.Ordinal);

    public static LvrCsvResult Read(TextReader reader)
    {
        var rows = ParseRows(reader).GetEnumerator();
        if (!rows.MoveNext()) return new LvrCsvResult([], [], 0);

        var header = rows.Current.Select(h => h.Trim().TrimStart('﻿')).ToArray();
        if (header.Length == 0 || header[0] != LvrColumns.District)
            throw new FormatException($"不是實價登錄 CSV：第一欄為「{header.FirstOrDefault()}」，預期「{LvrColumns.District}」。");

        var records = new List<LvrRawRecord>();
        var malformed = 0;
        var dataLine = 0;
        var first = true;
        while (rows.MoveNext())
        {
            var row = rows.Current;
            // 第 2 列是英文欄名，略過
            if (first) { first = false; if (IsEnglishHeader(row)) continue; }
            if (row.Count == 1 && string.IsNullOrWhiteSpace(row[0])) continue; // 空行
            dataLine++;
            if (row.Count != header.Length) { malformed++; continue; }

            var dict = new Dictionary<string, string>(header.Length, StringComparer.Ordinal);
            for (var i = 0; i < header.Length; i++) dict[header[i]] = row[i];
            records.Add(new LvrRawRecord(dataLine, dict));
        }
        return new LvrCsvResult(header, records, malformed);
    }

    private static bool IsEnglishHeader(IReadOnlyList<string> row) =>
        row.Count > 0 && row[0].Contains("district", StringComparison.OrdinalIgnoreCase);

    /// <summary>RFC 4180 風格切欄：支援引號包覆、引號內逗號/換行、"" 跳脫。</summary>
    internal static IEnumerable<IReadOnlyList<string>> ParseRows(TextReader reader)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        var any = false;
        int c;
        while ((c = reader.Read()) != -1)
        {
            any = true;
            var ch = (char)c;
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (reader.Peek() == '"') { sb.Append('"'); reader.Read(); }
                    else inQuotes = false;
                }
                else sb.Append(ch);
                continue;
            }
            switch (ch)
            {
                case '"': inQuotes = true; break;
                case ',': fields.Add(sb.ToString()); sb.Clear(); break;
                case '\r':
                    if (reader.Peek() == '\n') reader.Read();
                    goto case '\n';
                case '\n':
                    fields.Add(sb.ToString()); sb.Clear();
                    yield return fields;
                    fields = new List<string>();
                    any = false;
                    break;
                default: sb.Append(ch); break;
            }
        }
        if (any)
        {
            fields.Add(sb.ToString());
            yield return fields;
        }
    }
}

/// <summary>a 檔（不動產買賣）用到的中文欄名。</summary>
public static class LvrColumns
{
    public const string District = "鄉鎮市區";
    public const string Target = "交易標的";
    public const string Address = "土地位置建物門牌";
    public const string LandArea = "土地移轉總面積平方公尺";
    public const string TradeDate = "交易年月日";
    public const string TradeUnits = "交易筆棟數";
    public const string Floor = "移轉層次";
    public const string TotalFloors = "總樓層數";
    public const string BuildingType = "建物型態";
    public const string MainUse = "主要用途";
    public const string CompletedOn = "建築完成年月";
    public const string BuildingArea = "建物移轉總面積平方公尺";
    public const string Rooms = "建物現況格局-房";
    public const string Halls = "建物現況格局-廳";
    public const string Baths = "建物現況格局-衛";
    public const string TotalPrice = "總價元";
    public const string UnitPrice = "單價元平方公尺";
    public const string ParkingType = "車位類別";
    public const string ParkingArea = "車位移轉總面積平方公尺";
    public const string ParkingPrice = "車位總價元";
    public const string Note = "備註";
    public const string SerialNo = "編號";
    public const string MainBuildingArea = "主建物面積";
    public const string Elevator = "電梯";
}
