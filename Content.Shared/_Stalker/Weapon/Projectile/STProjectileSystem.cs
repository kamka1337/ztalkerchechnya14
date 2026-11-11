using Content.Shared._Stalker.Random;
using Content.Shared._Stalker.Weapon.Evasion;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared._Stalker.Weapon.Projectile;

public sealed class STProjectileSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly STEvasionSystem _evasion = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<STProjectileDamageFalloffComponent, MapInitEvent>(OnFalloffProjectileMapInit);
        SubscribeLocalEvent<STProjectileDamageFalloffComponent, ProjectileHitEvent>(OnFalloffProjectileHit);

        SubscribeLocalEvent<STProjectileAccuracyComponent, MapInitEvent>(OnProjectileAccuracyMapInit);
        SubscribeLocalEvent<STProjectileAccuracyComponent, PreventCollideEvent>(OnProjectileAccuracyPreventCollide);

        SubscribeLocalEvent<STProjectilePassThroughComponent, PreventCollideEvent>(OnPassThroughPreventCollide);
    }

    public void SetProjectileFalloffWeaponModifier(Entity<STProjectileDamageFalloffComponent> projectile, float modifier)
    {
        projectile.Comp.WeaponModifier = modifier;
        Dirty(projectile);
    }

    private void OnFalloffProjectileMapInit(Entity<STProjectileDamageFalloffComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.StartCoordinates = _transform.GetMoverCoordinates(ent.Owner);
        Dirty(ent);
    }

    private void OnFalloffProjectileHit(Entity<STProjectileDamageFalloffComponent> ent, ref ProjectileHitEvent args)
    {
        if (ent.Comp.StartCoordinates is null || ent.Comp.MinRemainingDamageModifier < 0)
            return;

        // stalker-changes: use map coordinates to avoid cross-grid distance errors
        var startMapPos = _transform.ToMapCoordinates(ent.Comp.StartCoordinates.Value);
        var targetMapPos = _transform.GetMapCoordinates(args.Target);
        if (startMapPos.MapId != targetMapPos.MapId)
            return;
        var distance = (targetMapPos.Position - startMapPos.Position).Length();
        var minDamage = args.Damage.GetTotal() * ent.Comp.MinRemainingDamageModifier;

        foreach (var threshold in ent.Comp.Thresholds)
        {
            var range = distance - threshold.Range;
            if (range <= 0)
                continue;

            var totalDamage = args.Damage.GetTotal();
            if (totalDamage <= minDamage)
                break;

            var extraModifier = threshold.IgnoreModifiers ? 1 : ent.Comp.WeaponModifier;
            var minModifier = FixedPoint2.Min(minDamage / totalDamage, 1);
            args.Damage *= FixedPoint2.Clamp((totalDamage - range * threshold.Falloff * extraModifier) / totalDamage, minModifier, 1);
        }
    }

    private void OnProjectileAccuracyMapInit(Entity<STProjectileAccuracyComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.StartCoordinates = _transform.GetMoverCoordinates(ent.Owner);
        ent.Comp.Tick = _timing.CurTick.Value;
        Dirty(ent);
    }

    private void OnProjectileAccuracyPreventCollide(Entity<STProjectileAccuracyComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled || ent.Comp.ForceHit || ent.Comp.StartCoordinates is null)
            return;

        if (!HasComp<STEvasionComponent>(args.OtherEntity))
            return;

        var accuracy = ent.Comp.Accuracy;
        var targetCoordinates = _transform.GetMoverCoordinates(args.OtherEntity);
        var distance = (targetCoordinates.Position - ent.Comp.StartCoordinates.Value.Position).Length();

        foreach (var threshold in ent.Comp.Thresholds)
        {
            accuracy += CalculateFalloff(distance - threshold.Range, threshold.Falloff, threshold.AccuracyGrowth);
        }

        accuracy -= _evasion.GetEvasion(args.OtherEntity);
        accuracy = accuracy > ent.Comp.MinAccuracy ? accuracy : ent.Comp.MinAccuracy;

        var random = new STXoshiro128P(ent.Comp.GunSeed, ((long) ent.Comp.Tick << 32) | (uint) GetNetEntity(args.OtherEntity).Id).NextFloat(0f, 1f);
        if (accuracy >= random)
            return;

        args.Cancelled = true;
    }

    private static float CalculateFalloff(float range, float falloff, bool accuracyGrowth)
    {
        if (accuracyGrowth)
            return range >= 0 ? 0 : falloff * range;

        return range <= 0 ? 0 : -falloff * range;
    }

    private void OnPassThroughPreventCollide(Entity<STProjectilePassThroughComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled || !args.OurFixture.Hard)
            return;

        if (!TryComp<FixturesComponent>(args.OtherEntity, out var fixtures) ||
            !fixtures.Fixtures.TryGetValue(SharedProjectileSystem.ProjectileFixture, out var projectileFixture) ||
            !ReferenceEquals(args.OtherFixture, projectileFixture) ||
            !TryComp<ProjectileComponent>(args.OtherEntity, out var projectile))
            return;

        if (projectile.ProjectileSpent || projectile is { Weapon: null, OnlyCollideWhenShot: true })
            return;

        var startCoordinates = projectile.StartCoordinates;
        if (startCoordinates is null)
        {
            if (projectile.Shooter is not { } shooter || TerminatingOrDeleted(shooter))
                return;

            startCoordinates = _transform.GetMoverCoordinates(shooter);
        }

        var startMapPos = _transform.ToMapCoordinates(startCoordinates.Value);
        var coverMapPos = _transform.GetMapCoordinates(ent.Owner);
        if (startMapPos.MapId != coverMapPos.MapId)
            return;

        var distance = (coverMapPos.Position - startMapPos.Position).Length();
        var chance = CalculatePassThroughChance(ent.Comp, distance);
        if (chance <= 0f)
            return;

        if (chance >= 1f)
        {
            args.Cancelled = true;
            return;
        }

        var projectileNetId = GetNetEntity(args.OtherEntity).Id;
        var coverNetId = GetNetEntity(ent.Owner).Id;
        var s0 = ((long) projectile.StartTick << 32) | (uint) projectileNetId;
        var s1 = ((long) projectileNetId << 32) | (uint) coverNetId;
        var mixed = new STSplitMix64(s0).Next() ^ new STSplitMix64(s1).Next();
        var roll = (float) ((ulong) new STSplitMix64(mixed).Next() / (double) ulong.MaxValue);

        if (roll <= chance)
            args.Cancelled = true;
    }

    /// <summary>
    /// Computes the probability (0..1) of a projectile passing through cover at the given distance in tiles.
    /// </summary>
    public static float CalculatePassThroughChance(STProjectilePassThroughComponent comp, float distance)
    {
        return CalculatePassThroughChance(
            distance,
            comp.PointBlankRange,
            comp.PointBlankChance,
            comp.ReferenceRange,
            comp.ReferenceChance,
            comp.MinChance);
    }

    /// <summary>
    /// Computes the probability (0..1) of a projectile passing through cover at the given distance in tiles.
    /// </summary>
    public static float CalculatePassThroughChance(
        float distance,
        float pointBlankRange,
        float pointBlankChance,
        float referenceRange,
        float referenceChance,
        float minChance)
    {
        var clampedMin = Math.Clamp(minChance, 0f, 1f);
        var clampedPointBlank = Math.Clamp(pointBlankChance, clampedMin, 1f);

        if (distance <= pointBlankRange)
            return clampedPointBlank;

        var clampedReference = Math.Clamp(referenceChance, clampedMin, 1f);
        if (referenceRange <= pointBlankRange)
            return clampedReference;

        var slope = (clampedPointBlank - clampedReference) / (referenceRange - pointBlankRange);
        var chance = clampedPointBlank - slope * (distance - pointBlankRange);
        return Math.Clamp(chance, clampedMin, 1f);
    }
}
