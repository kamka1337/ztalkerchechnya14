using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable;
using Robust.Shared.Containers;

namespace Content.Shared._Stalker.Weapon.GrenadeLauncher;

/// <summary>
/// Shared system for the GP-25 underslung grenade launcher.
/// Grants the left-side item action button when holding a gun with GP-25 attached.
/// </summary>
public abstract class STSharedGrenadeLauncherSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public const string LauncherSlotId = "gun_module_launcher";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunComponent, GetItemActionsEvent>(OnGetActions);

        // Cancel the host gun's normal fire if aiming mode was used.
        SubscribeLocalEvent<STGrenadeLauncherAimingComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<STGrenadeLauncherAimingComponent, GotUnequippedHandEvent>(OnUnequippedHand);
        SubscribeLocalEvent<STGrenadeLauncherAimingComponent, HandDeselectedEvent>(OnHandDeselected);
        SubscribeLocalEvent<STGrenadeLauncherAimingComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<STGrenadeLauncherAimingComponent, ItemUnwieldedEvent>(OnItemUnwielded);
    }

    private void OnGetActions(Entity<GunComponent> gun, ref GetItemActionsEvent args)
    {
        if (!_itemSlots.TryGetSlot(gun, LauncherSlotId, out var slot) || slot.Item is not { } launcher)
            return;

        if (!TryComp<STGrenadeLauncherModuleComponent>(launcher, out var launcherComp))
            return;

        args.AddAction(ref launcherComp.FireActionEntity, launcherComp.FireAction);
    }

    protected virtual void OnShotAttempted(Entity<STGrenadeLauncherAimingComponent> ent, ref ShotAttemptedEvent args)
    {
        args.Cancel();
    }

    private void OnUnequippedHand(Entity<STGrenadeLauncherAimingComponent> ent, ref GotUnequippedHandEvent args)
    {
        RemCompDeferred<STGrenadeLauncherAimingComponent>(ent);
    }

    private void OnHandDeselected(Entity<STGrenadeLauncherAimingComponent> ent, ref HandDeselectedEvent args)
    {
        RemCompDeferred<STGrenadeLauncherAimingComponent>(ent);
    }

    private void OnRemovedFromContainer(Entity<STGrenadeLauncherAimingComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        RemCompDeferred<STGrenadeLauncherAimingComponent>(ent);
    }

    private void OnItemUnwielded(Entity<STGrenadeLauncherAimingComponent> ent, ref ItemUnwieldedEvent args)
    {
        RemCompDeferred<STGrenadeLauncherAimingComponent>(ent);
    }
}
