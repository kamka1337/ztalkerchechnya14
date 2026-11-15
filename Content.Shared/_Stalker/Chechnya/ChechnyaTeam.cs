using System;
using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.Chechnya;

[Serializable, NetSerializable]
public enum ChechnyaTeam : byte
{
    None = 0,
    RF = 1,
    CRI = 2
}

[Serializable, NetSerializable]
public sealed class SelectChechnyaTeamMessage : EntityEventArgs
{
    public ChechnyaTeam Team { get; }

    public SelectChechnyaTeamMessage(ChechnyaTeam team)
    {
        Team = team;
    }
}

[Serializable, NetSerializable]
public sealed class ChechnyaTeamSelectedEvent : EntityEventArgs
{
    public ChechnyaTeam Team { get; }

    public ChechnyaTeamSelectedEvent(ChechnyaTeam team)
    {
        Team = team;
    }
}

[Serializable, NetSerializable]
public sealed class ChechnyaTeamResetEvent : EntityEventArgs
{
}
