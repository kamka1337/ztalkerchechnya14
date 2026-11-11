using System;
using Content.Client._Stalker_EN.Atmosphere;
using Content.Client._Stalker_EN.Overlays;
using Content.Shared.Light.Components;
using Robust.Client.Graphics;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Light;

[TestFixture]
public sealed class AtmosphereAndDayCycleTest
{
    private static readonly ProtoId<ShaderPrototype> FilmShader = "STAtmosphereFilm";
    private static readonly ProtoId<ShaderPrototype> CloudShadowShader = "STCloudShadow";

    [Test]
    public async Task ShadersOverlaysAndChechnyaDayCycleLoadCleanly()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true, Connected = true });
        var client = pair.Client;
        var server = pair.Server;

        await client.WaitAssertion(() =>
        {
            var proto = client.ResolveDependency<IPrototypeManager>();
            var overlay = client.ResolveDependency<IOverlayManager>();

            Assert.Multiple(() =>
            {
                Assert.That(proto.Index(FilmShader).InstanceUnique(), Is.Not.Null);
                Assert.That(proto.Index(CloudShadowShader).InstanceUnique(), Is.Not.Null);

                Assert.That(overlay.HasOverlay<AtmosphericFilmOverlay>(), Is.True);
                Assert.That(overlay.HasOverlay<CloudShadowOverlay>(), Is.True);
            });
        });

        await server.WaitAssertion(() =>
        {
            var loader = server.System<MapLoaderSystem>();
            var mapSys = server.System<SharedMapSystem>();

            Assert.That(loader.TryLoadMap(new ResPath("/Maps/ChechV6.yml"), out var mapEnt, out _), Is.True);
            var mapUid = mapEnt!.Value.Owner;
            var mapId = mapEnt.Value.Comp.MapId;

            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.TryGetComponent<MapLightComponent>(mapUid, out _), Is.True);
                Assert.That(server.EntMan.TryGetComponent<LightCycleComponent>(mapUid, out var cycle), Is.True);
                Assert.That(cycle!.Duration, Is.EqualTo(TimeSpan.FromSeconds(1800)));
                Assert.That(cycle.Offset, Is.EqualTo(TimeSpan.FromSeconds(450)));
                Assert.That(cycle.InitialOffset, Is.False);
                Assert.That(server.EntMan.HasComponent<SunShadowComponent>(mapUid), Is.True);
                Assert.That(server.EntMan.HasComponent<SunShadowCycleComponent>(mapUid), Is.True);
            });

            mapSys.DeleteMap(mapId);
        });

        await pair.CleanReturnAsync();
    }
}
