using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Shijiatong.Api.Domain.Entities;

namespace Shijiatong.Api.Infrastructure;

/// <summary>首次啟動寫入種子資料（移植自前端 mock）。冪等：資料已存在則略過。</summary>
public static class DbSeeder
{
    private static DateTime Utc(string s) =>
        DateTime.SpecifyKind(DateTime.Parse(s, CultureInfo.InvariantCulture), DateTimeKind.Utc);

    private static DateOnly D(string s) => DateOnly.Parse(s, CultureInfo.InvariantCulture);

    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Districts.AnyAsync())
            db.Districts.AddRange(Districts());

        if (!await db.Transactions.AnyAsync())
            db.Transactions.AddRange(Transactions());

        if (!await db.Members.AnyAsync())
            db.Members.AddRange(Members());

        if (!await db.CrawlTasks.AnyAsync())
            db.CrawlTasks.AddRange(CrawlTasks());

        await db.SaveChangesAsync();
    }

    private static District[] Districts() =>
    [
        new() { Id = "xitun",   Name = "西屯區", Zone = "七期 / 單元二",   AvgUnitPrice = 42.8m, ChangePct = 3.2m,  Volume = 1284 },
        new() { Id = "beitun",  Name = "北屯區", Zone = "十四期 / 水湳",    AvgUnitPrice = 34.1m, ChangePct = 5.1m,  Volume = 1607 },
        new() { Id = "nantun",  Name = "南屯區", Zone = "單元三 / 黎明",    AvgUnitPrice = 38.6m, ChangePct = 2.4m,  Volume = 1043 },
        new() { Id = "west",    Name = "西區",   Zone = "草悟道 / 美術館",  AvgUnitPrice = 40.2m, ChangePct = 1.8m,  Volume = 612  },
        new() { Id = "north",   Name = "北區",   Zone = "一中 / 中國醫",    AvgUnitPrice = 31.5m, ChangePct = 0.9m,  Volume = 538  },
        new() { Id = "east",    Name = "東區",   Zone = "帝國糖廠 / 火車站",AvgUnitPrice = 28.7m, ChangePct = 4.3m,  Volume = 421  },
        new() { Id = "south",   Name = "南區",   Zone = "中興大學 / 文心南",AvgUnitPrice = 29.9m, ChangePct = 3.6m,  Volume = 489  },
        new() { Id = "central", Name = "中區",   Zone = "舊城 / 綠空鐵道",  AvgUnitPrice = 24.3m, ChangePct = -0.4m, Volume = 156  },
        new() { Id = "dali",    Name = "大里區", Zone = "國光 / 軟體園區",  AvgUnitPrice = 26.4m, ChangePct = 2.1m,  Volume = 734  },
        new() { Id = "taiping", Name = "太平區", Zone = "坪林 / 新光重劃",  AvgUnitPrice = 24.8m, ChangePct = 1.5m,  Volume = 658  },
        new() { Id = "fengyuan",Name = "豐原區", Zone = "葫蘆墩 / 車站",    AvgUnitPrice = 22.1m, ChangePct = 0.7m,  Volume = 402  },
        new() { Id = "wuri",    Name = "烏日區", Zone = "高鐵特區",         AvgUnitPrice = 27.6m, ChangePct = 6.2m,  Volume = 388  },
    ];

    // 行政區中文名 -> id（與 Districts() 對應）
    private static readonly Dictionary<string, string> NameToId = new()
    {
        ["西屯區"] = "xitun", ["北屯區"] = "beitun", ["南屯區"] = "nantun", ["西區"] = "west",
        ["北區"] = "north", ["東區"] = "east", ["南區"] = "south", ["中區"] = "central",
        ["大里區"] = "dali", ["太平區"] = "taiping", ["豐原區"] = "fengyuan", ["烏日區"] = "wuri",
    };

    private static Transaction Tx(string id, string community, string district, string road, string section,
        string type, int total, decimal unit, decimal ping, string floor, int age, string layout, int rooms,
        string trade, string date, string crawled, double lat, double lng, string parking) => new()
    {
        Id = id, Community = community, DistrictId = NameToId[district], Road = road, Section = section,
        Type = type, Total = total, Unit = unit, Ping = ping, Floor = floor, Age = age, Layout = layout,
        Rooms = rooms, Trade = trade, Date = D(date), Source = "內政部", CrawledAt = Utc(crawled),
        Lat = lat, Lng = lng, Parking = parking,
    };

    private static Transaction[] Transactions() =>
    [
        Tx("T0001", "聯聚理仁",     "西屯區", "市政路", "七期重劃區",   "住宅大樓", 8980,  86.4m, 103.9m, "18/26", 6,  "4房2廳", 4, "成屋",   "2026-05-12", "2026-05-28 03:14", 24.161, 120.642, "坡道平面"),
        Tx("T0002", "惠宇仁愛",     "西屯區", "惠中路", "七期重劃區",   "住宅大樓", 4280,  58.2m, 73.5m,  "11/22", 9,  "3房2廳", 3, "成屋",   "2026-05-08", "2026-05-28 03:14", 24.155, 120.645, "坡道平面"),
        Tx("T0003", "由鉅大恆",     "西屯區", "河南路", "單元二",       "住宅大樓", 3650,  49.8m, 73.3m,  "8/15",  4,  "3房2廳", 3, "成屋",   "2026-05-15", "2026-05-28 03:14", 24.179, 120.625, "坡道機械"),
        Tx("T0004", "富宇九大",     "北屯區", "崇德路", "十四期重劃",   "住宅大樓", 2180,  36.5m, 59.7m,  "6/14",  2,  "2房2廳", 2, "成屋",   "2026-05-19", "2026-05-28 03:14", 24.183, 120.689, "坡道平面"),
        Tx("T0005", "惠宇澄品",     "北屯區", "環中路", "水湳經貿園區", "住宅大樓", 2980,  41.2m, 72.3m,  "14/24", 1,  "3房2廳", 3, "預售屋", "2026-05-21", "2026-05-28 03:14", 24.193, 120.651, "坡道平面"),
        Tx("T0006", "太子昀",       "北屯區", "后庄路", "北屯機捷",     "電梯華廈", 1480,  28.9m, 51.2m,  "5/11",  3,  "2房2廳", 2, "成屋",   "2026-05-05", "2026-05-28 03:14", 24.201, 120.704, "坡道機械"),
        Tx("T0007", "寶輝秋紅谷",   "西屯區", "朝富路", "七期重劃區",   "住宅大樓", 12800, 98.6m, 129.8m, "29/32", 12, "4房2廳", 4, "成屋",   "2026-04-28", "2026-05-28 03:14", 24.165, 120.638, "坡道平面"),
        Tx("T0008", "勤美璞真",     "西區",   "英才路", "草悟道",       "住宅大樓", 5640,  64.3m, 87.7m,  "16/20", 8,  "3房2廳", 3, "成屋",   "2026-05-10", "2026-05-28 03:14", 24.148, 120.665, "坡道平面"),
        Tx("T0009", "達麗世界中心", "西區",   "公益路", "美術館",       "住宅大樓", 3120,  47.5m, 65.7m,  "9/18",  7,  "2房2廳", 2, "成屋",   "2026-05-17", "2026-05-28 03:14", 24.143, 120.658, "坡道平面"),
        Tx("T0010", "總太東方帝國", "南屯區", "惠中路", "單元三",       "住宅大樓", 2760,  39.4m, 70.1m,  "12/18", 5,  "3房2廳", 3, "成屋",   "2026-05-03", "2026-05-28 03:14", 24.137, 120.628, "坡道平面"),
        Tx("T0011", "惠宇觀韻",     "南屯區", "黎明路", "黎明重劃",     "住宅大樓", 2240,  35.8m, 62.6m,  "7/15",  4,  "3房2廳", 3, "成屋",   "2026-05-20", "2026-05-28 03:14", 24.131, 120.633, "坡道機械"),
        Tx("T0012", "精銳PARK",     "南屯區", "公益路", "文心森林",     "住宅大樓", 4480,  55.1m, 81.3m,  "20/25", 6,  "4房2廳", 4, "成屋",   "2026-04-30", "2026-05-28 03:14", 24.140, 120.640, "坡道平面"),
        Tx("T0013", "一中商圈套房", "北區",   "育才街", "一中商圈",     "套房",     528,   26.4m, 20.0m,  "8/12",  11, "套房",   1, "成屋",   "2026-05-14", "2026-05-28 03:14", 24.150, 120.684, "無"),
        Tx("T0014", "城市風laze",   "北區",   "太原路", "中國醫",       "電梯華廈", 1380,  30.1m, 45.8m,  "6/13",  5,  "2房2廳", 2, "成屋",   "2026-05-06", "2026-05-28 03:14", 24.166, 120.681, "坡道機械"),
        Tx("T0015", "帝國糖廠特區", "東區",   "復興路", "帝國糖廠",     "住宅大樓", 1620,  29.3m, 55.3m,  "10/17", 2,  "3房2廳", 3, "預售屋", "2026-05-22", "2026-05-28 03:14", 24.137, 120.692, "坡道平面"),
        Tx("T0016", "興大學區寓",   "南區",   "國光路", "中興大學",     "華廈",     980,   27.2m, 36.0m,  "4/7",   18, "2房2廳", 2, "成屋",   "2026-05-01", "2026-05-28 03:14", 24.123, 120.675, "無"),
        Tx("T0017", "國光花園",     "大里區", "國光路", "國光重劃",     "住宅大樓", 1280,  26.8m, 47.8m,  "9/15",  3,  "3房2廳", 3, "成屋",   "2026-05-11", "2026-05-28 03:14", 24.099, 120.677, "坡道平面"),
        Tx("T0018", "軟體園區寓",   "大里區", "科技路", "軟體園區",     "電梯華廈", 1050,  24.9m, 42.2m,  "7/12",  4,  "2房2廳", 2, "成屋",   "2026-04-26", "2026-05-28 03:14", 24.105, 120.690, "坡道機械"),
        Tx("T0019", "坪林綠意",     "太平區", "宜昌路", "坪林重劃",     "住宅大樓", 1180,  25.3m, 46.6m,  "8/14",  2,  "3房2廳", 3, "預售屋", "2026-05-18", "2026-05-28 03:14", 24.135, 120.718, "坡道平面"),
        Tx("T0020", "高鐵之心",     "烏日區", "高鐵五路","高鐵特區",    "住宅大樓", 1560,  28.4m, 54.9m,  "13/20", 1,  "3房2廳", 3, "預售屋", "2026-05-23", "2026-05-28 03:14", 24.111, 120.615, "坡道平面"),
        Tx("T0021", "葫蘆墩之心",   "豐原區", "中正路", "豐原車站",     "電梯華廈", 880,   21.7m, 40.6m,  "6/11",  5,  "2房2廳", 2, "成屋",   "2026-05-09", "2026-05-28 03:14", 24.254, 120.723, "坡道機械"),
        Tx("T0022", "聯聚方庭",     "西屯區", "惠來路", "七期重劃區",   "住宅大樓", 9650,  89.1m, 108.3m, "22/28", 7,  "4房2廳", 4, "成屋",   "2026-04-24", "2026-05-28 03:14", 24.158, 120.640, "坡道平面"),
    ];

    private static Member M(string name, string email, string avatar, Provider provider, Plan plan,
        MemberStatus status, string purpose, string created, string? reviewed = null, string? note = null) => new()
    {
        Id = Guid.NewGuid(), Name = name, Email = email, Avatar = avatar, Provider = provider, Plan = plan,
        Status = status, Purpose = purpose, CreatedAt = Utc(created),
        ReviewedAt = reviewed is null ? null : Utc(reviewed), Note = note,
    };

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
