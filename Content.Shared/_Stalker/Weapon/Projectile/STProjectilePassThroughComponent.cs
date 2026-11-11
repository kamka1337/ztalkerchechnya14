using Robust.Shared.GameStates;

namespace Content.Shared._Stalker.Weapon.Projectile;

/// <summary>
/// Allows projectiles to pass through this entity (such as sandbags) with a probability
/// that decreases as distance from the shot origin increases.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(STProjectileSystem))]
public sealed partial class STProjectilePassThroughComponent : Component
{
    /// <summary>
    /// Distance (in tiles) from the shot origin within which projectiles pass through at <see cref="PointBlankChance"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PointBlankRange = 1.5f;

    /// <summary>
    /// Probability (0..1) of passing through when fired within <see cref="PointBlankRange"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PointBlankChance = 1f;

    /// <summary>
    /// Reference distance (in tiles) at which the pass-through probability equals <see cref="ReferenceChance"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ReferenceRange = 10f;

    /// <summary>
    /// Probability (0..1) of passing through at <see cref="ReferenceRange"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ReferenceChance = 0.5f;

    /// <summary>
    /// Minimum probability (0..1) of passing through at long distances.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinChance = 0f;
}
