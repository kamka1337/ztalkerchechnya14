using Robust.Client.Graphics;

namespace Content.Client._Stalker_EN.Atmosphere;

/// <summary>
/// Registers the outdoor drifting cloud shadow overlay on the client.
/// </summary>
public sealed class AtmosphericCloudsSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new CloudShadowOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<CloudShadowOverlay>();
    }
}
