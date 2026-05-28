using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Session.LimbHealth;

[RegisterComponent]
public sealed partial class LimbSyringeComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    [DataField]
    public string Solution = "injector";

    [DataField]
    public bool Spent;
}

[RegisterComponent]
public sealed partial class LimbBleedCureComponent : Component
{
    [DataField]
    public bool CuresHeavy;

    [DataField]
    public bool ArmsLegsOnly;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);
}

[Serializable, NetSerializable]
public enum SyringeVisuals : byte
{
    Spent,
}

public enum LimbSurgeryStage : byte
{
    Needle,
    Scissors,
}

[RegisterComponent]
public sealed partial class LimbSurgeryToolComponent : Component
{
    [DataField]
    public LimbSurgeryStage Stage = LimbSurgeryStage.Needle;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);
}

[Serializable, NetSerializable]
public sealed partial class LimbSyringeDoAfterEvent : DoAfterEvent
{
    public LimbType Limb;

    public LimbSyringeDoAfterEvent()
    {
    }

    public LimbSyringeDoAfterEvent(LimbType limb)
    {
        Limb = limb;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class LimbSurgeryDoAfterEvent : DoAfterEvent
{
    public LimbSurgeryStage Stage;
    public LimbType Limb;

    public LimbSurgeryDoAfterEvent()
    {
    }

    public LimbSurgeryDoAfterEvent(LimbSurgeryStage stage, LimbType limb)
    {
        Stage = stage;
        Limb = limb;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class LimbBleedCureDoAfterEvent : DoAfterEvent
{
    public LimbType Limb;

    public LimbBleedCureDoAfterEvent()
    {
    }

    public LimbBleedCureDoAfterEvent(LimbType limb)
    {
        Limb = limb;
    }

    public override DoAfterEvent Clone() => this;
}
