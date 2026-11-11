using System.Collections.Generic;
using System.Linq;
using Content.Server._Stalker.Weapon.GrenadeLauncher;
using Content.Server.Hands.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Stalker.Weapon.GrenadeLauncher;
using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Stalker;

[TestFixture]
public sealed class GrenadierLoadoutTests
{
    private static readonly ProtoId<JobPrototype>[] GrenadierJobs =
    [
        "ChechnyaRFGrenadier",
        "ChechnyaCRIGrenadier",
    ];

    [Test]
    public async Task GrenadiersSpawnWithRegularAks74AndAttachedGp25()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            Dirty = true,
        });

        var server = pair.Server;
        var client = pair.Client;
        var testMap = await pair.CreateTestMap();

        var spawning = server.System<StationSpawningSystem>();
        var inventory = server.System<InventorySystem>();
        var hands = server.System<HandsSystem>();
        var actions = server.System<SharedActionsSystem>();
        var grenadeLauncher = server.System<STGrenadeLauncherSystem>();
        var spriteSys = client.System<SpriteSystem>();

        var spawnedWeapons = new List<(ProtoId<JobPrototype> Job, EntityUid Weapon)>();

        await server.WaitAssertion(() =>
        {
            Assert.That(
                server.ProtoMan.EnumeratePrototypes<EntityPrototype>().Any(p => p.ID == "STWeaponRifleAKS74GP25"),
                Is.False,
                "STWeaponRifleAKS74GP25 prototype should be removed.");

            foreach (var jobId in GrenadierJobs)
            {
                var mob = spawning.SpawnPlayerMob(testMap.GridCoords, jobId, null, null);

                Assert.That(
                    inventory.TryGetSlotEntity(mob, "suitstorage", out var weapon),
                    Is.True,
                    $"{jobId} did not spawn with a rifle in suitstorage.");

                Assert.That(
                    server.MetaData(weapon!.Value).EntityPrototype?.ID,
                    Is.EqualTo("STWeaponRifleAKS74"),
                    $"{jobId} should spawn with STWeaponRifleAKS74.");

                Assert.That(
                    grenadeLauncher.TryGetAttachedLauncher(weapon.Value, out var launcher, out var launcherComp),
                    Is.True,
                    $"{jobId} rifle did not have an attached grenade launcher.");

                Assert.That(
                    server.MetaData(launcher).EntityPrototype?.ID,
                    Is.EqualTo("STModuleLauncherGP25"),
                    $"{jobId} rifle should have STModuleLauncherGP25 attached.");

                Assert.That(
                    inventory.TryUnequip(mob, "suitstorage", silent: true, force: true),
                    Is.True,
                    $"{jobId} could not unequip their rifle from suitstorage.");

                Assert.That(
                    hands.TryPickupAnyHand(mob, weapon.Value, checkActionBlocker: false),
                    Is.True,
                    $"{jobId} could not pick up their rifle.");

                Assert.That(launcherComp.FireActionEntity, Is.Not.Null);
                Assert.That(
                    actions.GetActions(mob).Any(a => a.Owner == launcherComp.FireActionEntity!.Value),
                    Is.True,
                    $"{jobId} did not receive the GP-25 fire action when holding the rifle.");

                spawnedWeapons.Add((jobId, weapon.Value));
            }
        });

        await pair.RunTicksSync(5);

        await client.WaitAssertion(() =>
        {
            foreach (var (jobId, sWeapon) in spawnedWeapons)
            {
                var cWeapon = pair.ToClientUid(sWeapon);
                Assert.That(client.EntMan.TryGetComponent<SpriteComponent>(cWeapon, out var sprite), Is.True);
                Assert.That(spriteSys.LayerMapTryGet((cWeapon, sprite!), "launcher", out var layerIdx, false), Is.True);
                Assert.That(spriteSys.TryGetLayer((cWeapon, sprite), layerIdx, out var layer, false), Is.True);
                Assert.That(layer!.Visible, Is.True, $"Client launcher visual layer should be visible for {jobId}.");
            }
        });

        await pair.CleanReturnAsync();
    }
}
