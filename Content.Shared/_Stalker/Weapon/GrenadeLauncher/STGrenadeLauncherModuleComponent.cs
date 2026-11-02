using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stalker.Weapon.GrenadeLauncher;

/// <summary>
/// Marks this entity as an underslung grenade launcher module (e.g. GP-25).
/// Placed into a gun's "gun_module_launcher" slot.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class STGrenadeLauncherModuleComponent : Component
{
    /// <summary>
    /// The item slot ID on this entity that holds the grenade cartridge.
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public string ChamberSlotId = "gun_gl_chamber";

    /// <summary>
    /// Speed at which the grenade projectile is launched (tiles/s).
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public float ProjectileSpeed = 45f;

    /// <summary>
    /// Action prototype to fire the grenade launcher (appears on the left side of the screen).
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId FireAction = "STActionFireGP25";

    /// <summary>
    /// Entity ID of the granted action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? FireActionEntity;

    /// <summary>
    /// Sound played when the grenade launcher fires.
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public SoundSpecifier? SoundShot = new SoundPathSpecifier("/Audio/_Stalker/Effects/Guns/grenade_launcher.ogg");

    /// <summary>
    /// Sound played when loading a grenade into the launcher.
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public SoundSpecifier? SoundLoad = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/revolver_cock.ogg");
}
