using Robust.Shared.GameStates;

namespace Content.Shared._Stalker.Weapon.GrenadeLauncher;

/// <summary>
/// Attached to a gun when the player has activated grenade-launcher aiming mode
/// via the context-menu verb.
/// Removed when the grenade is fired, when aiming is cancelled, or when the gun is deselected/dropped.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class STGrenadeLauncherAimingComponent : Component
{
    /// <summary>
    /// The GP-25 entity sitting inside the gun's launcher slot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? LauncherEntity;

    /// <summary>
    /// The player who activated the aiming mode.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? User;
}
