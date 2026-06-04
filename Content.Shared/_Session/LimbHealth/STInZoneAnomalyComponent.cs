namespace Content.Shared._Session.LimbHealth;

[RegisterComponent]
public sealed partial class STInZoneAnomalyComponent : Component
{
    public readonly HashSet<EntityUid> Sources = new();
}
