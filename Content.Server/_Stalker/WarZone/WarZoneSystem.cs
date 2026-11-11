using System.Threading.Tasks;
using Content.Shared._Stalker.WarZone.Requirenments;
using Robust.Shared.Player;
using Content.Server.Database;
using Content.Shared._Stalker.WarZone;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Physics.Events;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared._Stalker.Bands;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Chat;
using Content.Server.Chat.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using System.Linq;

namespace Content.Server._Stalker.WarZone;

public sealed partial class WarZoneSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    private readonly Dictionary<string, float> _bandPoints = new();
    private readonly Dictionary<string, float> _factionPoints = new();
    private readonly Dictionary<EntityUid, TimeSpan> _lastRewardTimes = new();

    public IReadOnlyDictionary<string, float> BandPoints => _bandPoints;
    public IReadOnlyDictionary<string, float> FactionPoints => _factionPoints;
    public TimeSpan CurrentTime => _gameTiming.CurTime;

    public void SetBandPoints(string protoId, float points)
    {
        _bandPoints[protoId] = points;
    }

    public void SetFactionPoints(string protoId, float points)
    {
        _factionPoints[protoId] = points;
    }

    /// <summary>
    /// Gets the current points for a specific band.
    /// </summary>
    /// <param name="bandProtoId">The prototype ID of the band.</param>
    /// <returns>The points for the band, or 0 if the band is not tracked.</returns>
    public float GetBandPoints(string bandProtoId)
    {
        return _bandPoints.TryGetValue(bandProtoId, out var points) ? points : 0f;
    }

    /// <summary>
    /// Attempts to modify the points for a specific band by a given delta.
    /// Updates the internal cache and persists the change to the database.
    /// Use this instead of SetBandPoints for incremental changes.
    /// </summary>
    /// <param name="bandProtoId">The prototype ID of the band.</param>
    /// <param name="delta">The amount to change the points by (can be negative).</param>
    /// <returns>True if the points were successfully modified (even if clamped), false if the band prototype ID is invalid.</returns>
    public bool TryModifyBandPoints(string bandProtoId, float delta)
    {
        // Check if the band exists in prototypes to avoid adding points for invalid bands
        if (!_prototypeManager.HasIndex<STBandPrototype>(bandProtoId))
        {
            Logger.WarningS("warzone", $"Attempted to modify points for non-existent band prototype ID: {bandProtoId}");
            return false;
        }

        var currentPoints = GetBandPoints(bandProtoId);
        var newPoints = currentPoints + delta;

        // Prevent points from going below zero? Or allow debt? Assuming non-negative for now.
        if (newPoints < 0)
        {
             Logger.InfoS("warzone", $"Attempted to set points below zero for band {bandProtoId}. Clamping to 0.");
             newPoints = 0; // Clamp to zero if modification results in negative points
        }


        _bandPoints[bandProtoId] = newPoints;

        // Persist the change to the database asynchronously.
        // We don't necessarily need to wait for this to complete before returning true,
        // as the in-memory value is updated. Error handling for DB failure might be needed.
        _dbManager.SetStalkerBandAsync(new ProtoId<STBandPrototype>(bandProtoId), newPoints);

        Logger.DebugS("warzone", $"Modified points for band {bandProtoId} by {delta}. New total: {newPoints}");
        return true;
    }

    // Note: SetBandPoints and SetFactionPoints are still useful for direct setting, like in admin commands or initialization.

    public IEnumerable<(EntityUid Uid, WarZoneComponent Component)> GetAllWarZones()
    {
        var query = EntityQueryEnumerator<WarZoneComponent>();
        while (query.MoveNext(out var uid, out var comp))
            yield return (uid, comp);
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WarZoneComponent, ComponentInit>(OnWarZoneInit);
        SubscribeLocalEvent<WarZoneComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<WarZoneComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<MetaDataComponent, EntityTerminatingEvent>(OnEntityTerminating);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnWarZoneInit(EntityUid uid, WarZoneComponent component, ComponentInit args)
    {
        _ = InitializeWarZoneAsync(uid, component);
    }

    //SESSION-STALKER EDIT
    private async Task InitializeWarZoneAsync(EntityUid uid, WarZoneComponent component)
    {
        try
        {
            foreach (var bandProto in _prototypeManager.EnumeratePrototypes<STBandPrototype>())
            {
                _bandPoints[bandProto.ID] = 0;
            }

            foreach (var factionProto in _prototypeManager.EnumeratePrototypes<NpcFactionPrototype>())
            {
                _factionPoints[factionProto.ID] = 0;
            }

            component.InitialLoadComplete = false;
            component.PresentBandProtoIds = new();
            component.PresentFactionProtoIds = new();
            component.PresentEntities = new();
            component.PresentBandCounts = new();
            component.PresentFactionCounts = new();

            _ = LoadInitialZoneStateAsync(uid, component, null);
        }
        catch (Exception ex)
        {
            Logger.ErrorS("warzone", $"Error initializing war zone {component.ZoneProto}: {ex}");
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _gameTiming.CurTime;

        var query = EntityQueryEnumerator<WarZoneComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Check if enough time has passed since the last check for this zone
            if (now < comp.NextCheckTime)
                continue;

            // Schedule the next check time
            comp.NextCheckTime = now + TimeSpan.FromSeconds(1);

            // Run the capture update logic, passing 1.0f as the effective frame time
            _ = UpdateCaptureAsync(uid, comp, now, 1.0f);
        }

        // Distribute rewards separately
        foreach (var (zone, lastRewardTime) in _lastRewardTimes)
        {
            DistributeRewards(zone, lastRewardTime, now);
        }
    }

    private void ResetCaptureProgress(WarZoneComponent comp)
    {
        comp.CaptureProgressTime = 0f;
        comp.CaptureProgress = 0f;
        comp.LastAnnouncedProgressStep = 0;
        comp.CaptureInProgress = false;
    }

    private async Task UpdateCaptureAsync(EntityUid zone, WarZoneComponent comp, TimeSpan now, float effectiveFrameTime)
    {
        if (!comp.InitialLoadComplete || !_prototypeManager.TryIndex<STWarZonePrototype>(comp.ZoneProto, out var wzProto))
            return;

        // Clean up any dead, terminating or invalid entities from PresentEntities
        if (comp.PresentEntities != null && comp.PresentEntities.Count > 0)
        {
            List<EntityUid>? invalid = null;
            foreach (var ent in comp.PresentEntities)
            {
                if (!IsEligibleCapturer(ent))
                {
                    invalid ??= new();
                    invalid.Add(ent);
                }
            }

            if (invalid != null)
            {
                foreach (var ent in invalid)
                {
                    string? bId = null;
                    string? fId = null;
                    if (_entityManager.TryGetComponent(ent, out BandsComponent? b) &&
                        _prototypeManager.TryIndex<STBandPrototype>(b.BandProto, out var bp))
                    {
                        bId = bp.ID;
                        fId = bp.FactionId;
                    }
                    RemoveEntityFromCaptureZone(zone, comp, ent, bId, fId);
                }
            }
        }

        // Determine Present Bands and Factions
        var presentBands = comp.PresentBandProtoIds;
        var presentFactions = new HashSet<string>();
        if (presentBands != null)
        {
            foreach (var bandId in presentBands)
            {
                if (_prototypeManager.TryIndex<STBandPrototype>(bandId, out var bandProto) && bandProto.FactionId != default)
                {
                    presentFactions.Add(bandProto.FactionId);
                }
            }
        }

        EntityUid? feedbackEntity = GetFirstEntity(comp.PresentEntities);

        // Scenario 1: No Bands Present
        if (presentBands == null || presentBands.Count == 0)
        {
            if (comp.CaptureInProgress && (comp.CurrentAttackerBandProtoId != null || comp.CurrentAttackerFactionProtoId != null))
            {
                AnnounceCaptureAbandonedLocal(zone, comp, "No attackers present");
                comp.CaptureInProgress = false;
            }
            // Capture progress does NOT reset when interrupted.
            return;
        }

        // Scenario 2: Multiple Factions Present (Conflict / Contested)
        if (presentFactions.Count > 1)
        {
            if (comp.CaptureInProgress && (comp.CurrentAttackerBandProtoId != null || comp.CurrentAttackerFactionProtoId != null))
            {
                AnnounceCaptureAbandonedLocal(zone, comp, "Conflicting forces present");
                comp.CaptureInProgress = false;
            }
            // Conflict pauses capture progress without resetting it.
            return;
        }

        // Scenario 3: Single Faction Present
        string? singlePresentBand = presentBands.Count == 1 ? GetFirst(presentBands) : null;
        string? singlePresentFaction = GetFirst(presentFactions);

        // Check if the present faction/band is the current defender / ally
        bool isDefender = false;
        if (!string.IsNullOrEmpty(comp.DefendingBandProtoId))
        {
            if (singlePresentBand != null && singlePresentBand == comp.DefendingBandProtoId)
            {
                isDefender = true;
            }
            else if (singlePresentFaction != null &&
                     _prototypeManager.TryIndex<STBandPrototype>(comp.DefendingBandProtoId, out var defBandProto) &&
                     defBandProto.FactionId == singlePresentFaction)
            {
                isDefender = true;
            }
        }
        else if (!string.IsNullOrEmpty(comp.DefendingFactionProtoId) && singlePresentFaction != null)
        {
            if (singlePresentFaction == comp.DefendingFactionProtoId)
            {
                isDefender = true;
            }
        }

        // Defender standing on their own flag: roll back enemy capture progress
        if (isDefender)
        {
            comp.CaptureInProgress = false;

            if (comp.CaptureProgressTime > 0f)
            {
                comp.CaptureProgressTime = MathF.Max(0f, comp.CaptureProgressTime - effectiveFrameTime);
                comp.CaptureProgress = Math.Clamp(comp.CaptureProgressTime / wzProto.CaptureTime, 0f, 1f);

                var step = (int)Math.Ceiling(comp.CaptureProgress * 10);
                if (step < comp.LastAnnouncedProgressStep)
                {
                    comp.LastAnnouncedProgressStep = step;
                    if (feedbackEntity.HasValue)
                        AnnounceCaptureProgressLocal(zone, comp, (int)(comp.CaptureProgress * 100));
                }

                if (comp.CaptureProgressTime <= 0f)
                {
                    ResetCaptureProgress(comp);
                    comp.CurrentAttackerBandProtoId = null;
                    comp.CurrentAttackerFactionProtoId = null;

                    var portalName = comp.PortalName ?? "Unknown";
                    var message = Loc.GetString("st-warzone-capture-restored", ("zone", portalName));
                    var mapCoords = _transformSystem.GetMapCoordinates(zone);
                    var filter = Filter.Empty().AddInRange(mapCoords, ChatSystem.VoiceRange);
                    _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Emotes, message, message, zone, false, true, colorOverride: null);
                }
            }

            return;
        }

        // Attackers present
        string? currentAttackerBand = null;
        string? currentAttackerFaction = null;
        bool awardToFaction = false;

        if (presentBands.Count == 1)
        {
            currentAttackerBand = singlePresentBand;
            if (currentAttackerBand != null && _prototypeManager.TryIndex<STBandPrototype>(currentAttackerBand, out var bandProto))
            {
                currentAttackerFaction = bandProto.FactionId;
                awardToFaction = false;
            }
        }
        else
        {
            currentAttackerFaction = singlePresentFaction;
            currentAttackerBand = null;
            awardToFaction = true;
        }

        if (currentAttackerBand == null && currentAttackerFaction == null)
        {
            Logger.WarningS("warzone", $"Zone '{comp.PortalName}': Capture logic reached invalid state. Bands: {presentBands.Count}, Factions: {presentFactions.Count}.");
            return;
        }

        // Check Cooldown
        bool zoneCooldownActive = comp.CooldownEndTime.HasValue && now < comp.CooldownEndTime.Value;
        if (zoneCooldownActive)
        {
            if (feedbackEntity.HasValue && comp.CooldownEndTime.HasValue)
            {
                var remainingTime = comp.CooldownEndTime.Value - now;
                var portalName = comp.PortalName ?? "Unknown Zone";
                var message = Loc.GetString("st-warzone-cooldown", ("zone", portalName), ("minutes", $"{remainingTime.TotalMinutes:F1}"));
                _popup.PopupEntity(message, feedbackEntity.Value);
            }
            return;
        }

        // Check for New Attacker (Band or Faction)
        bool isNewAttacker = (currentAttackerBand != comp.CurrentAttackerBandProtoId || currentAttackerFaction != comp.CurrentAttackerFactionProtoId);
        if (isNewAttacker)
        {
            ResetCaptureProgress(comp);
            comp.CurrentAttackerBandProtoId = currentAttackerBand;
            comp.CurrentAttackerFactionProtoId = currentAttackerFaction;
            string attackerName = GetAttackerName(currentAttackerBand, currentAttackerFaction);
            AnnounceCaptureStartedLocal(zone, comp, attackerName);
        }

        comp.CaptureInProgress = true;

        // Check Requirements
        var ownerships = new Dictionary<ProtoId<STWarZonePrototype>, (string? BandProtoId, string? FactionProtoId)>();
        var lastCaptureTimes = new Dictionary<ProtoId<STWarZonePrototype>, DateTime?>();
        var zonePrototypes = new Dictionary<ProtoId<STWarZonePrototype>, STWarZonePrototype>();
        var requiredZoneIds = wzProto.Requirements?.OfType<ZoneOwnershipRequirenment>().SelectMany(r => r.RequiredZones).ToHashSet() ?? new HashSet<ProtoId<STWarZonePrototype>>();

        foreach (var rid in requiredZoneIds)
        {
            var ownership = await _dbManager.GetStalkerWarOwnershipAsync(rid);
            if (ownership != null)
            {
                ownerships[rid] = (ownership.Band?.BandProtoId, ownership.Faction?.FactionProtoId);
                lastCaptureTimes[rid] = ownership.LastCapturedByCurrentOwnerAt;
            }
            if (_prototypeManager.TryIndex<STWarZonePrototype>(rid, out var reqProto))
            {
                zonePrototypes[rid] = reqProto;
            }
        }

        Action<EntityUid, string, (string, object)[]?> feedbackCallback =
            (entity, locId, args) => _popup.PopupEntity(Loc.GetString(locId, args ?? Array.Empty<(string, object)>()), entity);

        var allMet = true;
        if (wzProto.Requirements != null)
        {
            foreach (var req in wzProto.Requirements)
            {
                var blockReason = req.Check(
                    currentAttackerBand,
                    currentAttackerFaction,
                    ownerships,
                    lastCaptureTimes,
                    zonePrototypes,
                    comp.ZoneProto,
                    effectiveFrameTime,
                    feedbackEntity,
                    feedbackCallback);

                if (blockReason != CaptureBlockReason.None)
                {
                    allMet = false;
                    break;
                }
            }
        }

        if (!allMet)
        {
            if (isNewAttacker) ResetCaptureProgress(comp);
            return;
        }

        // Update Capture Progress
        comp.CaptureProgressTime += effectiveFrameTime;
        comp.CaptureProgress = Math.Clamp(comp.CaptureProgressTime / wzProto.CaptureTime, 0f, 1f);

        // Announce each 10% increment locally
        var progressStep = (int)(comp.CaptureProgress * 10);
        if (progressStep > comp.LastAnnouncedProgressStep)
        {
            comp.LastAnnouncedProgressStep = progressStep;
            if (feedbackEntity.HasValue)
                AnnounceCaptureProgressLocal(zone, comp, progressStep * 10);
        }

        // Check for Capture Completion
        if (comp.CaptureProgressTime < wzProto.CaptureTime)
            return;

        // --- Capture Complete ---
        ProtoId<STBandPrototype>? finalBandOwnerId = null;
        ProtoId<NpcFactionPrototype>? finalFactionOwnerId = null;
        string finalOwnerName;

        if (awardToFaction)
        {
            comp.DefendingFactionProtoId = currentAttackerFaction;
            comp.DefendingBandProtoId = null;
            if (currentAttackerFaction != null)
                finalFactionOwnerId = new ProtoId<NpcFactionPrototype>(currentAttackerFaction);
            finalOwnerName = GetAttackerName(null, currentAttackerFaction);
            Logger.InfoS("warzone", $"Zone '{comp.PortalName}' captured by Faction: {currentAttackerFaction}");
        }
        else
        {
            comp.DefendingBandProtoId = currentAttackerBand;
            comp.DefendingFactionProtoId = currentAttackerFaction;
            if (currentAttackerBand != null)
                finalBandOwnerId = new ProtoId<STBandPrototype>(currentAttackerBand);
            finalOwnerName = GetAttackerName(currentAttackerBand, null);
            Logger.InfoS("warzone", $"Zone '{comp.PortalName}' captured by Band: {currentAttackerBand}");
        }

        await _dbManager.SetStalkerZoneOwnershipAsync(
            comp.ZoneProto,
            finalBandOwnerId,
            finalFactionOwnerId);

        if (wzProto.CaptureCooldownHours > 0)
        {
            comp.CooldownEndTime = _gameTiming.CurTime + TimeSpan.FromHours(wzProto.CaptureCooldownHours);
        }

        _chatManager.DispatchServerAnnouncement(Loc.GetString(
            "st-warzone-captured",
            ("zone", comp.PortalName ?? "Unknown"),
            ("attacker", finalOwnerName)));

        _lastRewardTimes[zone] = _gameTiming.CurTime;

        comp.CaptureProgress = 1f;
        comp.CaptureProgressTime = 0f;
        comp.CurrentAttackerBandProtoId = null;
        comp.CurrentAttackerFactionProtoId = null;
        comp.CaptureInProgress = false;
    }

    private void ResetAllRequirements(EntityUid zone)
    {
    }

    private void DistributeRewards(EntityUid zone, TimeSpan lastRewardTime, TimeSpan now)
    {
        if (!_entityManager.TryGetComponent(zone, out WarZoneComponent? wzComp))
            return;

        if (!_prototypeManager.TryIndex<STWarZonePrototype>(wzComp.ZoneProto, out var wzProto))
            return;

        var period = TimeSpan.FromSeconds(wzProto.RewardPeriod);

        if (now - lastRewardTime < period)
            return;

        // Check if anyone is defending
        if (wzComp.DefendingBandProtoId == null && wzComp.DefendingFactionProtoId == null)
        {
            // Only award if ShouldAwardWhenDefenderPresent is true and zone is uncaptured
            if (!wzProto.ShouldAwardWhenDefenderPresent)
                return;
            // If ShouldAwardWhenDefenderPresent is true, proceed with awarding logic below even when uncaptured
        }

        var points = wzProto.RewardPointsPerPeriod;
        bool rewarded = false;

        // Prioritize Band Reward if a band is defending
        if (wzComp.DefendingBandProtoId != null)
        {
            var bandProtoId = wzComp.DefendingBandProtoId;
            var currentPoints = _bandPoints.TryGetValue(bandProtoId, out var val) ? val : 0;
            var newPoints = currentPoints + points;
            _bandPoints[bandProtoId] = newPoints;
            _dbManager.SetStalkerBandAsync(new ProtoId<STBandPrototype>(bandProtoId), newPoints);
            Logger.InfoS("warzone", $"Awarded {points} points to band {bandProtoId} (total: {newPoints}) for controlling {wzComp.PortalName}");
            rewarded = true;
        }
        // Otherwise, reward Faction if a faction is defending (and no band)
        else if (wzComp.DefendingFactionProtoId != null)
        {
            var factionProtoId = wzComp.DefendingFactionProtoId;
            var currentPoints = _factionPoints.TryGetValue(factionProtoId, out var val) ? val : 0;
            var newPoints = currentPoints + points;
            _factionPoints[factionProtoId] = newPoints;
            _dbManager.SetStalkerFactionAsync(new ProtoId<NpcFactionPrototype>(factionProtoId), newPoints);
            Logger.InfoS("warzone", $"Awarded {points} points to faction {factionProtoId} (total: {newPoints}) for controlling {wzComp.PortalName}");
            rewarded = true;
        }

        if (rewarded)
        {
            _lastRewardTimes[zone] = now;
        }
    }

    // Helper to get the first element from a HashSet (order isn't guaranteed but consistent enough here)
    private static string? GetFirst(HashSet<string>? set)
    {
        if (set == null) return null;
        foreach (var g in set)
            return g;
        return null;
    }

    private static EntityUid? GetFirstEntity(HashSet<EntityUid>? set)
    {
         if (set == null) return null;
        foreach (var entity in set)
            return entity;
        return null;
    }

    private bool IsEligibleCapturer(EntityUid entity)
    {
        if (Deleted(entity) || Terminating(entity))
            return false;

        if (!TryComp<MobStateComponent>(entity, out var mobState) || _mobState.IsDead(entity, mobState))
            return false;

        if (!_entityManager.TryGetComponent(entity, out BandsComponent? bands) || bands.BandProto == default)
            return false;

        return true;
    }

    private void AddEntityToCaptureZone(EntityUid zoneUid, WarZoneComponent component, EntityUid entity)
    {
        if (!IsEligibleCapturer(entity))
            return;

        if (!_entityManager.TryGetComponent(entity, out BandsComponent? bands) || bands.BandProto == default)
            return;

        string? bandId = null;
        string? factionId = null;

        if (_prototypeManager.TryIndex<STBandPrototype>(bands.BandProto, out var bandProto))
        {
            bandId = bandProto.ID;
            factionId = bandProto.FactionId;
        }

        component.PresentEntities ??= new();
        component.PresentBandProtoIds ??= new();
        component.PresentFactionProtoIds ??= new();
        component.PresentBandCounts ??= new();
        component.PresentFactionCounts ??= new();

        if (!component.PresentEntities.Add(entity))
            return;

        bool changedState = false;
        if (bandId != null)
        {
            component.PresentBandCounts.TryGetValue(bandId, out var currentBandCount);
            component.PresentBandCounts[bandId] = currentBandCount + 1;
            if (currentBandCount == 0)
            {
                component.PresentBandProtoIds.Add(bandId);
                changedState = true;
            }
        }
        if (factionId != null)
        {
            component.PresentFactionCounts.TryGetValue(factionId, out var currentFactionCount);
            component.PresentFactionCounts[factionId] = currentFactionCount + 1;
            if (currentFactionCount == 0)
            {
                component.PresentFactionProtoIds.Add(factionId);
            }
        }

        if (changedState)
        {
            component.NextCheckTime = TimeSpan.Zero;
        }
    }

    // Collision Handling - Manages counts and presence sets
    private void OnStartCollide(EntityUid uid, WarZoneComponent component, ref readonly StartCollideEvent args)
    {
        AddEntityToCaptureZone(uid, component, args.OtherEntity);
    }

    private void OnEndCollide(EntityUid uid, WarZoneComponent component, ref readonly EndCollideEvent args)
    {
        var other = args.OtherEntity;
        string? bandId = null;
        string? factionId = null;

        if (_entityManager.TryGetComponent(other, out BandsComponent? bands) &&
            bands.BandProto != default &&
            _prototypeManager.TryIndex<STBandPrototype>(bands.BandProto, out var bandProto))
        {
            bandId = bandProto.ID;
            factionId = bandProto.FactionId;
        }

        RemoveEntityFromCaptureZone(uid, component, other, bandId, factionId);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            var query = EntityQueryEnumerator<WarZoneComponent>();
            while (query.MoveNext(out var zoneUid, out var wzComp))
            {
                if (wzComp.PresentEntities != null && wzComp.PresentEntities.Contains(args.Target))
                {
                    string? bandId = null;
                    string? factionId = null;
                    if (_entityManager.TryGetComponent(args.Target, out BandsComponent? bands) &&
                        _prototypeManager.TryIndex<STBandPrototype>(bands.BandProto, out var bandProto))
                    {
                        bandId = bandProto.ID;
                        factionId = bandProto.FactionId;
                    }

                    RemoveEntityFromCaptureZone(zoneUid, wzComp, args.Target, bandId, factionId);
                }
            }
        }
        else if (args.OldMobState == MobState.Dead && args.NewMobState != MobState.Dead)
        {
            if (TryComp<PhysicsComponent>(args.Target, out var physics))
            {
                foreach (var contacting in _physics.GetContactingEntities(args.Target, physics))
                {
                    if (TryComp<WarZoneComponent>(contacting, out var wzComp))
                    {
                        AddEntityToCaptureZone(contacting, wzComp, args.Target);
                    }
                }
            }
        }
    }

    // Entity Termination Handling - Refactored for clarity and safety
    private void OnEntityTerminating(EntityUid uid, MetaDataComponent meta, ref EntityTerminatingEvent args)
    {
        // Check if the terminating entity has a BandsComponent
        if (!_entityManager.TryGetComponent(uid, out BandsComponent? bands) || bands.BandProto == default)
            return; // Not a player/band member we track for captures

        string? bandId = null;
        string? factionId = null;
        if (_prototypeManager.TryIndex<STBandPrototype>(bands.BandProto, out var bandProto))
        {
            bandId = bandProto.ID;
            factionId = bandProto.FactionId;
        }

        // Find all zones this entity might be in and remove it
        var query = EntityQueryEnumerator<WarZoneComponent>();
        while (query.MoveNext(out var zoneUid, out var wzComp))
        {
            RemoveEntityFromCaptureZone(zoneUid, wzComp, uid, bandId, factionId);
        }
    }

    /// <summary>
    /// Helper method to remove a specific entity from a single capture zone's tracking state.
    /// Called by OnEndCollide (indirectly via OnEntityTerminating).
    /// </summary>
    private void RemoveEntityFromCaptureZone(EntityUid zoneUid, WarZoneComponent wzComp, EntityUid entityUid, string? bandId, string? factionId)
    {
        // Check if the component or necessary collections are null before proceeding
        if (wzComp.PresentEntities == null || wzComp.PresentBandProtoIds == null || wzComp.PresentFactionProtoIds == null || wzComp.PresentBandCounts == null || wzComp.PresentFactionCounts == null)
            return;

        // Only process if the entity was actually present
        if (!wzComp.PresentEntities.Remove(entityUid))
            return; // Entity wasn't in this zone's set

        // Update counts and presence sets
        bool changedState = false;
        if (bandId != null && wzComp.PresentBandCounts.TryGetValue(bandId, out var currentBandCount))
        {
            if (currentBandCount > 1)
            {
                wzComp.PresentBandCounts[bandId] = currentBandCount - 1;
            }
            else // Count is 1, will become 0
            {
                wzComp.PresentBandCounts.Remove(bandId);
                if (wzComp.PresentBandProtoIds.Remove(bandId)) // Remove from set only when count reaches zero
                    changedState = true; // Band presence changed from some to none
            }
        }
         else if (bandId != null)
        {
             Logger.WarningS("warzone", $"Terminating entity {entityUid} in zone {zoneUid} had band {bandId}, but band count was not found or already zero.");
        }


        if (factionId != null && wzComp.PresentFactionCounts.TryGetValue(factionId, out var currentFactionCount))
        {
             if (currentFactionCount > 1)
            {
                wzComp.PresentFactionCounts[factionId] = currentFactionCount - 1;
            }
            else // Count is 1, will become 0
            {
                wzComp.PresentFactionCounts.Remove(factionId);
                wzComp.PresentFactionProtoIds.Remove(factionId); // Remove from set only when count reaches zero
                // Note: Faction presence change doesn't trigger immediate update on its own.
            }
        }
        else if (factionId != null)
        {
             Logger.WarningS("warzone", $"Terminating entity {entityUid} in zone {zoneUid} had faction {factionId}, but faction count was not found or already zero.");
        }

        // If the entity's removal caused a band to disappear entirely, force an update check
        if (changedState)
        {
             // Reset NextCheckTime to force an update on the next frame
             wzComp.NextCheckTime = TimeSpan.Zero;
        }
    }

    // Helper to get a display name for the attacker/defender (band or faction)
    private string GetAttackerName(string? bandProtoId, string? factionProtoId)
    {
        // Prioritize Band Name if provided
        if (!string.IsNullOrEmpty(bandProtoId))
        {
            if (_prototypeManager.TryIndex<STBandPrototype>(bandProtoId, out var bandProto))
                return bandProto.Name; // Use LocId if available and localized? For now, just Name.
        }
        else if (!string.IsNullOrEmpty(factionProtoId))
        {
            if (_prototypeManager.TryIndex<NpcFactionPrototype>(factionProtoId, out var factionProto))
                return factionProto.ID;
        }
        return Loc.GetString("st-warzone-unknown-attacker");
    }

    // Announce capture abandonment locally
    private void AnnounceCaptureAbandonedLocal(EntityUid zoneUid, WarZoneComponent? wzComp, string reason = "")
    {
        if (wzComp == null || (wzComp.CurrentAttackerBandProtoId == null && wzComp.CurrentAttackerFactionProtoId == null))
            return;

        // Only announce if the attacker was not the defender (prevents spam if defender leaves/re-enters)
        // Note: This check might need refinement depending on desired announcement behavior
        bool wasAttacking = wzComp.CurrentAttackerBandProtoId != wzComp.DefendingBandProtoId || wzComp.CurrentAttackerFactionProtoId != wzComp.DefendingFactionProtoId;

        if (wasAttacking)
        {
            string attackerName = GetAttackerName(wzComp.CurrentAttackerBandProtoId, wzComp.CurrentAttackerFactionProtoId);
            var message = Loc.GetString("st-warzone-capture-abandoned", ("zone", wzComp.PortalName ?? "Unknown"), ("attacker", attackerName));
            // Optionally add reason: if (!string.IsNullOrEmpty(reason)) message += $" ({reason})";

            var mapCoords = _transformSystem.GetMapCoordinates(zoneUid);
            var filter = Filter.Empty().AddInRange(mapCoords, ChatSystem.VoiceRange); // Use appropriate range
            _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Emotes, message, message, zoneUid, false, true, colorOverride: null); // Consider ChatChannel choice
        }

        // Attacker info is reset in the main UpdateCaptureAsync logic where abandonment is detected
    }

    // Announce capture start locally
    private void AnnounceCaptureStartedLocal(EntityUid zoneUid, WarZoneComponent wzComp, string attackerName)
    {
        var message = Loc.GetString("st-warzone-capture-started", ("attacker", attackerName), ("zone", wzComp.PortalName ?? "Unknown"));
        var mapCoords = _transformSystem.GetMapCoordinates(zoneUid);
        var filter = Filter.Empty().AddInRange(mapCoords, ChatSystem.VoiceRange);
        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Emotes, message, message, zoneUid, false, true, colorOverride: null);

        var isRfBase = wzComp.ZoneProto == "DutyBase" || wzComp.ZoneProto == "RFBASE" || wzComp.PortalName == "RFBase" || wzComp.PortalName == "RFBASE" || wzComp.DefendingBandProtoId == "RFBand";
        var isCriBase = wzComp.ZoneProto == "FreedomBase" || wzComp.ZoneProto == "CRIBASE" || wzComp.PortalName == "CRIBase" || wzComp.PortalName == "CRIBASE" || wzComp.DefendingBandProtoId == "CRIBand";

        if (isRfBase && (wzComp.CurrentAttackerBandProtoId == "CRIBand" || attackerName.Contains("CRI", StringComparison.OrdinalIgnoreCase) || attackerName.Contains("\u0427\u0420\u0418", StringComparison.OrdinalIgnoreCase)))
        {
            _chatManager.DispatchServerAnnouncement("\u0412\u0421 \u0427\u0420\u0418 \u043D\u0430\u0447\u0430\u043B\u0430 \u0437\u0430\u0445\u0432\u0430\u0442 \u0431\u0430\u0437\u044B \u0412\u0421 \u0420\u0424", Color.Red);
        }
        else if (isCriBase && (wzComp.CurrentAttackerBandProtoId == "RFBand" || attackerName.Contains("RF", StringComparison.OrdinalIgnoreCase) || attackerName.Contains("\u0420\u0424", StringComparison.OrdinalIgnoreCase)))
        {
            _chatManager.DispatchServerAnnouncement("\u0412\u0421 \u0420\u0424 \u043D\u0430\u0447\u0430\u043B\u0430 \u0437\u0430\u0445\u0432\u0430\u0442 \u0431\u0430\u0437\u044B \u0412\u0421 \u0427\u0420\u0418", Color.Red);
        }
    }


    private void AnnounceCaptureProgressLocal(EntityUid zoneUid, WarZoneComponent wzComp, int percent)
    {
        var portalName = wzComp.PortalName ?? "Unknown";
        var message = Loc.GetString("st-warzone-progress", ("zone", portalName), ("percent", percent));
        var mapCoords = _transformSystem.GetMapCoordinates(zoneUid);
        var filter = Filter.Empty().AddInRange(mapCoords, ChatSystem.VoiceRange);
        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Emotes, message, message, zoneUid, false, true, colorOverride: null);
    }
    // Load initial zone ownership and cooldown state from pre-fetched DB data
    private async Task LoadInitialZoneStateAsync(EntityUid zoneUid, WarZoneComponent component, StalkerZoneOwnership? ownership) // Added ownership parameter
    {
        try
        {
            if (component.ZoneProto == default ||
                !_prototypeManager.TryIndex<STWarZonePrototype>(component.ZoneProto, out var wzProto))
            {
                Logger.ErrorS("warzone", $"Could not find STWarZonePrototype with ID '{component.ZoneProto}' during async state load for zone {zoneUid}.");
                component.InitialLoadComplete = true;
                return;
            }

            if (ownership != null)
            {
                Logger.InfoS("warzone", $"Zone '{component.PortalName}' ({component.ZoneProto}): Loading ownership. BandProtoId from DB record: '{ownership.Band?.BandProtoId ?? "NULL"}', FactionProtoId from DB record: '{ownership.Faction?.FactionProtoId ?? "NULL"}'");
                component.DefendingBandProtoId = ownership.Band?.BandProtoId;
                component.DefendingFactionProtoId = ownership.Faction?.FactionProtoId;
                Logger.InfoS("warzone", $"Zone '{component.PortalName}' ({component.ZoneProto}): Loaded ownership from init. Assigned DefendingBandProtoId: '{component.DefendingBandProtoId ?? "NULL"}', DefendingFactionProtoId: '{component.DefendingFactionProtoId ?? "NULL"}'");

                // Load cooldown state
                if (ownership.LastCapturedByCurrentOwnerAt.HasValue && wzProto.CaptureCooldownHours > 0)
                {
                    DateTime captureTime = ownership.LastCapturedByCurrentOwnerAt.Value;
                    DateTime cooldownEndDateTime = captureTime.AddHours(wzProto.CaptureCooldownHours);
                    DateTime currentDateTime = DateTime.UtcNow;

                    if (cooldownEndDateTime > currentDateTime)
                    {
                        TimeSpan remainingCooldown = cooldownEndDateTime - currentDateTime;
                        component.CooldownEndTime = _gameTiming.CurTime + remainingCooldown;
                         Logger.InfoS("warzone", $"Zone '{component.PortalName}' loaded with active cooldown: {remainingCooldown.TotalMinutes:F1} minutes remaining.");
                    }
                }
                 Logger.InfoS("warzone", $"Zone '{component.PortalName}' loaded ownership. Band: {component.DefendingBandProtoId ?? "None"}, Faction: {component.DefendingFactionProtoId ?? "None"}");
            }
             else
             {
                 Logger.InfoS("warzone", $"Zone '{component.PortalName}' ({component.ZoneProto}): No ownership record passed from init.");
             }

            component.InitialLoadComplete = true;
        }
        catch (Exception ex)
        {
            component.InitialLoadComplete = true; // Ensure load completes even on error
            Logger.ErrorS("warzone", $"Exception during async zone state load for {zoneUid} ({component.ZoneProto}): {ex}");
        }
    }
}
