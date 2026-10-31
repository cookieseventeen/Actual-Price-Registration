namespace Shijiatong.Api.Domain.Entities;

/// <summary>
/// 實價登錄開放資料原始列（staging）。原欄位完整保留為 jsonb，以官方「編號」為鍵 →
/// 重跑匯入不會重複；內容有更正時以 <see cref="ContentHash"/> 偵測並覆寫。
/// 尚未轉入 transactions（第 3 段）。
/// </summary>
public class LvrStagingRecord
{
    public string SerialNo { get; set; } = default!;     // 編號，如 RPQNMLMLRHLGFAB97DA
    public string County { get; set; } = default!;       // 縣市代碼：b = 台中
    public string Kind { get; set; } = default!;         // a = 不動產買賣
    public string District { get; set; } = default!;     // 鄉鎮市區（方便查詢，原值也在 Fields）
    public string Fields { get; set; } = default!;       // 原始欄位 JSON（中文欄名 → 字串值）
    public string ContentHash { get; set; } = default!;  // Fields 的 SHA-256（hex）
    public string FirstSource { get; set; } = default!;  // 首次出現的來源：115S3 / current-20261031
    public string LastSource { get; set; } = default!;   // 最近一次寫入（新增或內容變更）的來源
    public DateTime FirstImportedAt { get; set; }        // UTC
    public DateTime UpdatedAt { get; set; }              // UTC，最近一次內容變更
    public Guid LastRunId { get; set; }
}

public enum LvrImportStatus { Running, Succeeded, NoData, Failed }

/// <summary>一次匯入執行紀錄（手動觸發；之後排程也寫這裡）。</summary>
public class LvrImportRun
{
    public Guid Id { get; set; }
    public string Source { get; set; } = default!;       // 115S3 / current-20261031
    public string Url { get; set; } = default!;
    public LvrImportStatus Status { get; set; }
    public DateTime StartedAt { get; set; }              // UTC
    public DateTime? FinishedAt { get; set; }            // UTC
    public bool FromCache { get; set; }
    public int RawRows { get; set; }                     // 檔案資料列數（不含表頭）
    public int Malformed { get; set; }                   // 欄數不符或缺編號
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Unchanged { get; set; }
    public string? Error { get; set; }
}
