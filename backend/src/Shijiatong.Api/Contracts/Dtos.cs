using Shijiatong.Api.Domain.Entities;

namespace Shijiatong.Api.Contracts;

public record DistrictDto(string Id, string Name, string Zone, decimal Avg, decimal Change, int Vol)
{
    public static DistrictDto From(District d) => new(d.Id, d.Name, d.Zone, d.AvgUnitPrice, d.ChangePct, d.Volume);
}

public record TransactionDto(
    string Id, string Community, string District, string Road, string Section, string Type,
    int Total, decimal Unit, decimal Ping, string Floor, int Age, string Layout, int Rooms,
    string Trade, string Date, string Source, string Crawled, double Lat, double Lng, string Parking)
{
    public static TransactionDto From(Transaction t) => new(
        t.Id, t.Community, t.District?.Name ?? t.DistrictId, t.Road, t.Section, t.Type,
        t.Total, t.Unit, t.Ping, t.Floor, t.Age, t.Layout, t.Rooms, t.Trade,
        t.Date.ToString("yyyy-MM-dd"), t.Source, t.CrawledAt.ToString("yyyy-MM-dd HH:mm"),
        t.Lat, t.Lng, t.Parking);
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
