using System;
using System.Collections.Generic;
using Content.Server._Stalker.WarZone;
using Content.Shared._Session.Flag;
using Content.Shared._Stalker.WarZone;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Server.RoundEnd;
using Content.Shared.GameTicking;

namespace Content.Server._Session.Flag;

public sealed class FlagWarZoneSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly WarZoneSystem _warSys = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;

    private const float UpdateInterval = 0.25f;
    private float _timer;
    private bool _roundEnded;
    private readonly HashSet<EntityUid> _capturedBases = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _roundEnded = false;
        _capturedBases.Clear();
    }

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

            int level;

            if (_capturedBases.Contains(uid))
            {
                level = 4;
            }
            else if (IsRfBase(uid, wz))
            {
                if (IsRfBaseCaptured(wz))
                {
                    _capturedBases.Add(uid);
                    level = 4;

                    if (!_roundEnded)
                    {
                        _roundEnded = true;
                        _audio.PlayGlobal("/Audio/_Session/CRIpobeda.ogg", Filter.Broadcast(), true);
                        _roundEndSystem.EndRound();
                    }
                }
                else
                {
                    level = GetCaptureProgressLevel(wz.CaptureProgress);
                }
            }
            else if (IsCriBase(uid, wz))
            {
                if (IsCriBaseCaptured(wz))
                {
                    _capturedBases.Add(uid);
                    level = 4;

                    if (!_roundEnded)
                    {
                        _roundEnded = true;
                        _audio.PlayGlobal("/Audio/_Session/RFpobeda.ogg", Filter.Broadcast(), true);
                        _roundEndSystem.EndRound();
                    }
                }
                else
                {
                    level = GetCaptureProgressLevel(wz.CaptureProgress);
                }
            }
            else
            {
                level = GetCaptureProgressLevel(wz.CaptureProgress);
            }

            _appearance.SetData(uid, WarZoneFlagVisuals.Level, level, app);
        }
    }

    private static int GetCaptureProgressLevel(float progress)
    {
        if (progress >= 0.8f)
            return 1;
        if (progress >= 0.5f)
            return 2;
        return 3;
    }

    private bool IsRfBase(EntityUid uid, WarZoneComponent wz)
    {
        if (MetaData(uid).EntityPrototype?.ID.Equals("WarZoneEntity_DutyBase", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        var proto = wz.ZoneProto.Id;
        if (proto.Equals("DutyBase", StringComparison.OrdinalIgnoreCase) ||
            proto.Equals("RFBASE", StringComparison.OrdinalIgnoreCase) ||
            proto.Equals("RFBase", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (wz.PortalName != null &&
            (wz.PortalName.Equals("DutyBase", StringComparison.OrdinalIgnoreCase) ||
             wz.PortalName.Equals("RFBASE", StringComparison.OrdinalIgnoreCase) ||
             wz.PortalName.Equals("RFBase", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    private bool IsCriBase(EntityUid uid, WarZoneComponent wz)
    {
        if (MetaData(uid).EntityPrototype?.ID.Equals("WarZoneEntity_FreedomBase", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        var proto = wz.ZoneProto.Id;
        if (proto.Equals("FreedomBase", StringComparison.OrdinalIgnoreCase) ||
            proto.Equals("CRIBASE", StringComparison.OrdinalIgnoreCase) ||
            proto.Equals("CRIBase", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (wz.PortalName != null &&
            (wz.PortalName.Equals("FreedomBase", StringComparison.OrdinalIgnoreCase) ||
             wz.PortalName.Equals("CRIBASE", StringComparison.OrdinalIgnoreCase) ||
             wz.PortalName.Equals("CRIBase", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    private static bool IsRfBaseCaptured(WarZoneComponent wz)
    {
        if (!wz.InitialLoadComplete)
            return false;

        if (string.IsNullOrEmpty(wz.DefendingBandProtoId) && string.IsNullOrEmpty(wz.DefendingFactionProtoId))
            return false;

        if (!string.IsNullOrEmpty(wz.DefendingBandProtoId) &&
            !wz.DefendingBandProtoId.Equals("RFBand", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (wz.CaptureProgress >= 0.999f &&
            !string.IsNullOrEmpty(wz.CurrentAttackerBandProtoId) &&
            !wz.CurrentAttackerBandProtoId.Equals("RFBand", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool IsCriBaseCaptured(WarZoneComponent wz)
    {
        if (!wz.InitialLoadComplete)
            return false;

        if (string.IsNullOrEmpty(wz.DefendingBandProtoId) && string.IsNullOrEmpty(wz.DefendingFactionProtoId))
            return false;

        if (!string.IsNullOrEmpty(wz.DefendingBandProtoId) &&
            !wz.DefendingBandProtoId.Equals("CRIBand", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (wz.CaptureProgress >= 0.999f &&
            !string.IsNullOrEmpty(wz.CurrentAttackerBandProtoId) &&
            !wz.CurrentAttackerBandProtoId.Equals("CRIBand", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
