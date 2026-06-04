using Content.Server.Fluids.EntitySystems;
using Content.Shared._Session.LimbHealth;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
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
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

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

        _popup.PopupEntity($"Конечность выведена из строя: {args.Limb.DisplayName()}!",
            ent.Owner, ent.Owner, PopupType.LargeCaution);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/bone_rattle.ogg"), ent.Owner);

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

        _limbs.RefreshVitalsActive(ent.Owner, ent.Comp);
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
        {
            Dirty(ent);
            _limbs.RefreshVitalsActive(ent.Owner, ent.Comp);
        }
    }

    private void TickBleed(Entity<LimbHealthComponent> ent, bool heavy)
    {
        var flag = heavy ? LimbEffect.HeavyBleeding : LimbEffect.Bleeding;

        var sources = 0;
        var aliveCount = 0;
        foreach (var (_, st) in ent.Comp.Limbs)
        {
            if (!st.Destroyed)
                aliveCount++;

            if ((st.Effects & flag) != 0)
                sources++;
        }

        if (sources == 0 || aliveCount == 0)
            return;

        var budget = ent.Comp.BleedDamagePerLimb * aliveCount * sources;
        _limbs.ApplyBleedTick(ent.Owner, budget, ent.Comp);

        var puddle = ent.Comp.BleedPuddlePerTick * sources;
        if (puddle > 0)
            _puddle.TrySpillAt(ent.Owner, new Solution("Blood", puddle), out _, sound: false);
    }

    private void UpdateWeakBleedStops(Entity<LimbHealthComponent> ent, TimeSpan now)
    {
        if (ent.Comp.WeakBleedStopTime.Count == 0)
            return;

        List<LimbType>? toStop = null;
        foreach (var (limb, stopTime) in ent.Comp.WeakBleedStopTime)
        {
            if (now >= stopTime)
                (toStop ??= new()).Add(limb);
        }

        if (toStop == null)
            return;

        var changed = false;
        foreach (var limb in toStop)
        {
            ent.Comp.WeakBleedStopTime.Remove(limb);
            if (ent.Comp.Limbs.TryGetValue(limb, out var st) && (st.Effects & LimbEffect.Bleeding) != 0)
            {
                st.Effects &= ~LimbEffect.Bleeding;
                changed = true;
            }
        }

        if (changed)
        {
            Dirty(ent);
            _limbs.RefreshVitalsActive(ent.Owner, ent.Comp);
        }
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

        var unrev = EnsureComp<UnrevivableComponent>(ent.Owner);
        unrev.ReasonMessage = "stalker-limb-unrevivable";
        Dirty(ent.Owner, unrev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<LimbVitalsActiveComponent, LimbHealthComponent>();
        while (query.MoveNext(out var uid, out _, out var comp))
        {
            if (!_mobState.IsAlive(uid))
                continue;

            if (comp.Limbs.TryGetValue(LimbType.Abdomen, out var abdomen) && abdomen.Destroyed)
            {
                var extra = comp.AbdomenStarvationMultiplier - 1f;
                if (extra > 0f)
                {
                    if (TryComp<HungerComponent>(uid, out var hunger))
                        _hunger.ModifyHunger(uid, -hunger.ActualDecayRate * frameTime * extra, hunger);

                    if (TryComp<ThirstComponent>(uid, out var thirst))
                        _thirst.ModifyThirst(uid, thirst, -thirst.ActualDecayRate * frameTime * extra);
                }
            }

            if (now >= comp.NextWeakBleedTick)
            {
                comp.NextWeakBleedTick = now + comp.WeakBleedInterval;
                TickBleed((uid, comp), heavy: false);
            }

            if (now >= comp.NextHeavyBleedTick)
            {
                comp.NextHeavyBleedTick = now + comp.HeavyBleedInterval;
                TickBleed((uid, comp), heavy: true);
            }

            UpdateWeakBleedStops((uid, comp), now);

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
