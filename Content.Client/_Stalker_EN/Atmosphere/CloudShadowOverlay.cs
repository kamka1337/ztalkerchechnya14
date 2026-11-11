using System.Numerics;
using Content.Client.Light;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Stalker_EN.Atmosphere;

/// <summary>
/// Renders soft, drifting procedural cloud shadows onto the viewport light target during the BeforeLighting pass.
/// Point lights (campfires, flashlights, muzzle flashes) continue to illuminate over cloud shadows naturally.
/// </summary>
public sealed class CloudShadowOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> CloudShadowShaderId = "STCloudShadow";

    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    private readonly ShaderInstance _shadowShader;
    private readonly Vector2[] _corners = new Vector2[4];

    public CloudShadowOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = AfterLightTargetOverlay.ContentZIndex + 2;
        _shadowShader = _protoManager.Index(CloudShadowShaderId).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return args.Viewport.Eye != null && AtmosphereVisualHelper.IsOutdoorSkyMap(_entManager, args.MapUid);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.Viewport;
        var eye = viewport.Eye;
        if (eye == null)
            return;

        var (dayFactor, duskFactor, _) = AtmosphereVisualHelper.GetTimeFactors(_entManager, args.MapUid);
        var weatherBoost = AtmosphereVisualHelper.GetWeatherCloudBoost(_entManager, args.MapUid);

        // Cloud shadows are prominent in bright daylight, softer at dusk/dawn, and skipped completely at night.
        var shadowStrength = Math.Clamp(dayFactor * 0.27f + duskFactor * 0.09f + weatherBoost * dayFactor, 0.0f, 0.34f);
        if (shadowStrength <= 0.005f)
            return;

        var worldHandle = args.WorldHandle;
        var worldAABB = args.WorldAABB;
        var lightScale = viewport.LightRenderTarget.Size / (Vector2) viewport.Size;
        var scale = viewport.RenderScale / (Vector2.One / lightScale);

        _shadowShader.SetParameter("shadowStrength", shadowStrength);
        _shadowShader.SetParameter("cloudTime", (float) _timing.RealTime.TotalSeconds);
        _shadowShader.SetParameter("windVelocity", AtmosphereVisualHelper.CloudWindVelocity);

        worldHandle.RenderInRenderTarget(viewport.LightRenderTarget,
            () =>
            {
                var invMatrix = viewport.LightRenderTarget.GetWorldToLocalMatrix(eye, scale);
                worldHandle.SetTransform(invMatrix);
                worldHandle.UseShader(_shadowShader);

                // DrawPrimitives leaves aPos in world coordinates and applies ModelMatrix on the GPU,
                // whereas DrawRect bakes ModelMatrix into aPos on CPU (which would lock shadows to the screen).
                _corners[0] = worldAABB.BottomLeft;
                _corners[1] = worldAABB.BottomRight;
                _corners[2] = worldAABB.TopLeft;
                _corners[3] = worldAABB.TopRight;
                worldHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, _corners, Color.White);
                worldHandle.UseShader(null);
            },
            null);

        worldHandle.SetTransform(Matrix3x2.Identity);
    }
}
