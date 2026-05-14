using Content.Server._Stalker.WarZone;
using Content.Shared._Session.Flag;
using Content.Server._Stalker.StalkerDB;
using Content.Server._Stalker.Storage;
using Content.Shared._Stalker.WarZone;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Server.RoundEnd;
using System.Linq;

namespace Content.Server._Session.Flag;

public sealed class FlagWarZoneSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly WarZoneSystem _warSys = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly StalkerStorageSystem _stalkerStorageSystem = default!;


    private const float UpdateInterval = 1f;
    private float _timer;
    private readonly Dictionary<EntityUid, float> _prevProgress = new();

    public override void Update(float frameTime)
    {
        _timer += frameTime;
        if (_timer < UpdateInterval)
            return;
        _timer = 0f;

        var query = EntityQueryEnumerator<WarZoneFlagComponent, WarZoneComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out _, out var wz, out var app))
        {
            if (!_proto.TryIndex<STWarZonePrototype>(wz.ZoneProto, out var wzProto) || wzProto.CaptureTime <= 0f)
                continue;

            var progress = wz.CaptureProgress;

            // Detect capture completion: was progressing, now reset to 0
            if (_prevProgress.TryGetValue(uid, out var prev) && prev >= 0.8f && progress <= 0f)
            {
                var audioPath = (string)wz.ZoneProto == "DutyBase"
                    ? "/Audio/_Session/duty.ogg"
                    : "/Audio/_Session/freedom.ogg";
                _audio.PlayGlobal(audioPath, Filter.Broadcast(), true);
            }
            _prevProgress[uid] = progress;

            int level;
            if (progress <= 0f)      level = 3;
            else if (progress >= 0.8f) level = 1;
            else if (progress >= 0.5f) level = 2;
            else level = 3;

            var zonesList = _warSys.GetAllWarZones().ToList();

            var freedomZone = zonesList.FirstOrDefault(z => z.Component.ZoneProto == "FreedomBase");
            var dutyZone = zonesList.FirstOrDefault(z => z.Component.ZoneProto == "DutyBase");

            if (freedomZone != default && freedomZone.Component.DefendingBandProtoId != "STFreedomBand")
            {
                level = 4;
                _roundEndSystem.EndRound();
            }
            else if (dutyZone != default && dutyZone.Component.DefendingBandProtoId != "STDolgBand")
            {
                level = 4;
                _roundEndSystem.EndRound();
            }
            _appearance.SetData(uid, WarZoneFlagVisuals.Level, level, app);
        }
    }
}
