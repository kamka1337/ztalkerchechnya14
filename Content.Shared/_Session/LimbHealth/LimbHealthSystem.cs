using System.Linq;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Projectiles;
using Content.Shared.Rejuvenate;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._Session.LimbHealth;

public sealed class LimbHealthSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedBodyTargetingSystem _targeting = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;

    private bool _syncing;

    private EntityUid? _bulletHitTarget;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimbHealthComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LimbHealthComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
        SubscribeLocalEvent<LimbHealthComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<ProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
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
        InitLimbs(ent.Comp);
        ent.Comp.PermanentAsphyxiation = false;
        ent.Comp.ChestCheckStep = 0;
        ent.Comp.ActiveDoses.Clear();
        ent.Comp.NeedledLimbs.Clear();
        Dirty(ent);
        RaiseLocalEvent(new LimbHealthChangedEvent(ent.Owner));
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

        var dmg = new DamageSpecifier(args.Damage);
        var total = dmg.GetTotal();
        if (total == 0)
            return;

        var bleedTrigger = isBullet || HasSharpDamage(dmg);
        var destroyedNow = new List<LimbType>();

        if (total > 0)
            RouteDamage(ent, dmg, args.Origin, destroyedNow, bleedTrigger);
        else
            DistributeHeal(ent.Comp, dmg);

        RecomputeBody(ent.Owner, ent.Comp);
        Dirty(ent);

        foreach (var limb in destroyedNow)
            RaiseLocalEvent(new LimbDestroyedEvent(ent.Owner, limb));

        RaiseLocalEvent(new LimbHealthChangedEvent(ent.Owner));
    }

    private void RouteDamage(Entity<LimbHealthComponent> ent, DamageSpecifier dmg, EntityUid? origin,
        List<LimbType> destroyedNow, bool bleedTrigger)
    {
        if (origin is { } attacker && attacker != ent.Owner && _targeting.TryGetSelected(attacker, out var selected))
        {
            if (IsDestroyedState(ent.Comp, selected))
            {
                DistributeDamage(ent, dmg, destroyedNow, applyArmor: true);
                return;
            }

            LimbType target;
            if (_random.Prob(selected.HitChance()))
                target = selected;
            else
                target = PickOtherAlive(ent.Comp, selected);

            ApplyToLimb(ent, target, dmg, destroyedNow);

            if (bleedTrigger)
                RollBleed(ent, target);
            return;
        }

        DistributeDamage(ent, dmg * ent.Comp.OldDamageMultiplier, destroyedNow, applyArmor: true);
    }

    private static bool HasSharpDamage(DamageSpecifier dmg)
    {
        if (dmg.DamageDict.TryGetValue("Slash", out var s) && s > 0)
            return true;
        if (dmg.DamageDict.TryGetValue("Piercing", out var p) && p > 0)
            return true;
        return false;
    }

    private void ApplyToLimb(Entity<LimbHealthComponent> ent, LimbType limb, DamageSpecifier dmg,
        List<LimbType> destroyedNow)
    {
        var armored = ApplyLimbArmor(ent.Owner, limb, dmg);
        var leftover = AbsorbInto(ent.Comp, limb, armored, destroyedNow);

        if (!leftover.Empty && leftover.GetTotal() > 0)
            DistributeDamage(ent, leftover, destroyedNow, applyArmor: false, exclude: limb);
    }

    private void RollBleed(Entity<LimbHealthComponent> ent, LimbType limb)
    {
        if (!ent.Comp.Limbs.TryGetValue(limb, out var st) || st.Destroyed)
            return;

        var changed = false;

        if ((st.Effects & LimbEffect.HeavyBleeding) != 0)
        {
        }
        else if ((st.Effects & LimbEffect.Bleeding) != 0)
        {
            if (_random.Prob(ent.Comp.BleedEscalateChance))
            {
                st.Effects &= ~LimbEffect.Bleeding;
                st.Effects |= LimbEffect.HeavyBleeding;
                changed = true;
            }
        }
        else if (_random.Prob(ent.Comp.BleedHeavyChance))
        {
            st.Effects |= LimbEffect.HeavyBleeding;
            changed = true;
        }
        else if (_random.Prob(ent.Comp.BleedLightChance))
        {
            st.Effects |= LimbEffect.Bleeding;
            changed = true;
        }

        if (changed)
            Dirty(ent);
    }

    private void DistributeDamage(Entity<LimbHealthComponent> ent, DamageSpecifier dmg,
        List<LimbType> destroyedNow, bool applyArmor, LimbType? exclude = null)
    {
        var alive = GetAliveLimbs(ent.Comp, exclude);
        if (alive.Count == 0)
            return;

        FixedPoint2 totalMax = 0;
        foreach (var l in alive)
            totalMax += ent.Comp.MaxHealth[l];

        if (totalMax <= 0)
            return;

        var totalMaxF = totalMax.Float();
        foreach (var l in alive)
        {
            var w = ent.Comp.MaxHealth[l].Float() / totalMaxF;
            var portion = dmg * w;
            var armored = applyArmor ? ApplyLimbArmor(ent.Owner, l, portion) : portion;
            AbsorbInto(ent.Comp, l, armored, destroyedNow);
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

    private DamageSpecifier ApplyLimbArmor(EntityUid uid, LimbType limb, DamageSpecifier dmg)
    {
        string? slot = limb switch
        {
            LimbType.Head => "head",
            LimbType.Chest or LimbType.Abdomen => "outerClothing",
            _ => null,
        };

        if (slot == null)
            return dmg;

        if (!_inventory.TryGetSlotEntity(uid, slot, out var item))
            return dmg;

        if (!TryComp<ArmorComponent>(item, out var armor))
            return dmg;

        var set = armor.Modifiers ?? armor.BaseModifiers;
        if (set == null)
            return dmg;

        return DamageSpecifier.ApplyModifierSet(dmg, set);
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
        RaiseLocalEvent(new LimbHealthChangedEvent(uid));
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

        comp.ActiveDoses.Add(new LimbReagentDose
        {
            Limb = limb,
            Reagent = reagent,
            Healed = 0,
        });
        Dirty(uid, comp);
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
        RaiseLocalEvent(new LimbHealthChangedEvent(uid));
    }

    public bool CureBleed(EntityUid uid, LimbType limb, bool includeHeavy, LimbHealthComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return false;

        if (!comp.Limbs.TryGetValue(limb, out var st))
            return false;

        var had = st.Effects & (LimbEffect.Bleeding | LimbEffect.HeavyBleeding);
        if (had == 0)
            return false;

        if ((st.Effects & LimbEffect.HeavyBleeding) != 0 && !includeHeavy)
            return false;

        st.Effects &= ~LimbEffect.Bleeding;
        if (includeHeavy)
            st.Effects &= ~LimbEffect.HeavyBleeding;

        Dirty(uid, comp);
        RaiseLocalEvent(new LimbHealthChangedEvent(uid));
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
        st.Effects &= ~LimbEffect.Fracture;

        if (comp.MaxHealth.TryGetValue(limb, out var maxHp) && maxHp > 1)
            st.Damage.DamageDict["Blunt"] = maxHp - 1;

        RecomputeBody(uid, comp);
        Dirty(uid, comp);
        RaiseLocalEvent(new LimbRestoredEvent(uid, limb));
        RaiseLocalEvent(new LimbHealthChangedEvent(uid));
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
        return others.Count > 0 ? _random.Pick(others) : selected;
    }

    private void RecomputeBody(EntityUid uid, LimbHealthComponent comp)
    {
        if (!TryComp<DamageableComponent>(uid, out var dmgbl))
            return;

        var sum = new DamageSpecifier();
        foreach (var st in comp.Limbs.Values)
            sum += st.Damage;

        _syncing = true;
        _damageable.SetDamage((uid, dmgbl), sum);
        _syncing = false;
    }
}
