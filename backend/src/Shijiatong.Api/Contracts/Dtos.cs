using Shijiatong.Api.Domain.Entities;

namespace Shijiatong.Api.Contracts;

/// <remarks>change 為 null 代表前 12 個月樣本不足（需回補更早季別），不是 0%。</remarks>
public record DistrictDto(
    string Id, string Name, string? Zone, decimal Avg, decimal? Change, int Vol, double Lat, double Lng)
{
    public static DistrictDto From(District d) =>
        new(d.Id, d.Name, d.Zone, d.AvgUnitPrice, d.ChangePct, d.Volume, d.Lat, d.Lng);
}

/// <remarks>
/// 真實資料後：community / section 多為 null（前端改顯示「路名 + 樓層」）、age 可能為 null、
/// trade 為「預售屋」時 date 是簽約日、isOutlier 為 true 時統計已排除、lat/lng 目前為行政區中心點。
/// </remarks>
public record TransactionDto(
    string Id, string? Community, string District, string Road, string? Address, string? Section, string Type,
    int Total, decimal Unit, decimal Ping, string Floor, int? Age, string Layout, int Rooms,
    string Trade, string Date, string Source, string Crawled, double Lat, double Lng, string Parking,
    bool IsOutlier)
{
    public static TransactionDto From(Transaction t) => new(
        t.Id, t.Community, t.District?.Name ?? t.DistrictId, t.Road, t.Address, t.Section, t.Type,
        t.Total, t.Unit, t.Ping, t.Floor, t.Age, t.Layout, t.Rooms, t.Trade,
        t.Date.ToString("yyyy-MM-dd"), t.Source, t.CrawledAt.ToString("yyyy-MM-dd HH:mm"),
        t.Lat, t.Lng, t.Parking, t.IsOutlier);
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public record MemberDto(
    string Id, string Name, string Email, string Avatar, string Provider, string Plan,
    string Status, string Purpose, string CreatedAt, string? ReviewedAt, string? Note)
{
    public static MemberDto From(Member m) => new(
        m.Id.ToString(), m.Name, m.Email, m.Avatar,
        m.Provider.ToString().ToLowerInvariant(), m.Plan.ToString().ToLowerInvariant(),
        m.Status.ToString().ToLowerInvariant(), m.Purpose,
        m.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
        m.ReviewedAt?.ToString("yyyy-MM-dd HH:mm"), m.Note);
}

public record CrawlTaskDto(
    string Id, string District, string Status, int Records, string LastRun, string Next, string Duration)
{
    public static CrawlTaskDto From(CrawlTask c) => new(
        c.Id, c.DistrictName, c.Status.ToString().ToLowerInvariant(), c.Records,
        c.LastRun?.ToString("yyyy-MM-dd HH:mm") ?? "—",
        c.Next?.ToString("yyyy-MM-dd HH:mm") ?? "—", c.Duration);
}
