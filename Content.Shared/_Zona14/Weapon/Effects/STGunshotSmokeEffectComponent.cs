using System.Numerics;

namespace Content.Shared._Zona14.Weapon.Effects;

/// <summary>
/// Configures the client-side animation and dispersion of a gunshot smoke cloud entity.
/// </summary>
[RegisterComponent]
public sealed partial class STGunshotSmokeEffectComponent : Component
{
    /// <summary>
    /// Candidate RSI state names on the sprite; one is picked randomly per shot.
    /// </summary>
    [DataField]
    public List<string> States = new();

    /// <summary>
    /// Initial sprite scale at spawn.
    /// </summary>
    [DataField]
    public Vector2 StartScale = new(0.6f, 0.6f);

    /// <summary>
    /// Final sprite scale at the end of the animation.
    /// </summary>
    [DataField]
    public Vector2 EndScale = new(1.05f, 1.05f);

    /// <summary>
    /// Random fractional scale jitter (+/-) applied per shot.
    /// </summary>
    [DataField]
    public float ScaleJitter = 0.12f;

    /// <summary>
    /// Random angular spread (+/- degrees) around the shot direction.
    /// </summary>
    [DataField]
    public float AngleJitterDegrees = 6f;

    /// <summary>
    /// Forward drift distance along the shot direction during the cloud lifetime.
    /// </summary>
    [DataField]
    public float DriftDistance = 0.42f;

    /// <summary>
    /// Maximum random lateral drift perpendicular to the shot direction.
    /// </summary>
    [DataField]
    public float LateralDrift = 0.08f;

    /// <summary>
    /// Initial alpha opacity at spawn.
    /// </summary>
    [DataField]
    public float StartAlpha = 0.88f;

    /// <summary>
    /// Total animation duration in seconds.
    /// </summary>
    [DataField]
    public float Lifetime = 0.56f;
}
