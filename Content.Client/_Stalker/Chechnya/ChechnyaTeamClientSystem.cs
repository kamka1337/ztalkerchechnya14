using System;
using Content.Shared._Stalker.Chechnya;
using Robust.Shared.GameObjects;

namespace Content.Client._Stalker.Chechnya;

public sealed class ChechnyaTeamClientSystem : EntitySystem
{
    public ChechnyaTeam CurrentTeam { get; private set; } = ChechnyaTeam.None;

    public event Action<ChechnyaTeam>? TeamChanged;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ChechnyaTeamSelectedEvent>(OnTeamSelected);
        SubscribeNetworkEvent<ChechnyaTeamResetEvent>(OnTeamReset);
    }

    private void OnTeamSelected(ChechnyaTeamSelectedEvent ev)
    {
        CurrentTeam = ev.Team;
        TeamChanged?.Invoke(CurrentTeam);
    }

    private void OnTeamReset(ChechnyaTeamResetEvent ev)
    {
        CurrentTeam = ChechnyaTeam.None;
        TeamChanged?.Invoke(CurrentTeam);
    }

    public void SelectTeam(ChechnyaTeam team)
    {
        if (CurrentTeam != ChechnyaTeam.None || team == ChechnyaTeam.None)
            return;

        CurrentTeam = team;
        RaiseNetworkEvent(new SelectChechnyaTeamMessage(team));
        TeamChanged?.Invoke(CurrentTeam);
    }
}
