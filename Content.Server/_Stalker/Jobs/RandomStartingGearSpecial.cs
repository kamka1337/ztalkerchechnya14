using Content.Server.Station.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Stalker.Jobs;

/// <summary>
/// Selects a starting kit from a weighted pool and equips it onto the mob.
/// Supports equipping gear prototypes and automatically inserting weapon modules and magazines.
/// </summary>
[UsedImplicitly]
public sealed partial class RandomStartingGearSpecial : JobSpecial
{
    [DataField(required: true)]
    public List<RandomStartingGearEntry> Kits = new();

    public override void AfterEquip(EntityUid mob)
    {
        if (Kits.Count == 0)
            return;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var random = IoCManager.Resolve<IRobustRandom>();
        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        float totalWeight = 0;
        foreach (var kit in Kits)
        {
            totalWeight += kit.Weight;
        }

        if (totalWeight <= 0)
            return;

        var roll = random.NextFloat() * totalWeight;
        RandomStartingGearEntry? chosenKit = null;
        float current = 0;
        foreach (var kit in Kits)
        {
            current += kit.Weight;
            if (roll <= current)
            {
                chosenKit = kit;
                break;
            }
        }

        chosenKit ??= Kits[^1];

        var stationSpawning = entMan.System<StationSpawningSystem>();

        if (chosenKit.Gear != null)
        {
            if (protoMan.TryIndex<StartingGearPrototype>(chosenKit.Gear.Value, out var gearProto))
            {
                stationSpawning.EquipStartingGear(mob, gearProto, raiseEvent: false);
            }
        }

        var inventory = entMan.System<InventorySystem>();
        var itemSlots = entMan.System<ItemSlotsSystem>();
        var xform = entMan.GetComponent<TransformComponent>(mob);

        if (chosenKit.Modules != null && chosenKit.Modules.Count > 0)
        {
            foreach (var (equipSlot, moduleDict) in chosenKit.Modules)
            {
                if (!inventory.TryGetSlotEntity(mob, equipSlot, out var item))
                    continue;

                if (!entMan.TryGetComponent<ItemSlotsComponent>(item.Value, out var itemSlotsComp))
                    continue;

                foreach (var (itemSlotName, moduleProto) in moduleDict)
                {
                    if (!itemSlots.TryGetSlot(item.Value, itemSlotName, out var slot, itemSlotsComp))
                        continue;

                    if (slot.HasItem)
                    {
                        if (itemSlots.TryEject(item.Value, slot, user: null, out var oldItem, excludeUserAudio: true))
                        {
                            if (oldItem != null)
                                entMan.QueueDeleteEntity(oldItem.Value);
                        }
                    }

                    var moduleEnt = entMan.SpawnEntity(moduleProto, xform.Coordinates);
                    if (!itemSlots.TryInsert(item.Value, slot, moduleEnt, user: null, excludeUserAudio: true))
                    {
                        entMan.QueueDeleteEntity(moduleEnt);
                    }
                }
            }
        }

        // Locate weapon (check suitstorage first, then active hand)
        EntityUid? weapon = null;
        if (inventory.TryGetSlotEntity(mob, "suitstorage", out var suitItem))
        {
            weapon = suitItem;
        }
        else if (entMan.System<SharedHandsSystem>().TryGetActiveItem(mob, out var activeItem))
        {
            weapon = activeItem;
        }

        if (weapon != null)
        {
            // Insert requested magazine
            if (chosenKit.Magazine != null)
            {
                if (entMan.TryGetComponent<ItemSlotsComponent>(weapon.Value, out var weaponSlots))
                {
                    if (itemSlots.TryGetSlot(weapon.Value, "gun_magazine", out var magSlot, weaponSlots))
                    {
                        if (magSlot.HasItem)
                        {
                            if (itemSlots.TryEject(weapon.Value, magSlot, user: null, out var oldMag, excludeUserAudio: true))
                            {
                                if (oldMag != null)
                                    entMan.QueueDeleteEntity(oldMag.Value);
                            }
                        }

                        var magEnt = entMan.SpawnEntity(chosenKit.Magazine.Value, xform.Coordinates);
                        if (!itemSlots.TryInsert(weapon.Value, magSlot, magEnt, user: null, excludeUserAudio: true))
                        {
                            entMan.QueueDeleteEntity(magEnt);
                        }
                    }
                }
            }

            // Fill internal ammo provider (e.g. Mosin, SKS)
            if (chosenKit.InternalAmmo != null)
            {
                if (entMan.TryGetComponent<BallisticAmmoProviderComponent>(weapon.Value, out var ballisticComp))
                {
                    entMan.System<SharedGunSystem>().SetBallisticInternalAmmo((weapon.Value, ballisticComp), chosenKit.InternalAmmo.Value);
                }
            }
        }
    }
}

[DataDefinition]
public sealed partial class RandomStartingGearEntry
{
    [DataField]
    public float Weight = 1f;

    [DataField]
    public ProtoId<StartingGearPrototype>? Gear;

    /// <summary>
    /// equipmentSlot -> (itemSlot -> moduleProto)
    /// e.g. suitstorage -> (gun_module_scope: STModuleScopeRiflesT2Ru)
    /// </summary>
    [DataField]
    public Dictionary<string, Dictionary<string, EntProtoId>>? Modules;

    /// <summary>
    /// Magazine prototype to insert into the weapon's gun_magazine slot.
    /// </summary>
    [DataField]
    public EntProtoId? Magazine;

    /// <summary>
    /// Cartridge prototype to fill into the weapon's BallisticAmmoProviderComponent (internal magazine).
    /// </summary>
    [DataField]
    public EntProtoId? InternalAmmo;
}
