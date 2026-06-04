namespace Content.Shared._Session.LimbHealth;

public sealed class LimbDestroyedEvent : EntityEventArgs
{
    public readonly EntityUid Owner;
    public readonly LimbType Limb;

    public LimbDestroyedEvent(EntityUid owner, LimbType limb)
    {
        Owner = owner;
        Limb = limb;
    }
}

public sealed class LimbRestoredEvent : EntityEventArgs
{
    public readonly EntityUid Owner;
    public readonly LimbType Limb;

    public LimbRestoredEvent(EntityUid owner, LimbType limb)
    {
        Owner = owner;
        Limb = limb;
    }
}
