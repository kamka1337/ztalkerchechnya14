using Content.Client._Stalker_EN.Atmosphere;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._Stalker_EN.Overlays;

/// <summary>
/// Atmospheric post-processing overlay: soft bloom, gentle edge softening, filmic highlight roll-off,
/// and time-of-day colour grading to eliminate harsh digital contrast.
/// </summary>
public sealed class AtmosphericFilmOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> FilmShaderId = "STAtmosphereFilm";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _filmShader;

    public AtmosphericFilmOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 2;
        _filmShader = _prototypeManager.Index(FilmShaderId).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return false;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;
        if (playerEntity == null || !_entityManager.TryGetComponent(playerEntity, out EyeComponent? eyeComp))
            return false;

        return args.Viewport.Eye == eyeComp.Eye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var (_, duskFactor, nightFactor) = AtmosphereVisualHelper.GetTimeFactors(_entityManager, args.MapUid);
        var bloomStrength = 0.075f + duskFactor * 0.045f + nightFactor * 0.065f;

        _filmShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _filmShader.SetParameter("bloomStrength", bloomStrength);
        _filmShader.SetParameter("duskFactor", duskFactor);
        _filmShader.SetParameter("nightFactor", nightFactor);

        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_filmShader);
        worldHandle.DrawRect(args.WorldBounds, Color.White);
        worldHandle.UseShader(null);
    }
}
