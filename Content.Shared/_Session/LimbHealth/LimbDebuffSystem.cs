using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Systems;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Maths;

namespace Content.Shared._Session.LimbHealth;

public sealed class LimbDebuffSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly LimbHealthSystem _limbs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimbHealthComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovement);
        SubscribeLocalEvent<LimbDestroyedEvent>(OnLimbChanged);
        SubscribeLocalEvent<LimbRestoredEvent>(OnLimbRestored);

        SubscribeLocalEvent<LimbHealthComponent, UseAttemptEvent>(OnHandUseAttempt);
        SubscribeLocalEvent<LimbHealthComponent, AttackAttemptEvent>(OnHandUseAttempt);
        SubscribeLocalEvent<LimbHealthComponent, PickupAttemptEvent>(OnHandUseAttempt);
        SubscribeLocalEvent<LimbHealthComponent, ThrowAttemptEvent>(OnHandUseAttempt);

        SubscribeLocalEvent<GunComponent, GunRefreshModifiersEvent>(OnGunRefresh);
        SubscribeLocalEvent<GunComponent, GotEquippedHandEvent>(OnGunEquipped);
    }

    private void OnHandUseAttempt(EntityUid uid, LimbHealthComponent comp, CancellableEntityEventArgs args)
    {
        if (ActiveHandDisabled(uid, comp))
            args.Cancel();
    }

    private bool ActiveHandDisabled(EntityUid uid, LimbHealthComponent comp)
    {
        if (!TryComp<HandsComponent>(uid, out var hands))
            return false;

        var active = _hands.GetActiveHand((uid, hands));
        if (active == null || !_hands.TryGetHand((uid, hands), active, out var hand))
            return false;

        if (!LimbHandMap.TryGetArmForHand(hand.Value.Location, out var arm))
            return false;

        return comp.Limbs.TryGetValue(arm, out var st) && st.Destroyed;
    }

    private void OnRefreshMovement(Entity<LimbHealthComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var legs = _limbs.CountDestroyed(ent.Comp, l => l.IsLeg());
        var factor = legs switch
        {
            0 => 1f,
            1 => 0.67f,
            _ => 0.25f,
        };

        if (factor < 1f)
            args.ModifySpeed(factor);
    }

    private void OnLimbChanged(LimbDestroyedEvent args)
    {
        RefreshLimbDebuffs(args.Owner, args.Limb);

        if (args.Limb.IsArm())
            SwitchAwayFromArm(args.Owner, args.Limb);
    }

    private void SwitchAwayFromArm(EntityUid owner, LimbType destroyedArm)
    {
        if (!TryComp<HandsComponent>(owner, out var hands))
            return;

        if (!TryComp<LimbHealthComponent>(owner, out var limbs))
            return;

        var active = _hands.GetActiveHand((owner, hands));
        if (active == null || !_hands.TryGetHand((owner, hands), active, out var activeHand))
            return;

        if (!LimbHandMap.TryGetArmForHand(activeHand.Value.Location, out var activeArm) || activeArm != destroyedArm)
            return;

        foreach (var name in _hands.EnumerateHands((owner, hands)))
        {
            if (!_hands.TryGetHand((owner, hands), name, out var hand))
                continue;

            if (!LimbHandMap.TryGetArmForHand(hand.Value.Location, out var arm) || arm == destroyedArm)
                continue;

            if (limbs.Limbs.TryGetValue(arm, out var st) && st.Destroyed)
                continue;

            _hands.TrySetActiveHand((owner, hands), name);
            return;
        }
    }

    private void OnLimbRestored(LimbRestoredEvent args)
    {
        RefreshLimbDebuffs(args.Owner, args.Limb);
    }

    private void RefreshLimbDebuffs(EntityUid owner, LimbType limb)
    {
        if (limb.IsLeg())
            _movement.RefreshMovementSpeedModifiers(owner);

        if (limb.IsArm())
        {
            foreach (var held in _hands.EnumerateHeld(owner))
            {
                if (HasComp<GunComponent>(held))
                    _gun.RefreshModifiers(held);
            }
        }
    }

    private void OnGunEquipped(Entity<GunComponent> ent, ref GotEquippedHandEvent args)
    {
        _gun.RefreshModifiers((ent.Owner, ent.Comp));
    }

    private void OnGunRefresh(Entity<GunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var holder = Transform(ent.Owner).ParentUid;
        if (!TryComp<LimbHealthComponent>(holder, out var limbs))
            return;

        var arms = _limbs.CountDestroyed(limbs, l => l.IsArm());
        var mult = arms switch
        {
            0 => 1f,
            1 => 1.5f,
            _ => 4f,
        };

        if (mult <= 1f)
            return;

        args.MaxAngle = new Angle(args.MaxAngle.Theta * mult);
        args.MinAngle = new Angle(args.MinAngle.Theta * mult);
        args.AngleIncrease = new Angle(args.AngleIncrease.Theta * mult);
    }
}
