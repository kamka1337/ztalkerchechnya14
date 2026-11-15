using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Station.Events;
using Content.Shared._Stalker.Chechnya;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Stalker.Chechnya;

public sealed class ChechnyaTeamSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    private readonly Dictionary<NetUserId, ChechnyaTeam> _playerTeams = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SelectChechnyaTeamMessage>(OnSelectTeamMessage);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<StationJobsGetCandidatesEvent>(OnStationJobsGetCandidates);
    }

    private void OnStationJobsGetCandidates(ref StationJobsGetCandidatesEvent ev)
    {
        for (var i = ev.Jobs.Count - 1; i >= 0; i--)
        {
            var jobId = ev.Jobs[i].Id;
            if (!CanJoinJob(ev.Player, jobId))
            {
                ev.Jobs.RemoveAt(i);
            }
        }
    }

    private void OnSelectTeamMessage(SelectChechnyaTeamMessage ev, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (ev.Team == ChechnyaTeam.None)
            return;

        if (_playerTeams.TryGetValue(session.UserId, out var existingTeam) && existingTeam != ChechnyaTeam.None)
        {
            // Already chosen a team for this round - locked!
            RaiseNetworkEvent(new ChechnyaTeamSelectedEvent(existingTeam), session.Channel);
            return;
        }

        _playerTeams[session.UserId] = ev.Team;
        RaiseNetworkEvent(new ChechnyaTeamSelectedEvent(ev.Team), session.Channel);

        var teamName = ev.Team == ChechnyaTeam.RF ? "ВС РФ" : "ВС ЧРИ";
        Logger.InfoS("chechnya.team", $"Player {session.Name} ({session.UserId}) selected team {teamName}.");
    }

    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        var team = GetPlayerTeam(ev.PlayerSession.UserId);
        RaiseNetworkEvent(new ChechnyaTeamSelectedEvent(team), ev.PlayerSession.Channel);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _playerTeams.Clear();
        RaiseNetworkEvent(new ChechnyaTeamResetEvent());
    }

    public ChechnyaTeam GetPlayerTeam(NetUserId userId)
    {
        return _playerTeams.TryGetValue(userId, out var team) ? team : ChechnyaTeam.None;
    }

    public bool TrySetPlayerTeam(NetUserId userId, ChechnyaTeam team)
    {
        if (_playerTeams.TryGetValue(userId, out var existing) && existing != ChechnyaTeam.None)
            return false;

        _playerTeams[userId] = team;
        if (_playerManager.TryGetSessionById(userId, out var session))
        {
            RaiseNetworkEvent(new ChechnyaTeamSelectedEvent(team), session.Channel);
        }

        return true;
    }

    public bool CanJoinJob(NetUserId userId, string jobId)
    {
        var team = GetPlayerTeam(userId);

        var isRfJob = jobId.StartsWith("ChechnyaRF", StringComparison.OrdinalIgnoreCase);
        var isCriJob = jobId.StartsWith("ChechnyaCRI", StringComparison.OrdinalIgnoreCase);

        // If not a faction-specific job (e.g. observer or neutral), allow
        if (!isRfJob && !isCriJob)
            return true;

        if (team == ChechnyaTeam.RF)
            return isRfJob;

        if (team == ChechnyaTeam.CRI)
            return isCriJob;

        // Has not selected a team yet
        return false;
    }
}
