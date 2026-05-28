using Content.Server.Fluids.EntitySystems;
using Content.Shared._Session.LimbHealth;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Session.LimbHealth;

public sealed class LimbVitalsSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly LimbHealthSystem _limbs = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimbDestroyedEvent>(OnDestroyed);
    }

    private void OnDestroyed(LimbDestroyedEvent args)
    {
        if (!TryComp<LimbHealthComponent>(args.Owner, out var comp))
            return;
        Entity<LimbHealthComponent> ent = (args.Owner, comp);

        if (ent.Comp.ActiveDoses.RemoveAll(d => d.Limb == args.Limb) > 0)
            Dirty(ent);

        switch (args.Limb)
        {
            case LimbType.Head:
                KillByAsphyxiation(ent);
                break;

            case LimbType.Chest:
                ent.Comp.NextChestCheck = _timing.CurTime + ent.Comp.ChestAsphyxiationInterval;
                ent.Comp.ChestCheckStep = 0;
                Dirty(ent);
                break;
        }

        if (_limbs.CountDestroyed(ent.Comp) >= ent.Comp.MaxHealth.Count)
            _mobState.ChangeMobState(ent.Owner, MobState.Dead);
    }

    private void TickDoses(Entity<LimbHealthComponent> ent)
    {
        if (ent.Comp.ActiveDoses.Count == 0)
            return;

        var changed = false;
        for (var i = ent.Comp.ActiveDoses.Count - 1; i >= 0; i--)
        {
            var dose = ent.Comp.ActiveDoses[i];

            if (!LimbReagents.All.TryGetValue(dose.Reagent, out var info))
            {
                ent.Comp.ActiveDoses.RemoveAt(i);
                changed = true;
                continue;
            }

            if (!ent.Comp.Limbs.TryGetValue(dose.Limb, out var st) || st.Destroyed)
            {
                ent.Comp.ActiveDoses.RemoveAt(i);
                changed = true;
                continue;
            }

            var budget = info.TotalHeal - dose.Healed;
            if (budget <= 0)
            {
                ent.Comp.ActiveDoses.RemoveAt(i);
                changed = true;
                continue;
            }

            var amount = FixedPoint2.Min(info.RatePerSecond, budget);
            var actuallyHealed = _limbs.HealLimbTypes(ent.Owner, dose.Limb, amount, info.HealAllTypes, ent.Comp);

            if (actuallyHealed <= 0)
            {
                ent.Comp.ActiveDoses.RemoveAt(i);
                changed = true;
                continue;
            }

            dose.Healed += actuallyHealed;
            changed = true;

            if (actuallyHealed < amount || dose.Healed >= info.TotalHeal)
                ent.Comp.ActiveDoses.RemoveAt(i);
        }

        if (changed)
            Dirty(ent);
    }

    private void TickBleed(Entity<LimbHealthComponent> ent)
    {
        List<(LimbType Limb, FixedPoint2 Damage, FixedPoint2 Puddle)>? bleeds = null;
        foreach (var (limb, st) in ent.Comp.Limbs)
        {
            if (st.Destroyed)
                continue;

            if ((st.Effects & LimbEffect.HeavyBleeding) != 0)
                (bleeds ??= new()).Add((limb, ent.Comp.HeavyBleedDamage, ent.Comp.HeavyBleedPuddle));
            else if ((st.Effects & LimbEffect.Bleeding) != 0)
                (bleeds ??= new()).Add((limb, ent.Comp.LightBleedDamage, ent.Comp.LightBleedPuddle));
        }

        if (bleeds == null)
            return;

        FixedPoint2 totalPuddle = 0;
        foreach (var (limb, damage, puddle) in bleeds)
        {
            var dmg = new DamageSpecifier();
            dmg.DamageDict["Bloodloss"] = damage;
            _limbs.ApplyLimbDamage(ent.Owner, limb, dmg, ent.Comp);
            totalPuddle += puddle;
        }

        if (totalPuddle > 0)
            _puddle.TrySpillAt(ent.Owner, new Solution("Blood", totalPuddle), out _, sound: false);
    }

    private static float ChestAsphyxiationChance(int step)
    {
        var raw = step switch
        {
            0 => 0.05f,
            _ => 0.08f + 0.02f * step,
        };
        return MathF.Min(0.50f, raw);
    }

    private void KillByAsphyxiation(Entity<LimbHealthComponent> ent)
    {
        ent.Comp.PermanentAsphyxiation = true;
        Dirty(ent);
        _mobState.ChangeMobState(ent.Owner, MobState.Dead);
        _threshold.SetAllowRevives(ent.Owner, false);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<LimbHealthComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_mobState.IsAlive(uid))
                continue;

            if (comp.Limbs.TryGetValue(LimbType.Abdomen, out var abdomen) && abdomen.Destroyed)
            {
                if (TryComp<HungerComponent>(uid, out var hunger))
                    _hunger.ModifyHunger(uid, -hunger.ActualDecayRate * frameTime, hunger);

                if (TryComp<ThirstComponent>(uid, out var thirst))
                    _thirst.ModifyThirst(uid, thirst, -thirst.ActualDecayRate * frameTime);
            }

            if (now >= comp.NextBleedTick)
            {
                comp.NextBleedTick = now + comp.BleedInterval;
                TickBleed((uid, comp));
            }

            if (now >= comp.NextDoseTick)
            {
                comp.NextDoseTick = now + comp.DoseInterval;
                TickDoses((uid, comp));
            }

            if (comp.PermanentAsphyxiation)
                continue;

            if (!comp.Limbs.TryGetValue(LimbType.Chest, out var chest) || !chest.Destroyed)
                continue;

            if (now < comp.NextChestCheck)
                continue;

            comp.NextChestCheck = now + comp.ChestAsphyxiationInterval;
            var chance = ChestAsphyxiationChance(comp.ChestCheckStep);
            comp.ChestCheckStep++;

            if (_random.Prob(chance))
                KillByAsphyxiation((uid, comp));
        }
    }
}
