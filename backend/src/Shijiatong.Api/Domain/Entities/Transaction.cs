namespace Shijiatong.Api.Domain.Entities;

/// <summary>單一物件成交紀錄，對應前端 Transaction。</summary>
public class Transaction
{
    public string Id { get; set; } = default!;        // T0001
    public string Community { get; set; } = default!; // 社區名
    public string DistrictId { get; set; } = default!;// FK -> District.Id
    public District? District { get; set; }
    public string Road { get; set; } = default!;
    public string Section { get; set; } = default!;   // 重劃區 / 生活圈
    public string Type { get; set; } = default!;      // 住宅大樓 / 華廈 ...
    public int Total { get; set; }                    // 總價（萬）
    public decimal Unit { get; set; }                 // 單價（萬/坪）
    public decimal Ping { get; set; }                 // 坪數
    public string Floor { get; set; } = default!;     // 18/26
    public int Age { get; set; }                      // 屋齡
    public string Layout { get; set; } = default!;    // 4房2廳
    public int Rooms { get; set; }
    public string Trade { get; set; } = default!;     // 成屋 / 預售屋
    public DateOnly Date { get; set; }                // 成交日
    public string Source { get; set; } = default!;    // 內政部
    public DateTime CrawledAt { get; set; }           // 爬取時間（UTC）
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Parking { get; set; } = default!;
}
