namespace Content.Shared._Zona14.Weapon.Effects;

/// <summary>
/// Raised locally on the client on a gun entity when a muzzle flash effect is triggered.
/// </summary>
[ByRefEvent]
public record struct STGunshotMuzzleEffectEvent(Angle WorldAngle, EntityUid? Tracked);
