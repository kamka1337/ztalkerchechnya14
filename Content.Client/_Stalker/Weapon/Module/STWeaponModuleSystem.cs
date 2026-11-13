using Content.Client.Weapons.Ranged.Components;
using Content.Shared._Stalker.Weapon.Module;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;

namespace Content.Client._Stalker.Weapon.Module;

public sealed class STWeaponModuleSystem : STSharedWeaponModuleSystem
{
    private EntityQuery<ContainerManagerComponent> _containerMangerQuery;
    private EntityQuery<SpriteComponent> _spriteQuery;
    private EntityQuery<STWeaponModuleComponent> _weaponModuleQuery;

    public override void Initialize()
    {
        base.Initialize();

        _containerMangerQuery = GetEntityQuery<ContainerManagerComponent>();
        _spriteQuery = GetEntityQuery<SpriteComponent>();
        _weaponModuleQuery = GetEntityQuery<STWeaponModuleComponent>();

        SubscribeLocalEvent<STWeaponModuleContainerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<STWeaponModuleContainerComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<STWeaponModuleContainerComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<STWeaponModuleContainerComponent> ent, ref ComponentStartup args)
    {
        UpdateAllVisuals(ent);
    }

    private void UpdateAllVisuals(Entity<STWeaponModuleContainerComponent> ent)
    {
        if (!_containerMangerQuery.TryComp(ent, out var containerManager))
            return;

        foreach (var (_, container) in containerManager.Containers)
        {
            foreach (var contained in container.ContainedEntities)
            {
                if (_weaponModuleQuery.HasComp(contained))
                {
                    UpdateVisuals(ent, contained, visible: true);
                }
            }
        }
    }

    private void OnInserted(Entity<STWeaponModuleContainerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateVisuals(ent, args.Entity, visible: true);
    }

    private void OnRemoved(Entity<STWeaponModuleContainerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateVisuals(ent, args.Entity, visible: false);
    }

    private void UpdateVisuals(Entity<STWeaponModuleContainerComponent> ent, Entity<STWeaponModuleComponent?> moduleEnt, bool visible = true)
    {
        if (!_spriteQuery.TryComp(ent, out var spriteComponent))
            return;

        UpdateVisuals(spriteComponent, moduleEnt, visible: visible);
    }

    private void UpdateVisuals(SpriteComponent spriteComponent, Entity<STWeaponModuleComponent?> ent, bool visible = true)
    {
        if (spriteComponent.BaseRSI is null)
            return;

        if (!_weaponModuleQuery.Resolve(ent, ref ent.Comp, logMissing: false))
            return;

        var layerKey = !string.IsNullOrEmpty(ent.Comp.Layer) ? ent.Comp.Layer : ent.Comp.StatePostfix;
        if (string.IsNullOrEmpty(layerKey))
        {
            layerKey = ent.Comp.ScopeEffect != null ? "scope" : "muzzle";
        }

        if (!visible)
        {
            if (spriteComponent.LayerMapTryGet(layerKey, out var removeLayer))
            {
                spriteComponent.LayerSetVisible(removeLayer, false);
            }
            return;
        }

        var prototype = MetaData(ent).EntityPrototype;
        if (prototype is null)
            return;

        string? state = null;
        var postfix = !string.IsNullOrEmpty(ent.Comp.StatePostfix) ? ent.Comp.StatePostfix : layerKey;

        var specificState = $"{prototype.ID}-{postfix}";
        var baseState = $"base-{postfix}";

        if (spriteComponent.BaseRSI.TryGetState(specificState, out _))
            state = specificState;
        else if (spriteComponent.BaseRSI.TryGetState(baseState, out _))
            state = baseState;
        else if (spriteComponent.BaseRSI.TryGetState(postfix, out _))
            state = postfix;

        if (state is null)
            return;

        if (!spriteComponent.LayerMapTryGet(layerKey, out var layer))
        {
            layer = spriteComponent.LayerMapReserveBlank(layerKey);
        }

        spriteComponent.LayerSetState(layer, state);
        spriteComponent.LayerSetVisible(layer, true);
    }
}
