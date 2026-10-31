using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Shijiatong.Api.Domain;
using Shijiatong.Api.Domain.Entities;

namespace Shijiatong.Api.Infrastructure;

/// <summary>
/// 啟動時寫入基礎資料。冪等：資料已存在則略過。
/// 成交資料不再灌 demo：由實價登錄開放資料匯入後重建（/api/admin/lvr/import → /rebuild）。
/// 會員與爬蟲任務仍為 demo 種子（後者待第 4 段排程取代）。
/// </summary>
public static class DbSeeder
{
    private static DateTime Utc(string s) =>
        DateTime.SpecifyKind(DateTime.Parse(s, CultureInfo.InvariantCulture), DateTimeKind.Utc);

    public static async Task SeedAsync(AppDbContext db, IEnumerable<string> adminEmails)
    {
        await EnsureDistrictsAsync(db);

        if (!await db.Members.AnyAsync())
            db.Members.AddRange(Members());

        await EnsureAdminMembersAsync(db, adminEmails);

        if (!await db.CrawlTasks.AnyAsync())
            db.CrawlTasks.AddRange(CrawlTasks());

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// 補齊台中 29 區（參考資料見 <see cref="TaichungDistricts"/>）：缺的新增（統計為 0，待重建計算），
    /// 已存在的只同步名稱 / 生活圈 / 中心點，不動統計。
    /// </summary>
    private static async Task EnsureDistrictsAsync(AppDbContext db)
    {
        var existing = await db.Districts.ToDictionaryAsync(d => d.Id);
        foreach (var info in TaichungDistricts.All)
        {
            if (!existing.TryGetValue(info.Id, out var d))
            {
                db.Districts.Add(new District
                {
                    Id = info.Id, Name = info.Name, Zone = info.Zone, Lat = info.Lat, Lng = info.Lng,
                });
                continue;
            }
            d.Name = info.Name;
            d.Zone = info.Zone;
            d.Lat = info.Lat;
            d.Lng = info.Lng;
        }
    }

    private static Member M(string name, string email, string avatar, Provider provider, Plan plan,
        MemberStatus status, string purpose, string created, string? reviewed = null, string? note = null) => new()
    {
        Id = Guid.NewGuid(), Name = name, Email = email, Avatar = avatar, Provider = provider, Plan = plan,
        Status = status, Role = MemberRole.Member, Purpose = purpose, CreatedAt = Utc(created),
        ReviewedAt = reviewed is null ? null : Utc(reviewed), Note = note,
    };

    private static async Task EnsureAdminMembersAsync(AppDbContext db, IEnumerable<string> adminEmails)
    {
        foreach (var raw in adminEmails)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var email = raw.Trim();
            var existing = await db.Members.FirstOrDefaultAsync(m =>
                m.Email.ToLower() == email.ToLower());

            if (existing is null)
            {
                var local = email.Split('@')[0];
                db.Members.Add(new Member
                {
                    Name = local,
                    Email = email,
                    Avatar = local.Length > 0 ? local[..1] : "?",
                    Provider = Provider.Google,
                    Plan = Plan.Enterprise,
                    Status = MemberStatus.Active,
                    Role = MemberRole.Admin,
                    Purpose = "系統管理",
                    CreatedAt = DateTime.UtcNow,
                    ReviewedAt = DateTime.UtcNow,
                });
            }
            else if (existing.Role != MemberRole.Admin)
            {
                existing.Role = MemberRole.Admin;
            }
        }
    }

    private static Member[] Members() =>
    [
        M("陳冠宇", "kuanyu.chen@gmail.com",  "陳", Provider.Google, Plan.Pro,        MemberStatus.Active,    "不動產仲介", "2026-04-12 09:21", "2026-04-12 14:02"),
        M("林佳穎", "jiaying.lin@icloud.com", "林", Provider.Apple,  Plan.Free,       MemberStatus.Active,    "自住購屋",   "2026-04-20 18:44", "2026-04-21 10:11"),
        M("王志明", "zhiming.wang@gmail.com", "王", Provider.Google, Plan.Enterprise, MemberStatus.Active,    "估價/金融",  "2026-03-02 11:05", "2026-03-02 16:30"),
        M("張雅婷", "yating.chang@icloud.com","張", Provider.Apple,  Plan.Free,       MemberStatus.Pending,   "投資理財",   "2026-05-28 21:17"),
        M("黃建德", "jiande.huang@gmail.com", "黃", Provider.Google, Plan.Pro,        MemberStatus.Pending,   "不動產仲介", "2026-05-30 08:52"),
        M("吳秉澄", "bingcheng.wu@gmail.com", "吳", Provider.Google, Plan.Free,       MemberStatus.Suspended, "投資理財",   "2026-02-14 13:38", "2026-05-10 09:00", "異常大量匯出，暫停權限"),
        M("蔡宜真", "yizhen.tsai@icloud.com", "蔡", Provider.Apple,  Plan.Free,       MemberStatus.Rejected,  "學術研究",   "2026-05-22 16:09", "2026-05-23 09:45", "無法驗證使用單位"),
        M("劉冠廷", "kuanting.liu@gmail.com", "劉", Provider.Google, Plan.Pro,        MemberStatus.Active,    "估價/金融",  "2026-01-08 10:00", "2026-01-08 11:20"),
    ];

    private static CrawlTask[] CrawlTasks() =>
    [
        new() { Id = "C01", DistrictName = "西屯區", Status = CrawlStatus.Done,    Records = 1284, LastRun = Utc("2026-05-28 03:14"), Next = Utc("2026-05-29 03:00"), Duration = "4分12秒" },
        new() { Id = "C02", DistrictName = "北屯區", Status = CrawlStatus.Done,    Records = 1607, LastRun = Utc("2026-05-28 03:18"), Next = Utc("2026-05-29 03:00"), Duration = "5分02秒" },
        new() { Id = "C03", DistrictName = "南屯區", Status = CrawlStatus.Done,    Records = 1043, LastRun = Utc("2026-05-28 03:23"), Next = Utc("2026-05-29 03:00"), Duration = "3分48秒" },
        new() { Id = "C04", DistrictName = "西區",   Status = CrawlStatus.Running, Records = 612,  LastRun = null,                     Next = null,                    Duration = "—" },
        new() { Id = "C05", DistrictName = "北區",   Status = CrawlStatus.Queued,  Records = 538,  LastRun = Utc("2026-05-27 03:21"), Next = null,                    Duration = "—" },
        new() { Id = "C06", DistrictName = "東區",   Status = CrawlStatus.Done,    Records = 421,  LastRun = Utc("2026-05-28 03:31"), Next = Utc("2026-05-29 03:00"), Duration = "2分10秒" },
        new() { Id = "C07", DistrictName = "南區",   Status = CrawlStatus.Done,    Records = 489,  LastRun = Utc("2026-05-28 03:34"), Next = Utc("2026-05-29 03:00"), Duration = "2分38秒" },
        new() { Id = "C08", DistrictName = "大里區", Status = CrawlStatus.Error,   Records = 0,    LastRun = Utc("2026-05-28 03:40"), Next = null,                    Duration = "逾時" },
    ];
}
