using Content.Shared._Session.LimbHealth;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Client._Session.LimbHealth;

public sealed class KuklaSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    public event Action? Updated;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimbHealthComponent, AfterAutoHandleStateEvent>(OnLimbState);
        SubscribeLocalEvent<BodyTargetingComponent, AfterAutoHandleStateEvent>(OnTargetState);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnDetached);
    }

    private void OnLimbState(Entity<LimbHealthComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Owner == _player.LocalEntity)
            Updated?.Invoke();
    }

    private void OnTargetState(Entity<BodyTargetingComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Owner == _player.LocalEntity)
            Updated?.Invoke();
    }

    private void OnAttached(LocalPlayerAttachedEvent args)
    {
        Updated?.Invoke();
    }

    private void OnDetached(LocalPlayerDetachedEvent args)
    {
        Updated?.Invoke();
    }

    public void SelectLimb(LimbType limb)
    {
        var aiming = true;
        if (_player.LocalEntity is { } uid && TryComp<BodyTargetingComponent>(uid, out var comp))
        {
            aiming = !(comp.Aiming && comp.SelectedLimb == limb);
            comp.SelectedLimb = limb;
            comp.Aiming = aiming;
            Updated?.Invoke();
        }

        RaiseNetworkEvent(new SelectLimbEvent(limb, aiming));
    }

    public void SetSelectedLimb(LimbType limb)
    {
        var aiming = false;
        if (_player.LocalEntity is { } uid && TryComp<BodyTargetingComponent>(uid, out var comp))
        {
            aiming = comp.Aiming;
            comp.SelectedLimb = limb;
            Updated?.Invoke();
        }

        RaiseNetworkEvent(new SelectLimbEvent(limb, aiming));
    }
}
