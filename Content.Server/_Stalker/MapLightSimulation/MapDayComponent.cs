namespace Content.Server._Stalker.MapLightSimulation;

[RegisterComponent]
public sealed partial class MapDayComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double DayTime = 5400d; // 1,5 hours

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<Color> Colors = new()
    {
        Color.FromHex("#e2d2b722"),
        Color.FromHex("#e2d2b722"),
        Color.FromHex("#e2d2b722"),
        Color.FromHex("#e2d2b722"),
        Color.FromHex("#e2d2b722"),
        Color.FromHex("#e2d2b722"),
        Color.FromHex("#e2d2b722"),
        Color.FromHex("#e2d2b722"),
        Color.FromHex("#e2d2b722"),
        Color.FromHex("#e2d2b722"),
        Color.FromHex("#e2d2b722"),
        Color.FromHex("#e2d2b722"),
    };
}
