using Robust.Shared.GameStates;

namespace Content.Shared._Session.LimbHealth;

/// <summary>
/// When attached to a projectile (e.g. shrapnel), its damage against entities with LimbHealthComponent
/// is routed exclusively to limbs (arms and legs), never to head or chest.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TargetLimbsOnlyComponent : Component
{
}
