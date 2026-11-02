using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged.Components;
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
    }

    private void OnGetActions(Entity<GunComponent> gun, ref GetItemActionsEvent args)
    {
        if (!_itemSlots.TryGetSlot(gun, LauncherSlotId, out var slot) || slot.Item is not { } launcher)
            return;

        if (!TryComp<STGrenadeLauncherModuleComponent>(launcher, out var launcherComp))
            return;

        args.AddAction(ref launcherComp.FireActionEntity, launcherComp.FireAction);
    }
}
