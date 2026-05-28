using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._Session.LimbHealth;

public sealed class SharedBodyTargetingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SelectLimbEvent>(OnSelectLimb);
    }

    private void OnSelectLimb(SelectLimbEvent ev, EntitySessionEventArgs args)
    {
        if (!_net.IsServer)
            return;

        if (args.SenderSession.AttachedEntity is not { } uid)
            return;

        var comp = EnsureComp<BodyTargetingComponent>(uid);
        comp.SelectedLimb = ev.Limb;
        Dirty(uid, comp);
    }

    public bool TryGetSelected(EntityUid? attacker, out LimbType limb)
    {
        limb = LimbType.Chest;
        if (attacker is not { } uid)
            return false;

        if (!TryComp<BodyTargetingComponent>(uid, out var comp))
            return false;

        limb = comp.SelectedLimb;
        return true;
    }
}
