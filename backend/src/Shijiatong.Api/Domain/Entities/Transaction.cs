namespace Shijiatong.Api.Domain.Entities;

/// <summary>
/// 單一物件成交紀錄。資料來源：內政部實價登錄開放資料（台中不動產買賣），
/// 由 lvr_staging_records 經正規化重建（見 Features/LvrImport/LvrTransformService）。
/// </summary>
public class Transaction
{
    public string Id { get; set; } = default!;        // 官方編號，如 RPQNMLMLRHLGFAB97DA
    public string? Community { get; set; }            // 社區名：買賣資料無此欄位 → null（前端改顯示路名+樓層）
    public string DistrictId { get; set; } = default!;// FK -> District.Id
    public District? District { get; set; }
    public string Road { get; set; } = default!;      // 路段（由門牌抽取；抽不到為空字串）
    public string? Address { get; set; }              // 去識別化門牌（如 臺中市西區三民路一段50巷8號10樓之36）
    public string? Section { get; set; }              // 重劃區 / 生活圈：資料無此欄位 → null
    public string Type { get; set; } = default!;      // 住宅大樓 / 華廈 / 公寓 / 透天厝 / 套房
    public int Total { get; set; }                    // 總價（萬）
    public decimal Unit { get; set; }                 // 單價（萬/坪，官方欄位，已扣除單獨計價車位）
    public decimal Ping { get; set; }                 // 建物移轉總面積（坪，含車位）
    public string Floor { get; set; } = default!;     // 18/26、全/4、1-2/5
    public int? Age { get; set; }                     // 屋齡；建築完成年月空白時為 null
    public string Layout { get; set; } = default!;    // 4房2廳
    public int Rooms { get; set; }
    public string Trade { get; set; } = default!;     // 成屋 / 預售屋（= 預售屋交屋，Date 為簽約日）
    public DateOnly Date { get; set; }                // 交易日
    public string Source { get; set; } = default!;    // 內政部
    public DateTime CrawledAt { get; set; }           // 取得時間（UTC，= staging 最近一次寫入）
    public double Lat { get; set; }                   // 目前為行政區中心點（約略）
    public double Lng { get; set; }
    public string Parking { get; set; } = default!;   // 坡道平面 / … / 無
    public bool IsOutlier { get; set; }               // 單價 < 5 或 > 200 萬/坪：保留顯示，統計排除
}
