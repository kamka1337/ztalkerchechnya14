using Content.Shared._DZ.FarGunshot;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._DZ.FarGunshot;

public sealed class FarGunshotSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FarGunshotComponent, FargunshotEvent>(OnFarGunshot);
    }

    public void OnFarGunshot(EntityUid uid, FarGunshotComponent component, FargunshotEvent args)
    {
        if (uid == EntityUid.Invalid || args.GunUid != uid.Id)
            return;

        var shootPos = _transform.GetMapCoordinates(uid);

        var range = component.Range * (component.SilencerDecrease ?? component.BaseSilencerDecrease);

        if (range <= 14f)
            return;

        // Create a filter for players who are far enough to hear the distant gunshot,
        // excluding those within close range (14)
        var farSoundFilter = Filter.Empty()
            .AddInRange(shootPos, range)
            .RemoveInRange(shootPos, 14f);

        // Do not spawn audio entity or parse audio file if nobody can hear it
        if (farSoundFilter.Count == 0)
            return;

        _audio.PlayGlobal(
            component.Sound,
            farSoundFilter,
            recordReplay: true,
            AudioParams.Default
        );
    }
}