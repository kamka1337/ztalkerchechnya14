using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._Stalker.Jobs;

/// <summary>
/// Inserts a starting module entity into an item slot of an equipped item on spawn.
/// </summary>
[UsedImplicitly]
public sealed partial class STAttachWeaponModuleSpecial : JobSpecial
{
    [DataField(required: true)]
    public string EquipmentSlot { get; private set; } = string.Empty;

    [DataField(required: true)]
    public string ItemSlot { get; private set; } = string.Empty;

    [DataField(required: true)]
    public EntProtoId Module { get; private set; }

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var inventory = entMan.System<InventorySystem>();

        if (!inventory.TryGetSlotEntity(mob, EquipmentSlot, out var item))
            return;

        if (!entMan.TryGetComponent<ItemSlotsComponent>(item.Value, out var itemSlotsComp))
            return;

        var itemSlots = entMan.System<ItemSlotsSystem>();
        if (!itemSlots.TryGetSlot(item.Value, ItemSlot, out var slot, itemSlotsComp) || slot.HasItem)
            return;

        var coords = entMan.GetComponent<TransformComponent>(mob).Coordinates;
        var moduleEnt = entMan.SpawnEntity(Module, coords);

        if (!itemSlots.TryInsert(item.Value, slot, moduleEnt, user: null, excludeUserAudio: true))
        {
            entMan.QueueDeleteEntity(moduleEnt);
        }
    }
}
