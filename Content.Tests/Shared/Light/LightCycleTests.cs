using System;
using Content.Client.Light.EntitySystems;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Tests.Shared.Light;

[Parallelizable]
[TestFixture]
[TestOf(typeof(SharedLightCycleSystem))]
public sealed class LightCycleTests
{
    [Test]
    public void GetColorVariesAcrossDayNightCycle()
    {
        var comp = new LightCycleComponent
        {
            Duration = TimeSpan.FromMinutes(30),
            MinLightLevel = 0.42f,
            Enabled = true
        };
        var ent = (default(EntityUid), comp);
        var baseColor = Color.FromHex("#F5EDE0FF");

        var totalSeconds = (float) comp.Duration.TotalSeconds;
        var midnight = SharedLightCycleSystem.GetColor(ent, baseColor, 0f);
        var dawn = SharedLightCycleSystem.GetColor(ent, baseColor, totalSeconds * 0.16f);
        var noon = SharedLightCycleSystem.GetColor(ent, baseColor, totalSeconds * 0.50f);
        var dusk = SharedLightCycleSystem.GetColor(ent, baseColor, totalSeconds * 0.84f);

        Assert.Multiple(() =>
        {
            Assert.That(midnight, Is.Not.EqualTo(noon), "Night and midday must not be frozen to the same color.");
            Assert.That(noon.RByte, Is.GreaterThan(midnight.RByte));
            Assert.That(noon.GByte, Is.GreaterThan(midnight.GByte));
            Assert.That(midnight.BByte, Is.GreaterThan(midnight.RByte), "Night should keep a cool blue moonlight tint.");
            Assert.That(dawn.RByte, Is.GreaterThan(dawn.BByte), "Dawn should be warmer than midnight blue.");
            Assert.That(dusk.RByte, Is.GreaterThan(dusk.BByte), "Dusk should be warmer than midnight blue.");
        });
    }

    [Test]
    public void GetColorHandlesNegativeTime()
    {
        var comp = new LightCycleComponent
        {
            Duration = TimeSpan.FromMinutes(30),
            MinLightLevel = 0.42f,
            Enabled = true
        };
        var ent = (default(EntityUid), comp);
        var baseColor = Color.FromHex("#F5EDE0FF");

        var totalSeconds = (float) comp.Duration.TotalSeconds;
        var wrapped = SharedLightCycleSystem.GetColor(ent, baseColor, -totalSeconds * 0.25f);
        var equivalent = SharedLightCycleSystem.GetColor(ent, baseColor, totalSeconds * 0.75f);

        Assert.That(wrapped, Is.EqualTo(equivalent));
    }

    [Test]
    public void SunShadowVariesSmoothlyAndNeverThrowsAtZero()
    {
        var sys = new SunShadowSystem();
        var comp = new SunShadowCycleComponent
        {
            Duration = TimeSpan.FromMinutes(30)
        };
        var ent = (default(EntityUid), comp);
        var total = (float) comp.Duration.TotalSeconds;

        var midnight = sys.GetShadow(ent, 0f);
        var morning = sys.GetShadow(ent, total * 0.25f);
        var noon = sys.GetShadow(ent, total * 0.50f);
        var evening = sys.GetShadow(ent, total * 0.75f);
        var lateNight = sys.GetShadow(ent, total * 0.99f);

        Assert.Multiple(() =>
        {
            Assert.That(midnight.Alpha, Is.EqualTo(0f));
            Assert.That(morning.Direction.X, Is.LessThan(0f));
            Assert.That(noon.Alpha, Is.GreaterThan(morning.Alpha));
            Assert.That(evening.Direction.X, Is.GreaterThan(0f));
            Assert.That(lateNight.Alpha, Is.LessThan(evening.Alpha));
        });
    }
}
