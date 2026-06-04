namespace Shijiatong.Api.Domain.Entities;

/// <summary>行政區（含重劃區 / 熱門生活圈）統計，對應前端 District。</summary>
public class District
{
    public string Id { get; set; } = default!;       // e.g. "xitun"
    public string Name { get; set; } = default!;      // 西屯區
    public string Zone { get; set; } = default!;      // 七期 / 單元二
    public decimal AvgUnitPrice { get; set; }         // 平均單價（萬/坪）
    public decimal ChangePct { get; set; }            // 年變化（%）
    public int Volume { get; set; }                   // 成交量

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
