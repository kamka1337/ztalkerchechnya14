using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Zona14.Weapon.Effects;

/// <summary>
/// Spawns a client-side gunshot smoke cloud effect when this weapon fires.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class STGunshotSmokeComponent : Component
{
    /// <summary>
    /// Primary smoke puff entity prototype spawned at the muzzle.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId EffectProto;

    /// <summary>
    /// Optional secondary trailing barrel wisp prototype spawned behind the main puff.
    /// </summary>
    [DataField]
    public EntProtoId? SecondaryEffectProto;

    /// <summary>
    /// Forward distance from the gun/user origin along the shot angle where the main cloud spawns.
    /// </summary>
    [DataField]
    public float MuzzleOffset = 0.56f;

    /// <summary>
    /// Forward distance from the gun/user origin where the secondary wisp spawns.
    /// </summary>
    [DataField]
    public float SecondaryOffset = 0.44f;

    /// <summary>
    /// Extra forward offset applied when a muzzle module (silencer) is attached.
    /// </summary>
    [DataField]
    public float SuppressedMuzzleOffsetBonus = 0.16f;

    /// <summary>
    /// Scale multiplier applied to the smoke effect when suppressed.
    /// </summary>
    [DataField]
    public float SuppressedScaleMultiplier = 0.6f;

    /// <summary>
    /// Alpha multiplier applied to the smoke effect when suppressed.
    /// </summary>
    [DataField]
    public float SuppressedAlphaMultiplier = 0.55f;
}
