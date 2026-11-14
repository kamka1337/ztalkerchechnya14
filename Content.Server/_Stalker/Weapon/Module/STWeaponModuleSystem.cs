using Content.Server._DZ.FarGunshot;
using Content.Server._Stalker.Weapon.Scoping;
using Content.Shared._Stalker.Weapon.Module;
using Content.Shared._Stalker.Weapon.Module.Effects;
using Content.Shared._Stalker.Weapon.Scoping;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server._Stalker.Weapon.Module;

public sealed class STWeaponModuleSystem : STSharedWeaponModuleSystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly STScopeSystem _sharedScope = default!;

    private EntityQuery<ContainerManagerComponent> _containerMangerQuery;
    private EntityQuery<STWeaponModuleContainerComponent> _containerModuleQuery;

    public override void Initialize()
    {
        base.Initialize();

        _containerMangerQuery = GetEntityQuery<ContainerManagerComponent>();
        _containerModuleQuery = GetEntityQuery<STWeaponModuleContainerComponent>();

        SubscribeLocalEvent<STWeaponModuleComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<STWeaponModuleComponent, EntGotRemovedFromContainerMessage>(OnRemoved);

        SubscribeLocalEvent<STWeaponModuleContainerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<STWeaponModuleContainerComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
    }

    private void OnInserted(Entity<STWeaponModuleComponent> entity, ref EntGotInsertedIntoContainerMessage args)
    {
        UpdateAllContainerEffects(args.Container.Owner);
    }

    private void OnRemoved(Entity<STWeaponModuleComponent> entity, ref EntGotRemovedFromContainerMessage args)
    {
        UpdateAllContainerEffects(args.Container.Owner);
    }

    private void OnInit(Entity<STWeaponModuleContainerComponent> entity, ref ComponentInit args)
    {
        entity.Comp.CachedEffect = new STWeaponModuleEffect();
        entity.Comp.CachedScopeEffect = null;
        entity.Comp.IntegratedScopeEffect = HasComp<ScopeComponent>(entity);

        UpdateAllContainerEffects(entity);
    }

    private void OnGunRefreshModifiers(Entity<STWeaponModuleContainerComponent> entity, ref GunRefreshModifiersEvent args)
    {
        var effect = entity.Comp.CachedEffect;

        args.FireRate *= effect.FireRateModifier;
        args.AngleDecay *= effect.AngleDecayModifier;
        args.AngleIncrease *= effect.AngleIncreaseModifier;
        args.MinAngle *= effect.MinAngleModifier;
        args.MaxAngle *= effect.MaxAngleModifier;
        args.ProjectileSpeed *= effect.ProjectileSpeedModifier;

        if (TryComp(entity.Owner, out FarGunshotComponent? farGunshotComponent))
        {
            farGunshotComponent.SilencerDecrease = effect.FarshotSoundDecrease < 0.99f
                ? effect.FarshotSoundDecrease
                : null;
        }

        if (args.SoundGunshot is null)
            return;

        if (effect.SoundGunshotVolumeAddition != 0)
        {
            var audioParams = args.SoundGunshot.Params;
            audioParams.Volume += effect.SoundGunshotVolumeAddition;

            if (args.SoundGunshot is SoundPathSpecifier pathSpec)
            {
                args.SoundGunshot = new SoundPathSpecifier(pathSpec.Path, audioParams);
            }
            else if (args.SoundGunshot is SoundCollectionSpecifier colSpec && colSpec.Collection != null)
            {
                args.SoundGunshot = new SoundCollectionSpecifier(colSpec.Collection, audioParams);
            }
        }
    }

    private void UpdateAllContainerEffects(EntityUid entityUid)
    {
        if (!_containerModuleQuery.TryGetComponent(entityUid, out var containerComponent))
            return;

        UpdateAllContainerEffects((entityUid, containerComponent));
    }

    private void UpdateAllContainerEffects(Entity<STWeaponModuleContainerComponent> entity)
    {
        if (!_containerMangerQuery.TryGetComponent(entity, out var containerComponent))
            return;

        var effect = new STWeaponModuleEffect();
        STWeaponModuleScopeEffect? scopeEffect = null;

        foreach (var (_, container) in containerComponent.Containers)
        {
            foreach (var containedEntity in container.ContainedEntities)
            {
                if (!TryComp<STWeaponModuleComponent>(containedEntity, out var moduleComponent))
                    continue;

                effect = STWeaponModuleEffect.Merge(effect, moduleComponent.Effect);

                if (moduleComponent.ScopeEffect is null)
                    continue;

                scopeEffect ??= moduleComponent.ScopeEffect;
                scopeEffect = STWeaponModuleScopeEffect.Merge(scopeEffect.Value, moduleComponent.ScopeEffect.Value);
            }
        }

        var modeDelta = effect.AdditionalAvailableModes ^ entity.Comp.CachedEffect.AdditionalAvailableModes;

        entity.Comp.CachedEffect = effect;
        Dirty(entity);

        if (!entity.Comp.IntegratedScopeEffect)
            _sharedScope.TrySet(entity.Owner, scopeEffect);

        if (!TryComp<GunComponent>(entity, out var gun))
            return;

        // Here bit mask works like switch and switch modes that changed
        _gun.SetAvailableModes(entity, gun.AvailableModes ^ modeDelta, gun);
        _gun.RefreshModifiers((entity, gun));
    }
}
