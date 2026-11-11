using System.Numerics;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._Stalker.Weapon.Projectile;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._Stalker;

[TestFixture]
public sealed class STSandbagPassThroughTest
{
    private const string TargetProtoId = "STTestSandbagTarget";
    private const string SandbagProtoId = "sandbag";
    private const string BulletProtoId = "Bullet918PGJ";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {TargetProtoId}
  components:
  - type: Transform
    anchored: true
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeAabb
          bounds: ""-0.45,-0.45,0.45,0.45""
        layer:
        - BulletImpassable
  - type: Damageable
    damageContainer: StructuralInorganic
";

    [Test]
    public async Task SandbagPassThroughDependsOnDistance()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var mapSys = server.System<SharedMapSystem>();
        var gunSys = server.System<GunSystem>();
        var tileDefMan = server.ResolveDependency<ITileDefinitionManager>();

        var map = await pair.CreateTestMap();
        var plating = new Tile(tileDefMan["Plating"].TileId);

        EntityUid sandbag = default;
        EntityUid orthoTarget = default;
        EntityUid diagTarget = default;
        EntityUid shooter = default;

        await server.WaitAssertion(() =>
        {
            for (var x = -22; x <= 4; x++)
            {
                for (var y = -2; y <= 2; y++)
                {
                    mapSys.SetTile(map.Grid.Owner, map.Grid.Comp, new Vector2i(x, y), plating);
                }
            }

            shooter = entMan.SpawnEntity(TargetProtoId, new EntityCoordinates(map.Grid.Owner, 0.5f, -5.5f));
            sandbag = entMan.SpawnEntity(SandbagProtoId, new EntityCoordinates(map.Grid.Owner, 0.5f, 0.5f));
            Assert.That(entMan.HasComponent<STProjectilePassThroughComponent>(sandbag),
                "sandbag prototype is missing STProjectilePassThroughComponent");

            orthoTarget = entMan.SpawnEntity(TargetProtoId, new EntityCoordinates(map.Grid.Owner, 2.5f, 0.5f));
            diagTarget = entMan.SpawnEntity(TargetProtoId, new EntityCoordinates(map.Grid.Owner, 1.5f, 1.5f));
        });

        // 1. Point-blank orthogonal (1.0 tile) and diagonal (~1.414 tiles) -> 100% pass-through.
        for (var i = 0; i < 10; i++)
        {
            await server.WaitPost(() =>
            {
                var orthoBullet = entMan.SpawnEntity(BulletProtoId, new EntityCoordinates(map.Grid.Owner, -0.5f, 0.5f));
                gunSys.ShootProjectile(orthoBullet, new Vector2(1f, 0f), Vector2.Zero, null, shooter, 20f);

                var diagBullet = entMan.SpawnEntity(BulletProtoId, new EntityCoordinates(map.Grid.Owner, -0.5f, -0.5f));
                gunSys.ShootProjectile(diagBullet, new Vector2(1f, 1f), Vector2.Zero, null, shooter, 20f);
            });
            await pair.RunTicksSync(1);
        }

        await pair.RunSeconds(0.5f);

        await server.WaitAssertion(() =>
        {
            var sandbagDamage = entMan.GetComponent<DamageableComponent>(sandbag).TotalDamage;
            var orthoDamage = entMan.GetComponent<DamageableComponent>(orthoTarget).TotalDamage;
            var diagDamage = entMan.GetComponent<DamageableComponent>(diagTarget).TotalDamage;

            Assert.That(sandbagDamage, Is.EqualTo(FixedPoint2.Zero), "Point-blank shots should pass through sandbag without hitting it.");
            Assert.That(orthoDamage, Is.GreaterThan(FixedPoint2.Zero), "Point-blank orthogonal shots should hit the target behind sandbag.");
            Assert.That(diagDamage, Is.GreaterThan(FixedPoint2.Zero), "Point-blank diagonal shots should hit the target behind sandbag.");
        });

        // 2. 20 tiles away -> 0% pass-through (all shots blocked by sandbag).
        FixedPoint2 orthoBeforeLongRange = FixedPoint2.Zero;
        await server.WaitPost(() =>
        {
            orthoBeforeLongRange = entMan.GetComponent<DamageableComponent>(orthoTarget).TotalDamage;
        });

        for (var i = 0; i < 8; i++)
        {
            await server.WaitPost(() =>
            {
                var bullet = entMan.SpawnEntity(BulletProtoId, new EntityCoordinates(map.Grid.Owner, -19.5f, 0.5f));
                gunSys.ShootProjectile(bullet, new Vector2(1f, 0f), Vector2.Zero, null, shooter, 20f);
            });
            await pair.RunTicksSync(1);
        }

        await pair.RunSeconds(1.5f);

        await server.WaitAssertion(() =>
        {
            var sandbagDamage = entMan.GetComponent<DamageableComponent>(sandbag).TotalDamage;
            var orthoAfterLongRange = entMan.GetComponent<DamageableComponent>(orthoTarget).TotalDamage;

            Assert.That(sandbagDamage, Is.GreaterThan(FixedPoint2.Zero), "Long-range shots should be blocked by sandbag.");
            Assert.That(orthoAfterLongRange, Is.EqualTo(orthoBeforeLongRange), "20-tile shots should not pass through sandbag.");
        });

        // 3. 10 tiles away -> ~50% pass-through (some hit sandbag, some pass through to target).
        FixedPoint2 sandbagBeforeMidRange = FixedPoint2.Zero;
        FixedPoint2 orthoBeforeMidRange = FixedPoint2.Zero;
        await server.WaitPost(() =>
        {
            sandbagBeforeMidRange = entMan.GetComponent<DamageableComponent>(sandbag).TotalDamage;
            orthoBeforeMidRange = entMan.GetComponent<DamageableComponent>(orthoTarget).TotalDamage;
        });

        for (var i = 0; i < 24; i++)
        {
            await server.WaitPost(() =>
            {
                var bullet = entMan.SpawnEntity(BulletProtoId, new EntityCoordinates(map.Grid.Owner, -9.5f, 0.5f));
                gunSys.ShootProjectile(bullet, new Vector2(1f, 0f), Vector2.Zero, null, shooter, 20f);
            });
            await pair.RunTicksSync(1);
        }

        await pair.RunSeconds(1.0f);

        await server.WaitAssertion(() =>
        {
            var sandbagDelta = entMan.GetComponent<DamageableComponent>(sandbag).TotalDamage - sandbagBeforeMidRange;
            var targetDelta = entMan.GetComponent<DamageableComponent>(orthoTarget).TotalDamage - orthoBeforeMidRange;

            Assert.That(sandbagDelta, Is.GreaterThan(FixedPoint2.Zero), "At 10 tiles, some shots should be blocked by sandbag.");
            Assert.That(targetDelta, Is.GreaterThan(FixedPoint2.Zero), "At 10 tiles, some shots should pass through sandbag.");
        });

        await pair.CleanReturnAsync();
    }
}
