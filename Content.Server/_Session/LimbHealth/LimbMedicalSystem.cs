using Content.Shared._Session.LimbHealth;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;

namespace Content.Server._Session.LimbHealth;

public sealed class LimbMedicalSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedBodyTargetingSystem _targeting = default!;
    [Dependency] private readonly LimbHealthSystem _limbs = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimbSyringeComponent, AfterInteractEvent>(OnSyringeInteract);
        SubscribeLocalEvent<LimbSyringeComponent, LimbSyringeDoAfterEvent>(OnSyringeDoAfter);

        SubscribeLocalEvent<LimbSurgeryToolComponent, AfterInteractEvent>(OnSurgeryInteract);
        SubscribeLocalEvent<LimbSurgeryToolComponent, LimbSurgeryDoAfterEvent>(OnSurgeryDoAfter);

        SubscribeLocalEvent<LimbBleedCureComponent, AfterInteractEvent>(OnBleedCureInteract);
        SubscribeLocalEvent<LimbBleedCureComponent, LimbBleedCureDoAfterEvent>(OnBleedCureDoAfter);
    }

    private LimbType GetSelected(EntityUid user)
    {
        return _targeting.TryGetSelected(user, out var limb) ? limb : LimbType.Chest;
    }

    private void OnSyringeInteract(Entity<LimbSyringeComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!HasComp<LimbHealthComponent>(target))
            return;

        args.Handled = true;

        if (ent.Comp.Spent)
        {
            _popup.PopupEntity("Шприц уже использован.", ent.Owner, args.User);
            return;
        }

        var limb = GetSelected(args.User);

        if (_limbs.IsDestroyed(target, limb))
        {
            _popup.PopupEntity("Конечность выбита - шприц не поможет.", target, args.User);
            return;
        }

        var ev = new LimbSyringeDoAfterEvent(limb);
        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, ev, ent.Owner, target, ent.Owner)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
            BlockDuplicate = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnSyringeDoAfter(Entity<LimbSyringeComponent> ent, ref LimbSyringeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target is not { } target)
            return;

        args.Handled = true;

        if (ent.Comp.Spent || !TryComp<LimbHealthComponent>(target, out var comp))
            return;

        if (_limbs.IsDestroyed(target, args.Limb, comp))
            return;

        var healed = _limbs.HealLimbAmount(target, args.Limb, ent.Comp.HealAmount, comp);

        if (healed <= FixedPoint2.Zero)
        {
            _popup.PopupEntity("Нечего лечить этим шприцом.", target, args.Args.User);
            return;
        }

        SpendSyringe(ent);
    }

    private void SpendSyringe(Entity<LimbSyringeComponent> ent)
    {
        ent.Comp.Spent = true;
        _appearance.SetData(ent.Owner, SyringeVisuals.Spent, true);
    }

    private void OnBleedCureInteract(Entity<LimbBleedCureComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!HasComp<LimbHealthComponent>(target))
            return;

        args.Handled = true;

        var limb = GetSelected(args.User);

        if (ent.Comp.ArmsLegsOnly && !(limb.IsArm() || limb.IsLeg()))
        {
            _popup.PopupEntity("Жгут накладывается только на руки и ноги.", target, args.User);
            return;
        }

        if (!_limbs.TryGetBleed(target, limb, out var heavy))
        {
            _popup.PopupEntity("На этой конечности нет кровотечения.", target, args.User);
            return;
        }

        if (heavy && !ent.Comp.CuresHeavy)
        {
            _popup.PopupEntity("Сильное кровотечение бинтом не остановить.", target, args.User);
            return;
        }

        var ev = new LimbBleedCureDoAfterEvent(limb);
        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, ev, ent.Owner, target, ent.Owner)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
            BlockDuplicate = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnBleedCureDoAfter(Entity<LimbBleedCureComponent> ent, ref LimbBleedCureDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target is not { } target)
            return;

        args.Handled = true;

        if (!_limbs.CureBleed(target, args.Limb, ent.Comp.CuresHeavy))
            return;

        if (TryComp<LimitedChargesComponent>(ent.Owner, out _))
        {
            _charges.TryUseCharge(ent.Owner);
            if (_charges.IsEmpty(ent.Owner))
                QueueDel(ent.Owner);
        }
        else
        {
            QueueDel(ent.Owner);
        }
    }

    private void OnSurgeryInteract(Entity<LimbSurgeryToolComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp<LimbHealthComponent>(target, out var comp))
            return;

        args.Handled = true;

        if (_mobState.IsDead(target))
        {
            _popup.PopupEntity("Бесполезно - пациент мёртв.", target, args.User);
            return;
        }

        var limb = GetSelected(args.User);

        if (limb is LimbType.Head or LimbType.Chest)
        {
            _popup.PopupEntity("Это ранение необратимо - зашить невозможно.", target, args.User);
            return;
        }

        if (!comp.Limbs.TryGetValue(limb, out var st) || !st.Destroyed)
        {
            _popup.PopupEntity("Эта конечность не выбита.", target, args.User);
            return;
        }

        if (ent.Comp.Stage == LimbSurgeryStage.Needle
            && TryComp<LimitedChargesComponent>(ent.Owner, out _)
            && _charges.IsEmpty(ent.Owner))
        {
            _popup.PopupEntity("Игла израсходована.", ent.Owner, args.User);
            return;
        }

        if (ent.Comp.Stage == LimbSurgeryStage.Scissors && !comp.NeedledLimbs.Contains(limb))
        {
            _popup.PopupEntity("Сначала введите мед. иглу.", target, args.User);
            return;
        }

        if (ent.Comp.Stage == LimbSurgeryStage.Needle && comp.NeedledLimbs.Contains(limb))
        {
            _popup.PopupEntity("Игла уже введена - используйте тактические ножницы.", target, args.User);
            return;
        }

        var ev = new LimbSurgeryDoAfterEvent(ent.Comp.Stage, limb);
        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, ev, ent.Owner, target, ent.Owner)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
            BlockDuplicate = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnSurgeryDoAfter(Entity<LimbSurgeryToolComponent> ent, ref LimbSurgeryDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target is not { } target)
            return;

        args.Handled = true;

        if (!TryComp<LimbHealthComponent>(target, out var comp))
            return;

        if (_mobState.IsDead(target))
            return;

        if (args.Limb is LimbType.Head or LimbType.Chest)
            return;

        if (!comp.Limbs.TryGetValue(args.Limb, out var st) || !st.Destroyed)
            return;

        if (args.Stage == LimbSurgeryStage.Needle)
        {
            if (TryComp<LimitedChargesComponent>(ent.Owner, out _) && !_charges.TryUseCharge(ent.Owner))
            {
                _popup.PopupEntity("Игла израсходована.", ent.Owner, args.Args.User);
                return;
            }

            comp.NeedledLimbs.Add(args.Limb);
            _popup.PopupEntity("Мед. игла введена. Завершите тактическими ножницами.", target, args.Args.User);
        }
        else
        {
            if (!comp.NeedledLimbs.Contains(args.Limb))
                return;

            comp.NeedledLimbs.Remove(args.Limb);
            _limbs.RestoreDestroyedLimb(target, args.Limb, comp);
            _popup.PopupEntity("Конечность восстановлена.", target, args.Args.User);
        }
    }
}
