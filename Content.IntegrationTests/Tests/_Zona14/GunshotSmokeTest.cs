using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._Zona14.Weapon.Effects;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Zona14;

public sealed class GunshotSmokeTest : InteractionTest
{
    private static readonly EntProtoId[] ConfiguredWeapons =
    [
        "STWeaponRifleAKM",
        "STWeaponRifleAKMC",
        "STWeaponRifleAKS74",
        "STWeaponRifleAKS74Green",
        "STWeaponRifleAK74M",
        "STWeaponRifleAKS74U",
        "STWeaponMachineGunPKM",
        "STWeaponSniperSVDS",
        "STWeaponSniperSVDSGrass",
        "STWeaponSniperSVDSCovenant",
        "STWeaponRifleSVDSWood",
        "WeaponPistolStalkerPM",
        "STWeaponSMGMP5",
        "STWeaponLauncherRocket",
        "STWeaponLauncherRG6",
        "STWeaponLauncherRGM40",
        "STModuleLauncherGP25",
    ];

    [Test]
    public async Task ConfiguredWeaponsSpawnAndAnimateGunshotSmoke()
    {
        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var weaponId in ConfiguredWeapons)
                {
                    Assert.That(ProtoMan.TryIndex(weaponId, out var weaponProto), Is.True,
                        $"Missing weapon prototype {weaponId}");
                    Assert.That(weaponProto!.TryGetComponent<STGunshotSmokeComponent>(out var smokeComp, Factory), Is.True,
                        $"Weapon {weaponId} is missing STGunshotSmokeComponent");
                    Assert.That(ProtoMan.HasIndex(smokeComp!.EffectProto), Is.True,
                        $"Weapon {weaponId} references missing smoke effect {smokeComp.EffectProto}");
                    if (smokeComp.SecondaryEffectProto is { } secProto)
                    {
                        Assert.That(ProtoMan.HasIndex(secProto), Is.True,
                            $"Weapon {weaponId} references missing secondary smoke effect {secProto}");
                    }
                }
            });
        });

        var gunUids = new List<NetEntity>();
        await Server.WaitPost(() =>
        {
            EntProtoId[] sampleIds =
            [
                "STWeaponRifleAKM",
                "STWeaponRifleAKS74",
                "STWeaponMachineGunPKM",
                "STWeaponSniperSVDS",
            ];

            var spawnCoords = SEntMan.GetCoordinates(PlayerCoords);
            foreach (var id in sampleIds)
            {
                var ent = SEntMan.SpawnEntity(id, spawnCoords);
                gunUids.Add(SEntMan.GetNetEntity(ent));
            }
        });

        await Pair.RunTicksSync(5);

        await Client.WaitAssertion(() =>
        {
            foreach (var netGun in gunUids)
            {
                var cGun = CEntMan.GetEntity(netGun);
                Assert.That(CEntMan.EntityExists(cGun), Is.True, $"Client did not receive gun {netGun} in PVS");

                var ev = new STGunshotMuzzleEffectEvent(Angle.Zero, null);
                CEntMan.EventBus.RaiseLocalEvent(cGun, ref ev);
            }

            var spawnedEffects = CEntMan.EntityQuery<STGunshotSmokeEffectComponent, SpriteComponent, AnimationPlayerComponent>().ToList();
            // Each of the 4 sample weapons spawns primary + secondary smoke effects = 8 entities.
            Assert.That(spawnedEffects.Count, Is.EqualTo(8));
        });

        // Advance simulation past the longest smoke animation lifetime (~0.72s).
        await Pair.RunSeconds(1.0f);

        await Client.WaitAssertion(() =>
        {
            var remainingEffects = CEntMan.EntityQuery<STGunshotSmokeEffectComponent>().Count();
            Assert.That(remainingEffects, Is.EqualTo(0), "Gunshot smoke effects should despawn after animation completes.");
        });
    }
}
