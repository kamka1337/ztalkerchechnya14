using Content.Shared.FixedPoint;

namespace Content.Shared._Session.LimbHealth;

public static class LimbReagents
{
    public static readonly IReadOnlyDictionary<string, LimbReagentInfo> All = new Dictionary<string, LimbReagentInfo>
    {
        ["Ketorolac"] = new(
            RatePerSecond: 3,
            TotalHeal: 35,
            HealAllTypes: false),
        ["Dextropin"] = new(
            RatePerSecond: 3,
            TotalHeal: 60,
            HealAllTypes: true),
        ["Tranexam"] = new(
            RatePerSecond: 7,
            TotalHeal: 50,
            HealAllTypes: false),
    };

    public static readonly HashSet<string> PhysicalDamageTypes = new()
    {
        "Blunt",
        "Slash",
        "Piercing",
        "Heat",
        "Cold",
    };
}

public sealed record LimbReagentInfo(FixedPoint2 RatePerSecond, FixedPoint2 TotalHeal, bool HealAllTypes);
