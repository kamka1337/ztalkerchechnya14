using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Systems;
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

        SubscribeLocalEvent<GunComponent, GunRefreshModifiersEvent>(OnGunRefresh);
        SubscribeLocalEvent<GunComponent, GotEquippedHandEvent>(OnGunEquipped);
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
