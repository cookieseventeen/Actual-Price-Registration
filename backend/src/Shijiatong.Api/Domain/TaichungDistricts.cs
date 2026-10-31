namespace Shijiatong.Api.Domain;

/// <summary>台中市 29 行政區參考資料（id、生活圈說明、約略中心點）。</summary>
/// <remarks>
/// id 沿用既有拼音規則（原 12 區不變）。座標為約略的區公所位置，僅供「先用行政區中心點」之用，
/// 之後做 geocoding 時改用門牌座標。Zone 只保留原 12 區的生活圈說明，其餘為 null。
/// </remarks>
public static class TaichungDistricts
{
    public sealed record Info(string Id, string Name, string? Zone, double Lat, double Lng);

    public static readonly IReadOnlyList<Info> All =
    [
        new("xitun",    "西屯區", "七期 / 單元二",      24.1815, 120.6265),
        new("beitun",   "北屯區", "十四期 / 水湳",       24.1820, 120.6868),
        new("nantun",   "南屯區", "單元三 / 黎明",       24.1386, 120.6146),
        new("west",     "西區",   "草悟道 / 美術館",     24.1419, 120.6654),
        new("north",    "北區",   "一中 / 中國醫",       24.1586, 120.6825),
        new("east",     "東區",   "帝國糖廠 / 火車站",   24.1367, 120.6973),
        new("south",    "南區",   "中興大學 / 文心南",   24.1206, 120.6638),
        new("central",  "中區",   "舊城 / 綠空鐵道",     24.1426, 120.6797),
        new("dali",     "大里區", "國光 / 軟體園區",     24.0994, 120.6779),
        new("taiping",  "太平區", "坪林 / 新光重劃",     24.1265, 120.7186),
        new("fengyuan", "豐原區", "葫蘆墩 / 車站",       24.2524, 120.7183),
        new("wuri",     "烏日區", "高鐵特區",            24.1046, 120.6240),
        new("shalu",    "沙鹿區", null, 24.2335, 120.5667),
        new("wuqi",     "梧棲區", null, 24.2548, 120.5317),
        new("qingshui", "清水區", null, 24.2686, 120.5594),
        new("tanzi",    "潭子區", null, 24.2122, 120.7053),
        new("longjing", "龍井區", null, 24.1927, 120.5457),
        new("houli",    "后里區", null, 24.3093, 120.7108),
        new("dajia",    "大甲區", null, 24.3489, 120.6223),
        new("daya",     "大雅區", null, 24.2290, 120.6476),
        new("shengang", "神岡區", null, 24.2581, 120.6618),
        new("wufeng",   "霧峰區", null, 24.0616, 120.7003),
        new("waipu",    "外埔區", null, 24.3320, 120.6543),
        new("dadu",     "大肚區", null, 24.1538, 120.5413),
        new("dongshi",  "東勢區", null, 24.2589, 120.8279),
        new("xinshe",   "新社區", null, 24.2339, 120.8094),
        new("daan",     "大安區", null, 24.3461, 120.5865),
        new("shigang",  "石岡區", null, 24.2751, 120.7806),
        new("heping",   "和平區", null, 24.1757, 120.8848),
    ];

    private static readonly Dictionary<string, Info> ByName = All.ToDictionary(d => d.Name, StringComparer.Ordinal);

    /// <summary>以中文區名查詢（開放資料「鄉鎮市區」欄）。</summary>
    public static Info? FindByName(string name) => ByName.GetValueOrDefault(name.Trim());
}
