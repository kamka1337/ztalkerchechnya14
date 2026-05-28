using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Session.LimbHealth;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class LimbHealthComponent : Component
{
    [DataField]
    public Dictionary<LimbType, FixedPoint2> MaxHealth = new()
    {
        [LimbType.Head] = 35,
        [LimbType.Chest] = 85,
        [LimbType.Abdomen] = 70,
        [LimbType.LeftArm] = 60,
        [LimbType.RightArm] = 60,
        [LimbType.LeftLeg] = 65,
        [LimbType.RightLeg] = 65,
    };

    [DataField, AutoNetworkedField]
    public Dictionary<LimbType, LimbState> Limbs = new();

    [DataField]
    public float OldDamageMultiplier = 4.4f;

    [DataField]
    public float BleedLightChance = 0.25f;

    [DataField]
    public float BleedHeavyChance = 0.10f;

    [DataField]
    public float BleedEscalateChance = 0.25f;

    [DataField]
    public FixedPoint2 LightBleedDamage = 5;

    [DataField]
    public FixedPoint2 HeavyBleedDamage = 10;

    [DataField]
    public FixedPoint2 LightBleedPuddle = 2;

    [DataField]
    public FixedPoint2 HeavyBleedPuddle = 8;

    [DataField]
    public TimeSpan BleedInterval = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan NextBleedTick;

    [DataField]
    public TimeSpan ChestAsphyxiationInterval = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan NextChestCheck;

    [DataField]
    public int ChestCheckStep;

    [DataField, AutoNetworkedField]
    public bool PermanentAsphyxiation;

    [DataField]
    public HashSet<LimbType> NeedledLimbs = new();

    [DataField, AutoNetworkedField]
    public List<LimbReagentDose> ActiveDoses = new();

    [DataField]
    public TimeSpan NextDoseTick;

    [DataField]
    public TimeSpan DoseInterval = TimeSpan.FromSeconds(1);
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class LimbReagentDose
{
    [DataField]
    public LimbType Limb;

    [DataField]
    public string Reagent = string.Empty;

    [DataField]
    public FixedPoint2 Healed;
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class LimbState
{
    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public bool Destroyed;

    [DataField]
    public LimbEffect Effects;

    public FixedPoint2 TotalDamage => Damage.GetTotal();

    public FixedPoint2 Health(FixedPoint2 max) => FixedPoint2.Max(FixedPoint2.Zero, max - Damage.GetTotal());
}

public static class KuklaLayout
{
    public const int Width = 32;
    public const int Height = 64;

    public static readonly Dictionary<LimbType, Vector2> Centers = new()
    {
        [LimbType.Head] = new Vector2(0.500f, 0.078f),
        [LimbType.Chest] = new Vector2(0.469f, 0.250f),
        [LimbType.Abdomen] = new Vector2(0.453f, 0.406f),
        [LimbType.LeftArm] = new Vector2(0.172f, 0.359f),
        [LimbType.RightArm] = new Vector2(0.812f, 0.352f),
        [LimbType.LeftLeg] = new Vector2(0.281f, 0.711f),
        [LimbType.RightLeg] = new Vector2(0.641f, 0.719f),
    };
}
