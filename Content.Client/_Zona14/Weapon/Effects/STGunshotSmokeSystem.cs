using System.Numerics;
using Content.Shared._Zona14.Weapon.Effects;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client._Zona14.Weapon.Effects;

/// <summary>
/// Spawns and animates detailed client-side gunshot smoke clouds for configured firearms.
/// </summary>
public sealed class STGunshotSmokeSystem : EntitySystem
{
    private const string AnimationKey = "st-gunshot-smoke";
    private const string SmokeLayerKey = "base";
    private const string MuzzleSlotId = "gun_module_muzzle";

    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<STGunshotSmokeComponent, STGunshotMuzzleEffectEvent>(OnGunshotMuzzleEffect);
    }

    private void OnGunshotMuzzleEffect(Entity<STGunshotSmokeComponent> ent, ref STGunshotMuzzleEffectEvent args)
    {
        if (!TryGetSpawnOrigin(ent.Owner, out var baseCoordinates))
            return;

        var isSuppressed = _container.TryGetContainer(ent.Owner, MuzzleSlotId, out var muzzleSlot)
                           && muzzleSlot.ContainedEntities.Count > 0;

        var offsetBonus = isSuppressed ? ent.Comp.SuppressedMuzzleOffsetBonus : 0f;
        var scaleMult = isSuppressed ? ent.Comp.SuppressedScaleMultiplier : 1f;
        var alphaMult = isSuppressed ? ent.Comp.SuppressedAlphaMultiplier : 1f;

        SpawnSmokePuff(
            ent.Comp.EffectProto,
            baseCoordinates,
            args.WorldAngle,
            ent.Comp.MuzzleOffset + offsetBonus,
            scaleMult,
            alphaMult);

        if (ent.Comp.SecondaryEffectProto is { } secondaryProto)
        {
            SpawnSmokePuff(
                secondaryProto,
                baseCoordinates,
                args.WorldAngle,
                ent.Comp.SecondaryOffset + offsetBonus,
                scaleMult,
                alphaMult * 0.85f);
        }
    }

    private bool TryGetSpawnOrigin(EntityUid gunUid, out EntityCoordinates coordinates)
    {
        var gunXform = Transform(gunUid);
        var gridUid = gunXform.GridUid;

        if (TryComp(gridUid, out MapGridComponent? mapGrid))
        {
            coordinates = new EntityCoordinates(gridUid.Value, _maps.LocalToGrid(gridUid.Value, mapGrid, gunXform.Coordinates));
            return true;
        }

        if (gunXform.MapUid != null)
        {
            coordinates = new EntityCoordinates(gunXform.MapUid.Value, _transform.GetWorldPosition(gunXform));
            return true;
        }

        coordinates = default;
        return false;
    }

    private void SpawnSmokePuff(
        EntProtoId proto,
        EntityCoordinates originCoords,
        Angle shotAngle,
        float muzzleOffset,
        float scaleMultiplier,
        float alphaMultiplier)
    {
        Vector2 localOffset;
        if (TryComp(originCoords.EntityId, out TransformComponent? originXform))
        {
            var localAngle = shotAngle - _transform.GetWorldRotation(originXform);
            localOffset = localAngle.RotateVec(new Vector2(muzzleOffset, 0f));
        }
        else
        {
            localOffset = shotAngle.RotateVec(new Vector2(muzzleOffset, 0f));
        }

        var spawnCoords = originCoords.Offset(localOffset);
        var smokeEnt = Spawn(proto, spawnCoords);

        if (!TryComp<STGunshotSmokeEffectComponent>(smokeEnt, out var effect) ||
            !TryComp<SpriteComponent>(smokeEnt, out var sprite))
        {
            QueueDel(smokeEnt);
            return;
        }

        var jitterAngle = Angle.FromDegrees(_random.NextFloat(-effect.AngleJitterDegrees, effect.AngleJitterDegrees));
        _transform.SetWorldRotationNoLerp(smokeEnt, shotAngle + jitterAngle);

        string? selectedState = null;
        var hasLayer = _sprite.LayerMapTryGet((smokeEnt, sprite), SmokeLayerKey, out var layerIdx, false);
        if (effect.States.Count > 0 && hasLayer)
        {
            selectedState = _random.Pick(effect.States);
            _sprite.LayerSetAutoAnimated((smokeEnt, sprite), layerIdx, false);
            _sprite.LayerSetRsiState((smokeEnt, sprite), layerIdx, selectedState);
        }

        var scaleFactor = (1f + _random.NextFloat(-effect.ScaleJitter, effect.ScaleJitter)) * scaleMultiplier;
        var startScale = effect.StartScale * scaleFactor;
        var endScale = effect.EndScale * scaleFactor;
        var midScale = Vector2.Lerp(startScale, endScale, 0.55f);

        var lateral = _random.NextFloat(-effect.LateralDrift, effect.LateralDrift) * scaleMultiplier;
        var drift = effect.DriftDistance * scaleMultiplier;
        var midOffset = new Vector2(drift * 0.62f, lateral * 0.45f);
        var endOffset = new Vector2(drift, lateral);

        var baseColor = sprite.Color;
        var startAlpha = Math.Clamp(effect.StartAlpha * alphaMultiplier, 0f, 1f);
        var startColor = baseColor.WithAlpha(startAlpha);
        var midColor = baseColor.WithAlpha(startAlpha * 0.78f);
        var endColor = baseColor.WithAlpha(0f);

        var lifetime = MathF.Max(0.1f, effect.Lifetime);
        _sprite.SetScale((smokeEnt, sprite), startScale);
        _sprite.SetColor((smokeEnt, sprite), startColor);

        var anim = new Animation
        {
            Length = TimeSpan.FromSeconds(lifetime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(midOffset, lifetime * 0.28f),
                        new AnimationTrackProperty.KeyFrame(endOffset, lifetime),
                    }
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startScale, 0f),
                        new AnimationTrackProperty.KeyFrame(midScale, lifetime * 0.28f),
                        new AnimationTrackProperty.KeyFrame(endScale, lifetime),
                    }
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startColor, 0f),
                        new AnimationTrackProperty.KeyFrame(midColor, lifetime * 0.30f),
                        new AnimationTrackProperty.KeyFrame(endColor, lifetime),
                    }
                },
            }
        };

        if (selectedState != null && hasLayer)
        {
            anim.AnimationTracks.Add(new AnimationTrackSpriteFlick
            {
                LayerKey = SmokeLayerKey,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(selectedState, 0f),
                }
            });
        }

        _animPlayer.Play(smokeEnt, anim, AnimationKey);
    }
}
