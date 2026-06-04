using Content.Shared.Hands.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._Session.LimbHealth;

[Serializable, NetSerializable]
public enum LimbType : byte
{
    Head,
    Chest,
    Abdomen,
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg,
}

[Flags, Serializable, NetSerializable]
public enum LimbEffect : byte
{
    None = 0,
    Bleeding = 1 << 0,
    HeavyBleeding = 1 << 1,
    Fracture = 1 << 2,
    Painkiller = 1 << 3,
    Good = 1 << 4,
    Bad = 1 << 5,
}

public static class LimbTypeExtensions
{
    public static bool IsLeg(this LimbType limb) => limb is LimbType.LeftLeg or LimbType.RightLeg;
    public static bool IsArm(this LimbType limb) => limb is LimbType.LeftArm or LimbType.RightArm;

    public static string SpriteFolder(this LimbType limb) => limb switch
    {
        LimbType.Head => "head",
        LimbType.Chest => "throax",
        LimbType.Abdomen => "puzo",
        LimbType.LeftArm => "left_arm",
        LimbType.RightArm => "right_arm",
        LimbType.LeftLeg => "left_leg",
        LimbType.RightLeg => "right_leg",
        _ => "throax",
    };

    public static float HitChance(this LimbType limb) => limb switch
    {
        LimbType.Head => 0.40f,
        LimbType.Chest => 0.75f,
        LimbType.Abdomen => 0.75f,
        LimbType.LeftArm or LimbType.RightArm => 0.50f,
        LimbType.LeftLeg or LimbType.RightLeg => 0.55f,
        _ => 0.50f,
    };
}

public static class LimbHandMap
{
    public static bool TryGetArmForHand(HandLocation location, out LimbType arm)
    {
        switch (location)
        {
            case HandLocation.Left:
                arm = LimbType.LeftArm;
                return true;
            case HandLocation.Right:
                arm = LimbType.RightArm;
                return true;
            default:
                arm = default;
                return false;
        }
    }
}
