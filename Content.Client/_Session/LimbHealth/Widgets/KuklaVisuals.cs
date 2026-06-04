using Content.Shared._Session.LimbHealth;

namespace Content.Client._Session.LimbHealth.Widgets;

public static class KuklaVisuals
{
    public const string Base = "/Textures/_Session/Interface/LimbHealth/";

    public static string ColorState(LimbState st, float frac)
    {
        if (st.Destroyed)
            return "black";
        if (frac > 0.75f)
            return "green";
        if (frac > 0.50f)
            return "yellow";
        if (frac > 0.25f)
            return "orange";
        return "red";
    }

    public static string? EffectIcon(LimbState st)
    {
        if ((st.Effects & LimbEffect.Fracture) != 0)
            return "perelom";
        if ((st.Effects & LimbEffect.HeavyBleeding) != 0)
            return "BIG_KROVOTEK";
        if ((st.Effects & LimbEffect.Bleeding) != 0)
            return "SMALL_KROVOTEK";
        if ((st.Effects & LimbEffect.Painkiller) != 0)
            return "OBEZBOL";
        if ((st.Effects & LimbEffect.Good) != 0)
            return "good_effect";
        if ((st.Effects & LimbEffect.Bad) != 0)
            return "bad_effect";
        return null;
    }
}
