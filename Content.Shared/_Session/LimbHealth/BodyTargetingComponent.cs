using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Session.LimbHealth;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BodyTargetingComponent : Component
{
    [DataField, AutoNetworkedField]
    public LimbType SelectedLimb = LimbType.Chest;

    [DataField, AutoNetworkedField]
    public bool Aiming;
}

[Serializable, NetSerializable]
public sealed class SelectLimbEvent : EntityEventArgs
{
    public LimbType Limb;
    public bool Aiming;

    public SelectLimbEvent(LimbType limb, bool aiming)
    {
        Limb = limb;
        Aiming = aiming;
    }
}
