using System.Numerics;
using Content.Shared.Light.Components;
using Content.Shared.Weather;
using Robust.Shared.Map.Components;

namespace Content.Client._Stalker_EN.Atmosphere;

/// <summary>
/// Shared helper for deriving normalized day/dusk/night atmosphere factors and wind state on a map.
/// </summary>
public static class AtmosphereVisualHelper
{
    public static readonly Vector2 CloudWindVelocity = new(0.060f, 0.024f);

    public static bool IsOutdoorSkyMap(IEntityManager entManager, EntityUid mapUid)
    {
        if (!entManager.HasComponent<MapLightComponent>(mapUid))
            return false;

        if (entManager.HasComponent<ImplicitRoofComponent>(mapUid))
            return false;

        return entManager.HasComponent<LightCycleComponent>(mapUid)
            || entManager.HasComponent<SunShadowComponent>(mapUid);
    }

    public static (float DayFactor, float DuskFactor, float NightFactor) GetTimeFactors(
        IEntityManager entManager,
        EntityUid mapUid)
    {
        if (!entManager.TryGetComponent<MapLightComponent>(mapUid, out var mapLight))
            return (1f, 0f, 0f);

        return GetTimeFactors(mapLight.AmbientLightColor);
    }

    public static (float DayFactor, float DuskFactor, float NightFactor) GetTimeFactors(Color ambientColor)
    {
        var luma = 0.2126f * ambientColor.R + 0.7152f * ambientColor.G + 0.0722f * ambientColor.B;
        var warmthDiff = ambientColor.R - ambientColor.B;

        var dayFactor = Math.Clamp((luma - 0.18f) / 0.55f, 0f, 1f);
        var nightFactor = 1f - Math.Clamp((luma - 0.12f) / 0.38f, 0f, 1f);
        var duskFactor = Math.Clamp((warmthDiff - 0.12f) / 0.30f, 0f, 1f) * (1f - nightFactor * 0.7f);

        return (dayFactor, duskFactor, nightFactor);
    }

    public static float GetWeatherCloudBoost(IEntityManager entManager, EntityUid mapUid)
    {
        if (!entManager.TryGetComponent<WeatherComponent>(mapUid, out var weather))
            return 0f;

        return weather.Weather.Count > 0 ? 0.06f : 0f;
    }
}
