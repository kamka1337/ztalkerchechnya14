using Content.Client._Session.LimbHealth.Widgets;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._Session.LimbHealth;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;

namespace Content.Client._Session.LimbHealth;

[UsedImplicitly]
public sealed class KuklaUiController : UIController, IOnStateEntered<GameplayState>, IOnSystemChanged<KuklaSystem>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IInputManager _input = default!;

    private KuklaWidget? UI => UIManager.ActiveScreen?.GetWidget<KuklaWidget>();
    private KuklaSystem? _system;

    public override void Initialize()
    {
        base.Initialize();

        var load = UIManager.GetUIController<GameplayStateLoadController>();
        load.OnScreenLoad += OnScreenLoad;
        load.OnScreenUnload += OnScreenUnload;

        _input.FirstChanceOnKeyEvent += OnFirstChanceKeyEvent;

        _input.SetInputCommand(ContentKeyFunctions.SelectLimbHead, InputCmdHandler.FromDelegate(_ => _system?.SelectLimb(LimbType.Head)));
        _input.SetInputCommand(ContentKeyFunctions.SelectLimbChest, InputCmdHandler.FromDelegate(_ => _system?.SelectLimb(LimbType.Chest)));
        _input.SetInputCommand(ContentKeyFunctions.SelectLimbAbdomen, InputCmdHandler.FromDelegate(_ => _system?.SelectLimb(LimbType.Abdomen)));
        _input.SetInputCommand(ContentKeyFunctions.SelectLimbLeftArm, InputCmdHandler.FromDelegate(_ => _system?.SelectLimb(LimbType.LeftArm)));
        _input.SetInputCommand(ContentKeyFunctions.SelectLimbRightArm, InputCmdHandler.FromDelegate(_ => _system?.SelectLimb(LimbType.RightArm)));
        _input.SetInputCommand(ContentKeyFunctions.SelectLimbLeftLeg, InputCmdHandler.FromDelegate(_ => _system?.SelectLimb(LimbType.LeftLeg)));
        _input.SetInputCommand(ContentKeyFunctions.SelectLimbRightLeg, InputCmdHandler.FromDelegate(_ => _system?.SelectLimb(LimbType.RightLeg)));
    }

    public void OnSystemLoaded(KuklaSystem system)
    {
        _system = system;
        system.Updated += Refresh;
    }

    public void OnSystemUnloaded(KuklaSystem system)
    {
        system.Updated -= Refresh;
        _system = null;
    }

    public void OnStateEntered(GameplayState state)
    {
        Refresh();
    }

    private void OnScreenLoad()
    {
        if (UI is { } ui)
            ui.LimbClicked += OnLimbClicked;
        Refresh();
    }

    private void OnScreenUnload()
    {
        if (UI is { } ui)
            ui.LimbClicked -= OnLimbClicked;
    }

    private void OnLimbClicked(LimbType limb)
    {
        _system?.SelectLimb(limb);
    }

    private void OnFirstChanceKeyEvent(KeyEventArgs args, KeyEventType type)
    {
        if (type != KeyEventType.Down || args.Handled)
            return;

        // Never intercept when user is typing in a text field (chat, search, console, etc.)
        if (UIManager.KeyboardFocused != null)
            return;

        // Only active if the player is attached to an entity with limb health
        if (_player.LocalEntity is not { } uid || !EntityManager.HasComponent<LimbHealthComponent>(uid))
            return;

        LimbType? target = args.Key switch
        {
            Keyboard.Key.NumpadNum8 => LimbType.Head,
            Keyboard.Key.NumpadNum5 => LimbType.Chest,
            Keyboard.Key.NumpadNum2 => LimbType.Abdomen,
            Keyboard.Key.NumpadNum4 => LimbType.LeftArm,
            Keyboard.Key.NumpadNum1 => LimbType.LeftLeg,
            Keyboard.Key.NumpadNum6 => LimbType.RightArm,
            Keyboard.Key.NumpadNum3 => LimbType.RightLeg,
            _ => null,
        };

        if (target != null)
        {
            _system?.SelectLimb(target.Value);
            args.Handle();
        }
    }

    private void Refresh()
    {
        if (UI is not { } ui)
            return;

        if (_player.LocalEntity is not { } uid
            || !EntityManager.TryGetComponent<LimbHealthComponent>(uid, out var comp))
        {
            ui.Visible = false;
            return;
        }

        ui.Visible = true;

        var selected = LimbType.Chest;
        var aiming = false;
        if (EntityManager.TryGetComponent<BodyTargetingComponent>(uid, out var targeting))
        {
            selected = targeting.SelectedLimb;
            aiming = targeting.Aiming;
        }

        ui.UpdateState(comp, selected, aiming);
    }
}
