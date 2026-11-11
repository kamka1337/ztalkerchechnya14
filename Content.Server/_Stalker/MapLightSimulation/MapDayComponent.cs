namespace Content.Server._Stalker.MapLightSimulation;

[RegisterComponent]
public sealed partial class MapDayComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double DayTime = 5400d; // 1,5 hours

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<Color> Colors = new()
    {
        // 00:00 - глубокая ночь (тёмно-синяя)
        Color.FromHex("#1A2244FF"),
        // 03:00 - предрассветная тьма
        Color.FromHex("#24325AFF"),
        // 06:00 - рассвет (тёплый оранжево-розовый)
        Color.FromHex("#C27645FF"),
        // 09:00 - утро (тёплый золотисто-кремовый)
        Color.FromHex("#E6CFA8FF"),
        // 12:00 - полдень (мягкий дневной свет)
        Color.FromHex("#F5EDE0FF"),
        // 15:00 - день
        Color.FromHex("#EFE2C8FF"),
        // 18:00 - закат (оранжево-янтарный)
        Color.FromHex("#C46B38FF"),
        // 19:30 - глубокий закат
        Color.FromHex("#6B3A38FF"),
        // 21:00 - сумерки (тёмно-сиреневый)
        Color.FromHex("#2A284EFF"),
        // 24:00 - ночь (цикл замыкается)
        Color.FromHex("#1A2244FF"),
    };
}
