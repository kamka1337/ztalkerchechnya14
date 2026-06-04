using System.Linq;
using Content.Shared._Stalker.ZoneAnomaly.Components;
using Content.Shared.Traits.Assorted;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Rejuvenate;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Session.LimbHealth;

public sealed class LimbHealthSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedBodyTargetingSystem _targeting = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private bool _syncing;

    private EntityUid? _bulletHitTarget;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimbHealthComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LimbHealthComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
        SubscribeLocalEvent<LimbHealthComponent, RejuvenateEvent>(OnRejuvenate,
            before: new[] { typeof(DamageableSystem) });
        SubscribeLocalEvent<LimbHealthComponent, UpdateMobStateEvent>(OnUpdateMobState,
            after: new[] { typeof(MobThresholdSystem) });
        SubscribeLocalEvent<ProjectileComponent, ProjectileHitEvent>(OnProjectileHit);

        SubscribeLocalEvent<ZoneAnomalyComponent, ZoneAnomalyEntityAddEvent>(OnAnomalyEntityAdd);
        SubscribeLocalEvent<ZoneAnomalyComponent, ZoneAnomalyEntityRemoveEvent>(OnAnomalyEntityRemove);
    }

    private void OnAnomalyEntityAdd(Entity<ZoneAnomalyComponent> ent, ref ZoneAnomalyEntityAddEvent args)
    {
        if (!_net.IsServer)
            return;
        if (!HasComp<LimbHealthComponent>(args.Entity))
            return;

        EnsureComp<STInZoneAnomalyComponent>(args.Entity).Sources.Add(ent.Owner);
    }

    private void OnAnomalyEntityRemove(Entity<ZoneAnomalyComponent> ent, ref ZoneAnomalyEntityRemoveEvent args)
    {
        if (!_net.IsServer)
            return;
        if (!TryComp<STInZoneAnomalyComponent>(args.Entity, out var marker))
            return;

        marker.Sources.Remove(ent.Owner);
        if (marker.Sources.Count == 0)
            RemComp<STInZoneAnomalyComponent>(args.Entity);
    }

    private void OnUpdateMobState(Entity<LimbHealthComponent> ent, ref UpdateMobStateEvent args)
    {
        if (ent.Comp.PermanentAsphyxiation)
            args.State = MobState.Dead;
    }

    private void OnProjectileHit(Entity<ProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (_net.IsServer)
            _bulletHitTarget = args.Target;
    }

    private void OnMapInit(Entity<LimbHealthComponent> ent, ref MapInitEvent args)
    {
        InitLimbs(ent.Comp);
        Dirty(ent);
    }

    private void InitLimbs(LimbHealthComponent comp)
    {
        comp.Limbs.Clear();
        foreach (var limb in comp.MaxHealth.Keys)
            comp.Limbs[limb] = new LimbState();
    }

    private void OnRejuvenate(Entity<LimbHealthComponent> ent, ref RejuvenateEvent args)
    {
        if (ent.Comp.PermanentAsphyxiation)
            RemComp<UnrevivableComponent>(ent.Owner);

        InitLimbs(ent.Comp);
        ent.Comp.PermanentAsphyxiation = false;
        ent.Comp.ChestCheckStep = 0;
        ent.Comp.ActiveDoses.Clear();
        ent.Comp.NeedledLimbs.Clear();
        ent.Comp.WeakBleedStopTime.Clear();
        Dirty(ent);
        RefreshVitalsActive(ent.Owner, ent.Comp);
    }

    private void OnBeforeDamage(Entity<LimbHealthComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (_syncing)
            return;

        if (args.Cancelled)
            return;

        args.Cancelled = true;

        if (!_net.IsServer)
            return;

        var isBullet = _bulletHitTarget == ent.Owner;
        _bulletHitTarget = null;

        var raw = new DamageSpecifier(args.Damage);

        if (TryComp<DamageableComponent>(ent.Owner, out var damageable))
        {
            foreach (var type in raw.DamageDict.Keys.ToList())
            {
                if (!damageable.Damage.DamageDict.ContainsKey(type))
                    raw.DamageDict.Remove(type);
            }
        }

        if (raw.Empty)
            return;

        var positive = new DamageSpecifier();
        var negative = new DamageSpecifier();
        foreach (var (type, val) in raw.DamageDict)
        {
            if (val > 0)
                positive.DamageDict[type] = val;
            else if (val < 0)
                negative.DamageDict[type] = val;
        }

        var destroyedNow = new List<LimbType>();

        var dealt = positive.GetTotal() > 0;
        if (dealt)
            RouteDamage(ent, positive, args.Origin, args.IgnoreResistances, args.IgnoreGlobalModifiers,
                args.IgnoreResistors, isBullet, destroyedNow);

        if (!negative.Empty)
            DistributeHeal(ent.Comp, negative);

        RecomputeBody(ent.Owner, ent.Comp, notify: dealt, origin: args.Origin, interruptsDoAfters: true);
        Dirty(ent);

        foreach (var limb in destroyedNow)
            RaiseLocalEvent(new LimbDestroyedEvent(ent.Owner, limb));

        RefreshVitalsActive(ent.Owner, ent.Comp);
    }

    private void RouteDamage(Entity<LimbHealthComponent> ent, DamageSpecifier raw, EntityUid? origin,
        bool ignoreResistances, bool ignoreGlobal, List<EntityUid>? ignoreResistors, bool isBullet,
        List<LimbType> destroyedNow)
    {
        var bleedTrigger = isBullet || HasSharpDamage(raw);

        if (origin is { } attacker && _targeting.TryGetAimed(attacker, out var selected))
        {
            if (IsDestroyedState(ent.Comp, selected))
            {
                var spread = RunVanillaModifiers(ent.Owner, new DamageSpecifier(raw), origin,
                    ~SlotFlags.POCKET, ignoreResistances, ignoreGlobal, ignoreResistors);
                DistributeDamage(ent, spread, destroyedNow);
                return;
            }

            var connected = _random.Prob(selected.HitChance());
            var target = connected ? selected : PickOtherAlive(ent.Comp, selected);

            var incoming = new DamageSpecifier(raw);
            if (connected)
            {
                var mult = target.AimedMultiplier();
                if (mult != 1f)
                    incoming *= mult;
            }

            var dmg = RunVanillaModifiers(ent.Owner, incoming, origin,
                SlotsForLimb(target), ignoreResistances, ignoreGlobal, ignoreResistors);

            if (dmg.GetTotal() > 0)
            {
                ApplyToLimb(ent, target, dmg, destroyedNow);
                if (bleedTrigger)
                    RollBleed(ent, target);
            }
            return;
        }

        if (origin == null)
        {
            if (IsStandingInHazard(ent.Owner) && PickAliveLeg(ent.Comp) is { } leg)
            {
                var legDmg = RunVanillaModifiers(ent.Owner, new DamageSpecifier(raw), origin,
                    SlotsForLimb(leg), ignoreResistances, ignoreGlobal, ignoreResistors);

                if (legDmg.GetTotal() > 0)
                {
                    ApplyToLimb(ent, leg, legDmg, destroyedNow);
                    if (bleedTrigger)
                        RollBleed(ent, leg);
                }
                return;
            }

            if (!HasImpactDamage(raw))
            {
                var bodyDmg = RunVanillaModifiers(ent.Owner, new DamageSpecifier(raw), origin,
                    ~SlotFlags.POCKET, ignoreResistances, ignoreGlobal, ignoreResistors);
                DistributeDamage(ent, bodyDmg, destroyedNow);
                return;
            }
        }

        if (PickWeightedRandomAlive(ent.Comp) is { } limb)
        {
            var incoming = HasSharpDamage(raw)
                ? new DamageSpecifier(raw)
                : raw * ent.Comp.OldDamageMultiplier;

            var dmg = RunVanillaModifiers(ent.Owner, incoming, origin,
                SlotsForLimb(limb), ignoreResistances, ignoreGlobal, ignoreResistors);

            if (dmg.GetTotal() > 0)
            {
                ApplyToLimb(ent, limb, dmg, destroyedNow);
                if (bleedTrigger)
                    RollBleed(ent, limb);
            }
        }
    }

    private static bool HasSharpDamage(DamageSpecifier dmg)
    {
        if (dmg.DamageDict.TryGetValue("Slash", out var s) && s > 0)
            return true;
        if (dmg.DamageDict.TryGetValue("Piercing", out var p) && p > 0)
            return true;
        return false;
    }

    private static bool HasImpactDamage(DamageSpecifier dmg)
    {
        if (dmg.DamageDict.TryGetValue("Blunt", out var b) && b > 0)
            return true;
        if (dmg.DamageDict.TryGetValue("Slash", out var s) && s > 0)
            return true;
        if (dmg.DamageDict.TryGetValue("Piercing", out var p) && p > 0)
            return true;
        return false;
    }

    private void ApplyToLimb(Entity<LimbHealthComponent> ent, LimbType limb, DamageSpecifier dmg,
        List<LimbType> destroyedNow)
    {
        var leftover = AbsorbInto(ent.Comp, limb, dmg, destroyedNow);

        if (!leftover.Empty && leftover.GetTotal() > 0)
            DistributeDamage(ent, leftover, destroyedNow, exclude: limb);
    }

    private void RollBleed(Entity<LimbHealthComponent> ent, LimbType limb)
    {
        if (!ent.Comp.Limbs.TryGetValue(limb, out var st) || st.Destroyed)
            return;

        var changed = false;
        var heavyOnset = false;

        if ((st.Effects & LimbEffect.HeavyBleeding) != 0)
        {
        }
        else if ((st.Effects & LimbEffect.Bleeding) != 0)
        {
            if (_random.Prob(ent.Comp.BleedEscalateChance))
            {
                st.Effects &= ~LimbEffect.Bleeding;
                st.Effects |= LimbEffect.HeavyBleeding;
                ent.Comp.WeakBleedStopTime.Remove(limb);
                changed = true;
                heavyOnset = true;
            }
        }
        else if (_random.Prob(ent.Comp.BleedHeavyChance))
        {
            st.Effects |= LimbEffect.HeavyBleeding;
            ent.Comp.WeakBleedStopTime.Remove(limb);
            changed = true;
            heavyOnset = true;
        }
        else if (_random.Prob(ent.Comp.BleedLightChance))
        {
            st.Effects |= LimbEffect.Bleeding;
            ScheduleWeakBleedStop(ent.Comp, limb);
            changed = true;
        }

        if (changed)
            Dirty(ent);

        if (heavyOnset)
            _popup.PopupEntity($"Сильное кровотечение: {limb.DisplayName()}!", ent.Owner, ent.Owner, PopupType.MediumCaution);
    }

    private void ScheduleWeakBleedStop(LimbHealthComponent comp, LimbType limb)
    {
        var dur = _random.NextFloat(comp.WeakBleedMinDuration, comp.WeakBleedMaxDuration);
        comp.WeakBleedStopTime[limb] = _timing.CurTime + TimeSpan.FromSeconds(dur);
    }

    public void ApplyBleedTick(EntityUid uid, FixedPoint2 totalBudget, LimbHealthComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;
        if (totalBudget <= 0)
            return;

        var alive = GetAliveLimbs(comp);
        if (alive.Count == 0)
            return;

        FixedPoint2 totalMax = 0;
        foreach (var l in alive)
            totalMax += comp.MaxHealth[l];

        if (totalMax <= 0)
            return;

        var totalMaxF = totalMax.Float();
        var budgetF = totalBudget.Float();
        var destroyedNow = new List<LimbType>();

        foreach (var l in alive)
        {
            var share = FixedPoint2.New(budgetF * (comp.MaxHealth[l].Float() / totalMaxF));
            if (share <= 0)
                continue;

            var dmg = new DamageSpecifier();
            dmg.DamageDict["Bloodloss"] = share;
            AbsorbInto(comp, l, dmg, destroyedNow);
        }

        RecomputeBody(uid, comp);
        Dirty(uid, comp);

        foreach (var l in destroyedNow)
            RaiseLocalEvent(new LimbDestroyedEvent(uid, l));
        RefreshVitalsActive(uid, comp);
    }

    private void DistributeDamage(Entity<LimbHealthComponent> ent, DamageSpecifier dmg,
        List<LimbType> destroyedNow, LimbType? exclude = null)
    {
        var remaining = new DamageSpecifier(dmg);
        while (remaining.GetTotal() > 0)
        {
            var alive = GetAliveLimbs(ent.Comp, exclude);
            if (alive.Count == 0)
                return;

            FixedPoint2 totalMax = 0;
            foreach (var l in alive)
                totalMax += ent.Comp.MaxHealth[l];

            if (totalMax <= 0)
                return;

            var before = remaining.GetTotal();
            var totalMaxF = totalMax.Float();
            var leftover = new DamageSpecifier();

            foreach (var l in alive)
            {
                var w = ent.Comp.MaxHealth[l].Float() / totalMaxF;
                var lo = AbsorbInto(ent.Comp, l, remaining * w, destroyedNow);
                if (!lo.Empty && lo.GetTotal() > 0)
                    leftover += lo;
            }

            remaining = leftover;
            if (remaining.GetTotal() >= before)
                return;
        }
    }

    private DamageSpecifier AbsorbInto(LimbHealthComponent comp, LimbType limb, DamageSpecifier dmg,
        List<LimbType> destroyedNow)
    {
        var total = dmg.GetTotal();
        if (total <= 0)
            return new DamageSpecifier();

        if (!comp.Limbs.TryGetValue(limb, out var st) || st.Destroyed)
            return dmg;

        var max = comp.MaxHealth[limb];
        var health = st.Health(max);
        if (health <= 0)
            return dmg;

        if (total <= health)
        {
            st.Damage += dmg;
            if (st.Health(max) <= 0)
            {
                st.Destroyed = true;
                destroyedNow.Add(limb);
            }
            return new DamageSpecifier();
        }

        var scale = health.Float() / total.Float();
        var absorbed = dmg * scale;
        st.Damage += absorbed;
        st.Destroyed = true;
        destroyedNow.Add(limb);

        return dmg - absorbed;
    }

    private void DistributeHeal(LimbHealthComponent comp, DamageSpecifier dmg)
    {
        foreach (var (type, val) in dmg.DamageDict)
        {
            if (val >= 0)
                continue;

            var need = -val;
            FixedPoint2 totalType = 0;
            foreach (var st in comp.Limbs.Values)
            {
                if (st.Destroyed)
                    continue;
                if (st.Damage.DamageDict.TryGetValue(type, out var d) && d > 0)
                    totalType += d;
            }

            if (totalType <= 0)
                continue;

            var heal = FixedPoint2.Min(need, totalType);
            var totalF = totalType.Float();
            var healF = heal.Float();

            foreach (var st in comp.Limbs.Values)
            {
                if (st.Destroyed)
                    continue;
                if (!st.Damage.DamageDict.TryGetValue(type, out var d) || d <= 0)
                    continue;

                var share = FixedPoint2.New(d.Float() / totalF * healF);
                var nv = FixedPoint2.Max(FixedPoint2.Zero, d - share);
                if (nv <= 0)
                    st.Damage.DamageDict.Remove(type);
                else
                    st.Damage.DamageDict[type] = nv;
            }
        }
    }

    private static SlotFlags SlotsForLimb(LimbType limb) => (limb switch
    {
        LimbType.Head => SlotFlags.HEAD | SlotFlags.MASK | SlotFlags.EYES | SlotFlags.EARS,
        LimbType.Chest => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING | SlotFlags.TORSO | SlotFlags.NECK,
        LimbType.Abdomen => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING | SlotFlags.TORSO | SlotFlags.BELT,
        LimbType.LeftArm or LimbType.RightArm => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING | SlotFlags.GLOVES,
        LimbType.LeftLeg or LimbType.RightLeg => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING | SlotFlags.LEGS | SlotFlags.FEET,
        _ => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
    }) & ~SlotFlags.POCKET;

    private DamageSpecifier RunVanillaModifiers(EntityUid body, DamageSpecifier dmg, EntityUid? origin,
        SlotFlags slots, bool ignoreResistances, bool ignoreGlobal, List<EntityUid>? ignoreResistors)
    {
        if (!ignoreResistances)
        {
            if (TryComp<DamageableComponent>(body, out var dmgbl)
                && dmgbl.DamageModifierSetId != null
                && _proto.Resolve(dmgbl.DamageModifierSetId, out var innate))
            {
                dmg = DamageSpecifier.ApplyModifierSet(dmg, innate);
            }

            var ev = new DamageModifyEvent(dmg, origin, ignoreResistors) { OverrideSlots = slots };
            RaiseLocalEvent(body, ev);
            dmg = ev.Damage;
        }

        if (!ignoreGlobal)
            dmg = _damageable.ApplyUniversalAllModifiers(dmg);

        return dmg;
    }

    public FixedPoint2 HealLimbTypes(EntityUid uid, LimbType limb, FixedPoint2 amount, bool allTypes,
        LimbHealthComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return 0;

        if (!comp.Limbs.TryGetValue(limb, out var st) || st.Destroyed)
            return 0;

        if (amount <= 0)
            return 0;

        FixedPoint2 eligibleTotal = 0;
        foreach (var (type, dmg) in st.Damage.DamageDict)
        {
            if (dmg <= 0)
                continue;
            if (!allTypes && !LimbReagents.PhysicalDamageTypes.Contains(type))
                continue;
            eligibleTotal += dmg;
        }

        if (eligibleTotal <= 0)
            return 0;

        var heal = FixedPoint2.Min(amount, eligibleTotal);
        var totalF = eligibleTotal.Float();
        var healF = heal.Float();

        foreach (var type in st.Damage.DamageDict.Keys.ToList())
        {
            var dmg = st.Damage.DamageDict[type];
            if (dmg <= 0)
                continue;
            if (!allTypes && !LimbReagents.PhysicalDamageTypes.Contains(type))
                continue;

            var share = FixedPoint2.New(dmg.Float() / totalF * healF);
            var nv = FixedPoint2.Max(FixedPoint2.Zero, dmg - share);
            if (nv <= 0)
                st.Damage.DamageDict.Remove(type);
            else
                st.Damage.DamageDict[type] = nv;
        }

        RecomputeBody(uid, comp);
        Dirty(uid, comp);
        return heal;
    }

    public FixedPoint2 HealLimbDamageType(EntityUid uid, LimbType limb, string type, FixedPoint2 amount,
        LimbHealthComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return 0;
        if (amount <= 0)
            return 0;
        if (!comp.Limbs.TryGetValue(limb, out var st) || st.Destroyed)
            return 0;
        if (!st.Damage.DamageDict.TryGetValue(type, out var d) || d <= 0)
            return 0;

        var heal = FixedPoint2.Min(amount, d);
        var nv = d - heal;
        if (nv <= 0)
            st.Damage.DamageDict.Remove(type);
        else
            st.Damage.DamageDict[type] = nv;

        RecomputeBody(uid, comp);
        Dirty(uid, comp);
        return heal;
    }

    public void InjectLimbDose(EntityUid uid, LimbType limb, string reagent,
        LimbHealthComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        if (!comp.Limbs.TryGetValue(limb, out var st) || st.Destroyed)
            return;

        if (!LimbReagents.All.ContainsKey(reagent))
            return;

        foreach (var existing in comp.ActiveDoses)
        {
            if (existing.Limb != limb || existing.Reagent != reagent)
                continue;

            existing.Healed = 0;
            Dirty(uid, comp);
            RefreshVitalsActive(uid, comp);
            return;
        }

        comp.ActiveDoses.Add(new LimbReagentDose
        {
            Limb = limb,
            Reagent = reagent,
            Healed = 0,
        });
        Dirty(uid, comp);
        RefreshVitalsActive(uid, comp);
    }

    public void ApplyLimbDamage(EntityUid uid, LimbType limb, DamageSpecifier dmg,
        LimbHealthComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;
        if (dmg.GetTotal() <= 0)
            return;

        var destroyedNow = new List<LimbType>();
        AbsorbInto(comp, limb, dmg, destroyedNow);
        RecomputeBody(uid, comp);
        Dirty(uid, comp);

        foreach (var l in destroyedNow)
            RaiseLocalEvent(new LimbDestroyedEvent(uid, l));
        RefreshVitalsActive(uid, comp);
    }

    public bool CureBleed(EntityUid uid, LimbType limb, bool curesLight, bool curesHeavy,
        LimbHealthComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return false;

        if (!comp.Limbs.TryGetValue(limb, out var st))
            return false;

        var hasLight = (st.Effects & LimbEffect.Bleeding) != 0;
        var hasHeavy = (st.Effects & LimbEffect.HeavyBleeding) != 0;
        if (!hasLight && !hasHeavy)
            return false;

        var changed = false;

        if (hasLight && curesLight)
        {
            st.Effects &= ~LimbEffect.Bleeding;
            comp.WeakBleedStopTime.Remove(limb);
            changed = true;
        }

        if (hasHeavy && curesHeavy)
        {
            st.Effects &= ~LimbEffect.HeavyBleeding;
            changed = true;
        }

        if (!changed)
            return false;

        Dirty(uid, comp);
        RefreshVitalsActive(uid, comp);
        return true;
    }

    public bool TryGetBleed(EntityUid uid, LimbType limb, out bool heavy, LimbHealthComponent? comp = null)
    {
        heavy = false;
        if (!Resolve(uid, ref comp, false) || !comp.Limbs.TryGetValue(limb, out var st))
            return false;

        heavy = (st.Effects & LimbEffect.HeavyBleeding) != 0;
        return heavy || (st.Effects & LimbEffect.Bleeding) != 0;
    }

    public bool RestoreDestroyedLimb(EntityUid uid, LimbType limb, LimbHealthComponent? comp = null)
    {
        if (limb is LimbType.Chest or LimbType.Head)
            return false;

        if (!Resolve(uid, ref comp, false))
            return false;

        if (!comp.Limbs.TryGetValue(limb, out var st) || !st.Destroyed)
            return false;

        st.Destroyed = false;
        st.Damage = new DamageSpecifier();
        st.Effects &= ~(LimbEffect.Fracture | LimbEffect.Bleeding | LimbEffect.HeavyBleeding);
        comp.WeakBleedStopTime.Remove(limb);

        if (comp.MaxHealth.TryGetValue(limb, out var maxHp) && maxHp > 1)
            st.Damage.DamageDict["Blunt"] = maxHp - 1;

        RecomputeBody(uid, comp);
        Dirty(uid, comp);
        RaiseLocalEvent(new LimbRestoredEvent(uid, limb));
        RefreshVitalsActive(uid, comp);
        return true;
    }

    public bool IsDestroyed(EntityUid uid, LimbType limb, LimbHealthComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return false;
        return IsDestroyedState(comp, limb);
    }

    private static bool IsDestroyedState(LimbHealthComponent comp, LimbType limb)
        => comp.Limbs.TryGetValue(limb, out var st) && st.Destroyed;

    public int CountDestroyed(LimbHealthComponent comp, Func<LimbType, bool>? filter = null)
    {
        var count = 0;
        foreach (var (limb, st) in comp.Limbs)
        {
            if (st.Destroyed && (filter == null || filter(limb)))
                count++;
        }
        return count;
    }

    public FixedPoint2 GetTotalHealth(LimbHealthComponent comp)
    {
        FixedPoint2 total = 0;
        foreach (var (limb, st) in comp.Limbs)
            total += st.Health(comp.MaxHealth[limb]);
        return total;
    }

    private List<LimbType> GetAliveLimbs(LimbHealthComponent comp, LimbType? exclude = null)
    {
        var list = new List<LimbType>();
        foreach (var (limb, st) in comp.Limbs)
        {
            if (!st.Destroyed && limb != exclude)
                list.Add(limb);
        }
        return list;
    }

    private LimbType PickOtherAlive(LimbHealthComponent comp, LimbType selected)
    {
        var others = GetAliveLimbs(comp, selected);
        if (others.Count == 0)
            return selected;

        FixedPoint2 totalMax = 0;
        foreach (var l in others)
            totalMax += comp.MaxHealth[l];

        if (totalMax <= 0)
            return _random.Pick(others);

        var roll = _random.NextFloat() * totalMax.Float();
        foreach (var l in others)
        {
            roll -= comp.MaxHealth[l].Float();
            if (roll <= 0)
                return l;
        }

        return others[^1];
    }

    private LimbType? PickWeightedRandomAlive(LimbHealthComponent comp)
    {
        var alive = GetAliveLimbs(comp);
        if (alive.Count == 0)
            return null;

        FixedPoint2 totalMax = 0;
        foreach (var l in alive)
            totalMax += comp.MaxHealth[l];

        if (totalMax <= 0)
            return _random.Pick(alive);

        var roll = _random.NextFloat() * totalMax.Float();
        foreach (var l in alive)
        {
            roll -= comp.MaxHealth[l].Float();
            if (roll <= 0)
                return l;
        }

        return alive[^1];
    }

    private bool IsStandingInHazard(EntityUid victim)
    {
        if (!TryComp<STInZoneAnomalyComponent>(victim, out var marker))
            return false;

        foreach (var src in marker.Sources.ToList())
        {
            if (!TryComp<ZoneAnomalyComponent>(src, out var anomaly) || !anomaly.InAnomaly.Contains(victim))
                marker.Sources.Remove(src);
        }

        if (marker.Sources.Count == 0)
        {
            RemComp<STInZoneAnomalyComponent>(victim);
            return false;
        }

        return true;
    }

    private LimbType? PickAliveLeg(LimbHealthComponent comp)
    {
        var legs = new List<LimbType>();
        if (comp.Limbs.TryGetValue(LimbType.LeftLeg, out var l) && !l.Destroyed)
            legs.Add(LimbType.LeftLeg);
        if (comp.Limbs.TryGetValue(LimbType.RightLeg, out var r) && !r.Destroyed)
            legs.Add(LimbType.RightLeg);
        return legs.Count > 0 ? _random.Pick(legs) : (LimbType?) null;
    }

    private void RecomputeBody(EntityUid uid, LimbHealthComponent comp, bool notify = false,
        EntityUid? origin = null, bool interruptsDoAfters = false)
    {
        if (!TryComp<DamageableComponent>(uid, out var dmgbl))
            return;

        var sum = new DamageSpecifier();
        foreach (var st in comp.Limbs.Values)
            sum += st.Damage;

        var old = notify ? new DamageSpecifier(dmgbl.Damage) : null;

        _syncing = true;
        _damageable.SetDamage((uid, dmgbl), sum);
        _syncing = false;

        if (notify && old != null)
        {
            var delta = sum - old;
            RaiseLocalEvent(uid, new DamageChangedEvent(dmgbl, delta, interruptsDoAfters, origin));
        }
    }

    public void RefreshVitalsActive(EntityUid uid, LimbHealthComponent comp)
    {
        var need = NeedsVitals(comp);
        var has = HasComp<LimbVitalsActiveComponent>(uid);

        if (need && !has)
            AddComp<LimbVitalsActiveComponent>(uid);
        else if (!need && has)
            RemComp<LimbVitalsActiveComponent>(uid);
    }

    private static bool NeedsVitals(LimbHealthComponent comp)
    {
        if (comp.ActiveDoses.Count > 0)
            return true;

        foreach (var (limb, st) in comp.Limbs)
        {
            if ((st.Effects & (LimbEffect.Bleeding | LimbEffect.HeavyBleeding)) != 0)
                return true;
            if (st.Destroyed && limb is LimbType.Abdomen or LimbType.Chest)
                return true;
        }

        return false;
    }
}
