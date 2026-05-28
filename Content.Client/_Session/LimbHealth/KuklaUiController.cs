using Content.Client._Session.LimbHealth.Widgets;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._Session.LimbHealth;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Session.LimbHealth;

[UsedImplicitly]
public sealed class KuklaUiController : UIController, IOnStateEntered<GameplayState>, IOnSystemChanged<KuklaSystem>
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private KuklaWidget? UI => UIManager.ActiveScreen?.GetWidget<KuklaWidget>();
    private KuklaSystem? _system;

    public override void Initialize()
    {
        base.Initialize();

        var load = UIManager.GetUIController<GameplayStateLoadController>();
        load.OnScreenLoad += OnScreenLoad;
        load.OnScreenUnload += OnScreenUnload;
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

        var selected = EntityManager.TryGetComponent<BodyTargetingComponent>(uid, out var targeting)
            ? targeting.SelectedLimb
            : LimbType.Chest;

        ui.UpdateState(comp, selected);
    }
}
