namespace Shijiatong.Api.Domain.Entities;

/// <summary>台中市行政區與統計（統計由真實成交重建時計算，見 LvrTransformService）。</summary>
public class District
{
    public string Id { get; set; } = default!;       // e.g. "xitun"
    public string Name { get; set; } = default!;      // 西屯區
    public string? Zone { get; set; }                 // 熱門生活圈說明（七期 / 單元二）；無則 null
    public decimal AvgUnitPrice { get; set; }         // 近 12 個月平均單價（萬/坪，一般成屋、排除異常值）
    public decimal? ChangePct { get; set; }           // 近 12 個月 vs 前 12 個月（%）；前期樣本不足時 null
    public int Volume { get; set; }                   // 近 12 個月成交筆數（一般成屋）
    public double Lat { get; set; }                   // 行政區中心點（約略，區公所附近）
    public double Lng { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
