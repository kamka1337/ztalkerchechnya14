using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._Stalker.Weapon.GrenadeLauncher;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Stalker.Weapon.GrenadeLauncher;

/// <summary>
/// Server-side system for the GP-25 underslung grenade launcher.
/// Handles firing via the left-side action button (STFireGP25ActionEvent),
/// manual loading by clicking with a VOG grenade in hand, and the unload verbs.
/// </summary>
public sealed class STGrenadeLauncherSystem : STSharedGrenadeLauncherSystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private static readonly ProtoId<TagPrototype> VogTag = "Vog25grenade";
    private readonly Dictionary<EntityUid, TimeSpan> _blockedShootUntil = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunComponent, STFireGP25ActionEvent>(OnFireAction);
        SubscribeLocalEvent<GunComponent, ShotAttemptedEvent>(OnGunShotAttempted);
        
        SubscribeLocalEvent<GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<GunComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<GunComponent, EntRemovedFromContainerMessage>(OnGunContainerRemoved);
    }

    // ─── Left-side Action Firing ───────────────────────────────────────────────

    private void OnFireAction(Entity<GunComponent> gun, ref STFireGP25ActionEvent args)
    {
        if (args.Handled)
            return;

        // Block the host gun from firing regular bullets on this click/tick
        var blockUntil = _gameTiming.CurTime + TimeSpan.FromSeconds(0.75f);
        _blockedShootUntil[gun.Owner] = blockUntil;
        _blockedShootUntil[args.Performer] = blockUntil;

        if (TryComp<WieldableComponent>(gun, out var wieldable) && !wieldable.Wielded)
        {
            _popup.PopupEntity(Loc.GetString("st-grenade-launcher-must-wield"), gun, args.Performer);
            args.Handled = true;
            return;
        }

        if (HasComp<PacifiedComponent>(args.Performer))
        {
            _popup.PopupEntity(Loc.GetString("pacified-cannot-fire-gun", ("entity", gun)), gun, args.Performer);
            args.Handled = true;
            return;
        }

        if (!TryGetAttachedLauncher(gun, out var launcher, out var launcherComp))
        {
            args.Handled = true;
            return;
        }

        if (!_itemSlots.TryGetSlot(launcher, launcherComp.ChamberSlotId, out var slot) || slot.Item is not { } grenadeEnt)
        {
            _popup.PopupEntity(Loc.GetString("st-grenade-launcher-empty"), gun, args.Performer);
            args.Handled = true;
            return;
        }

        FireGrenade(gun, launcher, launcherComp, grenadeEnt, args.Target, args.Performer);
        args.Handled = true;
    }

    private void OnGunShotAttempted(Entity<GunComponent> gun, ref ShotAttemptedEvent args)
    {
        if (_blockedShootUntil.TryGetValue(gun.Owner, out var until) && _gameTiming.CurTime < until)
        {
            args.Cancel();
            return;
        }

        if (_blockedShootUntil.TryGetValue(args.User, out var userUntil) && _gameTiming.CurTime < userUntil)
        {
            args.Cancel();
            return;
        }
    }



    private void OnGunContainerRemoved(Entity<GunComponent> gun, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != LauncherSlotId)
            return;

        if (!TryComp<STGrenadeLauncherModuleComponent>(args.Entity, out var launcherComp) || launcherComp.FireActionEntity is not { } actionEnt)
            return;

        if (!TerminatingOrDeleted(gun) && !TerminatingOrDeleted(actionEnt))
            Del(actionEnt);

        launcherComp.FireActionEntity = null;
        if (!TerminatingOrDeleted(args.Entity))
            Dirty(args.Entity, launcherComp);
    }

    // ─── Context Verbs (Unload only; loading is strictly manual) ──────────────

    private void OnGetAlternativeVerbs(GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var gun = args.Target;
        if (!HasComp<GunComponent>(gun))
            return;

        var user = args.User;

        if (!TryGetAttachedLauncher(gun, out var launcher, out var launcherComp))
            return;

        if (!_itemSlots.TryGetSlot(launcher, launcherComp.ChamberSlotId, out var slot) || slot.Item is not { } grenadeEnt)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("st-grenade-launcher-verb-unload"),
            Category = VerbCategory.Eject,
            IconEntity = GetNetEntity(grenadeEnt),
            Priority = 2,
            Act = () =>
            {
                if (_itemSlots.TryEjectToHands(launcher, slot, user))
                {
                    _popup.PopupEntity(Loc.GetString("st-grenade-launcher-unloaded"), gun, user);
                }
            }
        });
    }

    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var gun = args.Target;
        if (!HasComp<GunComponent>(gun))
            return;

        var user = args.User;

        if (!TryGetAttachedLauncher(gun, out var launcher, out var launcherComp))
            return;

        if (!_itemSlots.TryGetSlot(launcher, launcherComp.ChamberSlotId, out var slot) || slot.Item is not { } grenadeEnt)
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("st-grenade-launcher-verb-unload"),
            IconEntity = GetNetEntity(grenadeEnt),
            Priority = 2,
            Act = () =>
            {
                if (_itemSlots.TryEjectToHands(launcher, slot, user))
                {
                    _popup.PopupEntity(Loc.GetString("st-grenade-launcher-unloaded"), gun, user);
                }
            }
        });
    }

    // ─── Manual Loading via InteractUsing ──────────────────────────────────────

    private void OnInteractUsing(Entity<GunComponent> gun, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_tagSystem.HasTag(args.Used, VogTag))
            return;

        if (HasComp<PacifiedComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("pacified-cannot-fire-gun", ("entity", gun)), gun, args.User);
            args.Handled = true;
            return;
        }

        if (!TryGetAttachedLauncher(gun, out var launcher, out var launcherComp))
            return;

        if (!_itemSlots.TryGetSlot(launcher, launcherComp.ChamberSlotId, out var slot))
            return;

        if (slot.Item != null)
        {
            _popup.PopupEntity(Loc.GetString("st-grenade-launcher-already-loaded"), gun, args.User);
            args.Handled = true;
            return;
        }

        if (_itemSlots.TryInsert(launcher, launcherComp.ChamberSlotId, args.Used, args.User))
        {
            if (launcherComp.SoundLoad != null)
                _audio.PlayPvs(launcherComp.SoundLoad, gun);

            _popup.PopupEntity(Loc.GetString("st-grenade-launcher-loaded", ("grenade", args.Used)), gun, args.User);
            args.Handled = true;
        }
    }

    // ─── Fire ─────────────────────────────────────────────────────────────────

    private void FireGrenade(
        EntityUid gun,
        EntityUid launcher,
        STGrenadeLauncherModuleComponent launcherComp,
        EntityUid grenadeEntity,
        EntityCoordinates targetCoords,
        EntityUid user)
    {
        if (HasComp<PacifiedComponent>(user))
        {
            _popup.PopupEntity(Loc.GetString("pacified-cannot-fire-gun", ("entity", gun)), gun, user);
            return;
        }

        if (!TryComp<CartridgeAmmoComponent>(grenadeEntity, out var cartridge))
            return;

        var proto = cartridge.Prototype;

        var originMap = _transform.GetMapCoordinates(gun);
        var targetMap = _transform.ToMapCoordinates(targetCoords);

        if (originMap.MapId != targetMap.MapId)
            return;

        var direction = targetMap.Position - originMap.Position;
        if (direction == Vector2.Zero)
            direction = Vector2.UnitX;
        var mapDirection = direction.Normalized();

        var projectile = Spawn(proto, originMap);

        var gunVelocity = Vector2.Zero;
        if (TryComp<PhysicsComponent>(gun, out var gunPhysics))
            gunVelocity = gunPhysics.LinearVelocity;

        _gunSystem.ShootProjectile(projectile, mapDirection, gunVelocity, gun, user, launcherComp.ProjectileSpeed);

        if (launcherComp.SoundShot != null)
            _audio.PlayPvs(launcherComp.SoundShot, gun);

        _itemSlots.TryEject(launcher, launcherComp.ChamberSlotId, user, out _);
        if (!TerminatingOrDeleted(grenadeEntity))
            Del(grenadeEntity);
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    public bool TryGetAttachedLauncher(
        EntityUid gun,
        out EntityUid launcherEntity,
        [NotNullWhen(true)] out STGrenadeLauncherModuleComponent? launcherComp)
    {
        launcherEntity = EntityUid.Invalid;
        launcherComp = null;

        if (!_itemSlots.TryGetSlot(gun, LauncherSlotId, out var slot))
            return false;

        if (slot.Item is not { } item)
            return false;

        if (!TryComp(item, out launcherComp))
            return false;

        launcherEntity = item;
        return true;
    }
}
