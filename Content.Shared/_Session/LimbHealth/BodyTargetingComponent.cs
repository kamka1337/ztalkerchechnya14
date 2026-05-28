using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Session.LimbHealth;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BodyTargetingComponent : Component
{
    [DataField, AutoNetworkedField]
    public LimbType SelectedLimb = LimbType.Chest;
}

[Serializable, NetSerializable]
public sealed class SelectLimbEvent : EntityEventArgs
{
    public LimbType Limb;

    public SelectLimbEvent(LimbType limb)
    {
        Limb = limb;
    }
}
