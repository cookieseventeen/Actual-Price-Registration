namespace Shijiatong.Lvr;

/// <summary>排除原因（依判斷順序，只記第一個命中的）。</summary>
public enum LvrExclusion
{
    /// <summary>交易標的非「房地」：純土地、純車位、單獨建物。</summary>
    NotBuildingTarget,
    /// <summary>親友、員工、共有人或其他特殊關係間之交易。</summary>
    SpecialRelation,
    /// <summary>非一般市場交易：政府標讓售、協議價購、地清標售、法拍、僅車位等。</summary>
    NonMarketSale,
    /// <summary>非住宅型態：店面、辦公、廠房、「其他」…</summary>
    NonResidentialType,
    /// <summary>必要欄位無法解析：交易日、總價、建物面積。</summary>
    InvalidData,
}

/// <summary>正規化後的一筆住宅買賣（單位已換成 萬 / 坪 / 萬每坪）。</summary>
public sealed record LvrTransaction(
    string SerialNo,
    string District,
    string Address,
    string? Road,
    string BuildingType,
    string RawBuildingType,
    string MainUse,
    DateOnly TradeDate,
    DateOnly? CompletedOn,
    int? AgeYears,
    int Total,                  // 總價（萬，四捨五入）
    long TotalNtd,              // 總價（元，原始）
    decimal Unit,               // 單價（萬/坪）
    bool UnitDerived,           // 官方單價空白，改以 (總價-車位價)/(面積-車位面積) 推算
    decimal Ping,               // 建物移轉總面積（坪，含車位）
    decimal MainPing,           // 主建物面積（坪）
    int? Floor,                 // 移轉層次中最低的地上樓層；透天「全」為 null
    int? TotalFloors,
    string FloorLabel,          // 前端格式：18/26、全/4、1-2/5
    int Rooms,
    int Halls,
    int Baths,
    string Layout,              // 3房2廳
    string? ParkingType,
    decimal ParkingPing,
    long ParkingPriceNtd,
    bool HasElevator,
    IReadOnlyList<string> Notes)
{
    /// <summary>預售屋交屋（或土地建物分件登記）：交易日為當年簽約日，屋齡多為 0。</summary>
    public bool IsPresaleTransfer => Notes.Any(n => n.Contains(LvrNormalizer.PresaleTransferNote, StringComparison.Ordinal));

    /// <summary>單價異常（保留原值，統計排除）。</summary>
    public bool IsOutlier => LvrNormalizer.IsUnitOutlier(Unit);
}

public sealed record LvrNormalizeResult(LvrTransaction? Value, LvrExclusion? Excluded)
{
    public static LvrNormalizeResult Ok(LvrTransaction t) => new(t, null);
    public static LvrNormalizeResult Skip(LvrExclusion r) => new(null, r);
}

/// <summary>a 檔（不動產買賣）原始列 → 住宅成屋買賣。只保留可代表市場行情的交易。</summary>
public static class LvrNormalizer
{
    /// <summary>備註中代表「預售屋交屋 / 分件登記」的字樣。</summary>
    public const string PresaleTransferNote = "預售屋、或土地及建物分件登記";

    /// <summary>單價合理區間（萬/坪）；區間外標為異常值。2026-10 PoC：P1 13.8、P99 77.6。</summary>
    public const decimal MinReasonableUnit = 5m;
    public const decimal MaxReasonableUnit = 200m;

    public static bool IsUnitOutlier(decimal unitWanPerPing) =>
        unitWanPerPing < MinReasonableUnit || unitWanPerPing > MaxReasonableUnit;

    private static readonly string[] BuildingTargets = ["房地(土地+建物)", "房地(土地+建物)+車位"];

    private static readonly string[] SpecialRelationKeywords = ["親友、員工、共有人或其他特殊關係"];

    private static readonly string[] NonMarketKeywords =
        ["政府機關標讓售", "協議價購", "地清或未辦繼承標售", "法院拍賣", "法拍", "僅車位交易", "公有土地標售"];

    /// <summary>建物型態（原始字串前綴）→ 對外顯示名稱；不在表中的型態視為非住宅。</summary>
    private static readonly (string Prefix, string Name)[] ResidentialTypes =
    [
        ("住宅大樓", "住宅大樓"),
        ("華廈", "華廈"),
        ("公寓", "公寓"),
        ("透天厝", "透天厝"),
        ("套房", "套房"),
    ];

    public static LvrNormalizeResult Normalize(LvrRawRecord r)
    {
        var target = r[LvrColumns.Target];
        if (!BuildingTargets.Contains(target)) return LvrNormalizeResult.Skip(LvrExclusion.NotBuildingTarget);

        var note = r[LvrColumns.Note];
        if (SpecialRelationKeywords.Any(note.Contains)) return LvrNormalizeResult.Skip(LvrExclusion.SpecialRelation);
        if (NonMarketKeywords.Any(note.Contains)) return LvrNormalizeResult.Skip(LvrExclusion.NonMarketSale);

        var rawType = r[LvrColumns.BuildingType];
        var type = ResidentialTypes.FirstOrDefault(t => rawType.StartsWith(t.Prefix, StringComparison.Ordinal)).Name;
        if (type is null) return LvrNormalizeResult.Skip(LvrExclusion.NonResidentialType);

        var tradeDate = LvrParsing.ParseRocDate(r[LvrColumns.TradeDate]);
        var totalNtd = LvrParsing.ParseLong(r[LvrColumns.TotalPrice]);
        var areaSqm = LvrParsing.ParseDecimal(r[LvrColumns.BuildingArea]);
        if (tradeDate is null || totalNtd <= 0 || areaSqm <= 0) return LvrNormalizeResult.Skip(LvrExclusion.InvalidData);

        var parkingSqm = LvrParsing.ParseDecimal(r[LvrColumns.ParkingArea]);
        var parkingNtd = LvrParsing.ParseLong(r[LvrColumns.ParkingPrice]);

        var unitNtdPerSqm = LvrParsing.ParseDecimal(r[LvrColumns.UnitPrice]);
        var unitDerived = false;
        if (unitNtdPerSqm <= 0)
        {
            // 官方單價空白時自行推算；車位未單獨計價時無法扣除，只能用含車位面積
            var netArea = parkingNtd > 0 && areaSqm > parkingSqm ? areaSqm - parkingSqm : areaSqm;
            unitNtdPerSqm = (totalNtd - parkingNtd) / netArea;
            unitDerived = true;
        }

        var completedOn = LvrParsing.ParseRocCompletion(r[LvrColumns.CompletedOn]);
        var floors = LvrParsing.ParseFloors(r[LvrColumns.Floor]);
        var totalFloorList = LvrParsing.ParseFloors(r[LvrColumns.TotalFloors]).Where(f => f > 0).ToList();
        int? totalFloors = totalFloorList.Count > 0 ? totalFloorList.Max() : null;
        var aboveGround = floors.Where(f => f > 0).Order().ToList();
        int? floor = aboveGround.Count > 0 ? aboveGround[0] : floors.Count > 0 ? floors.Min() : null;

        var rooms = LvrParsing.ParseInt(r[LvrColumns.Rooms]);
        var halls = LvrParsing.ParseInt(r[LvrColumns.Halls]);
        var parkingType = r[LvrColumns.ParkingType];
        var district = r[LvrColumns.District];
        var address = LvrParsing.NormalizeDigits(r[LvrColumns.Address]);

        return LvrNormalizeResult.Ok(new LvrTransaction(
            SerialNo: r[LvrColumns.SerialNo],
            District: district,
            Address: address,
            Road: LvrParsing.ExtractRoad(address, district),
            BuildingType: type,
            RawBuildingType: rawType,
            MainUse: r[LvrColumns.MainUse],
            TradeDate: tradeDate.Value,
            CompletedOn: completedOn,
            AgeYears: LvrParsing.AgeYears(tradeDate.Value, completedOn),
            Total: (int)Math.Round(totalNtd / 10000m, MidpointRounding.AwayFromZero),
            TotalNtd: totalNtd,
            Unit: LvrParsing.NtdPerSqmToWanPerPing(unitNtdPerSqm),
            UnitDerived: unitDerived,
            Ping: LvrParsing.SquareMetersToPing(areaSqm),
            MainPing: LvrParsing.SquareMetersToPing(LvrParsing.ParseDecimal(r[LvrColumns.MainBuildingArea])),
            Floor: floor,
            TotalFloors: totalFloors,
            FloorLabel: FloorLabel(r[LvrColumns.Floor], aboveGround, floors, totalFloors),
            Rooms: rooms,
            Halls: halls,
            Baths: LvrParsing.ParseInt(r[LvrColumns.Baths]),
            Layout: $"{rooms}房{halls}廳",
            ParkingType: string.IsNullOrEmpty(parkingType) ? null : parkingType,
            ParkingPing: LvrParsing.SquareMetersToPing(parkingSqm),
            ParkingPriceNtd: parkingNtd,
            HasElevator: r[LvrColumns.Elevator] == "有",
            Notes: note.Split('；', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));
    }

    /// <summary>前端樓層字串：18/26、全/4、1-2/5；無法解析的部分以 ? 代替。</summary>
    internal static string FloorLabel(string rawFloor, IReadOnlyList<int> aboveGround, IReadOnlyList<int> all, int? totalFloors)
    {
        var top = totalFloors?.ToString() ?? "?";
        if (rawFloor == "全") return $"全/{top}";
        string part;
        if (aboveGround.Count == 0) part = all.Count > 0 ? $"B{-all.Min()}" : "?";
        else if (aboveGround.Count == 1) part = aboveGround[0].ToString();
        else part = $"{aboveGround[0]}-{aboveGround[^1]}";
        return $"{part}/{top}";
    }
}
