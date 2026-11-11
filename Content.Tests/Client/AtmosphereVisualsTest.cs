using System;
using System.IO;
using System.Reflection;
using Content.Client._Stalker_EN.Atmosphere;
using NUnit.Framework;
using Robust.Client.Graphics;
using Robust.Shared.ContentPack;
using Robust.Shared.Maths;

namespace Content.Tests.Client;

[Parallelizable]
[TestFixture]
[TestOf(typeof(AtmosphereVisualHelper))]
public sealed class AtmosphereVisualsTest
{
    [Test]
    public void TimeFactorsReflectDayDuskAndNight()
    {
        var (dayD, duskD, nightD) = AtmosphereVisualHelper.GetTimeFactors(Color.FromHex("#F5EDE0FF"));
        Assert.Multiple(() =>
        {
            Assert.That(dayD, Is.GreaterThan(0.9f));
            Assert.That(nightD, Is.LessThan(0.1f));
            Assert.That(duskD, Is.LessThan(0.1f));
        });

        var (_, duskS, nightS) = AtmosphereVisualHelper.GetTimeFactors(new Color(0.80f, 0.48f, 0.22f));
        Assert.Multiple(() =>
        {
            Assert.That(duskS, Is.GreaterThan(0.8f));
            Assert.That(nightS, Is.LessThan(0.2f));
        });

        var (dayN, _, nightN) = AtmosphereVisualHelper.GetTimeFactors(new Color(0.05f, 0.08f, 0.45f));
        Assert.Multiple(() =>
        {
            Assert.That(nightN, Is.GreaterThan(0.9f));
            Assert.That(dayN, Is.LessThan(0.1f));
        });
    }

    [TestCase("atmosphere_film.swsl")]
    [TestCase("cloud_shadows.swsl")]
    public void AtmosphereShadersParseCleanlyWithSwslParser(string shaderFile)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", ".."));
        var shaderPath = Path.Combine(repoRoot, "Resources", "Textures", "_Stalker_EN", "Shaders", shaderFile);
        Assert.That(File.Exists(shaderPath), Is.True, $"Missing shader file: {shaderPath}");

        var parserType = typeof(ShaderPrototype).Assembly.GetType("Robust.Client.Graphics.ShaderParser", throwOnError: true)!;
        var parseMethod = parserType.GetMethod(
            "Parse",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(TextReader), typeof(IResourceManager)],
            null);
        Assert.That(parseMethod, Is.Not.Null);

        using var reader = File.OpenText(shaderPath);
        var parsed = parseMethod!.Invoke(null, [reader, null]);
        Assert.That(parsed, Is.Not.Null);
    }
}
