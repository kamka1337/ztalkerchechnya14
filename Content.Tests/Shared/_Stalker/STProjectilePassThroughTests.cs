using Content.Shared._Stalker.Weapon.Projectile;
using NUnit.Framework;

namespace Content.Tests.Shared._Stalker;

[TestFixture]
[TestOf(typeof(STProjectileSystem))]
public sealed class STProjectilePassThroughTests
{
    [Test]
    public void CalculatePassThroughChance_DefaultSandbagCurve_MatchesExpectedPoints()
    {
        var comp = new STProjectilePassThroughComponent();

        // Point-blank range (<= 1.5 tiles, including 0 and diagonal adjacent ~1.414) -> 100%
        Assert.That(STProjectileSystem.CalculatePassThroughChance(comp, 0f), Is.EqualTo(1f));
        Assert.That(STProjectileSystem.CalculatePassThroughChance(comp, 0.65f), Is.EqualTo(1f));
        Assert.That(STProjectileSystem.CalculatePassThroughChance(comp, 1.0f), Is.EqualTo(1f));
        Assert.That(STProjectileSystem.CalculatePassThroughChance(comp, 1.42f), Is.EqualTo(1f));
        Assert.That(STProjectileSystem.CalculatePassThroughChance(comp, 1.5f), Is.EqualTo(1f));

        // 10 tiles -> 50%
        Assert.That(STProjectileSystem.CalculatePassThroughChance(comp, 10f), Is.EqualTo(0.5f).Within(0.0001f));

        // Midpoint between 1.5 and 10 -> 75%
        Assert.That(STProjectileSystem.CalculatePassThroughChance(comp, 5.75f), Is.EqualTo(0.75f).Within(0.0001f));

        // Monotonic decrease between 1.5 and 18.5
        var c2 = STProjectileSystem.CalculatePassThroughChance(comp, 2f);
        var c5 = STProjectileSystem.CalculatePassThroughChance(comp, 5f);
        var c10 = STProjectileSystem.CalculatePassThroughChance(comp, 10f);
        var c15 = STProjectileSystem.CalculatePassThroughChance(comp, 15f);
        Assert.That(c2, Is.GreaterThan(c5));
        Assert.That(c5, Is.GreaterThan(c10));
        Assert.That(c10, Is.GreaterThan(c15));

        // Long range clamps to MinChance (0%)
        Assert.That(STProjectileSystem.CalculatePassThroughChance(comp, 18.5f), Is.EqualTo(0f).Within(0.0001f));
        Assert.That(STProjectileSystem.CalculatePassThroughChance(comp, 30f), Is.EqualTo(0f));
    }

    [Test]
    public void CalculatePassThroughChance_RespectsMinChanceAndDegenerateRanges()
    {
        Assert.That(
            STProjectileSystem.CalculatePassThroughChance(50f, 2f, 1f, 10f, 0.5f, 0.2f),
            Is.EqualTo(0.2f));

        Assert.That(
            STProjectileSystem.CalculatePassThroughChance(5f, 5f, 1f, 5f, 0.5f, 0.2f),
            Is.EqualTo(1f));

        Assert.That(
            STProjectileSystem.CalculatePassThroughChance(6f, 5f, 1f, 5f, 0.5f, 0.2f),
            Is.EqualTo(0.5f));
    }
}
