using System.Collections.Generic;
using Content.Server.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Content.Shared.Inventory;
using Content.Shared.Tag;

namespace Content.Server._Stalker.PersonalDamage;

public sealed class PersonalDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;

        var entries = new List<(PersonalDamageComponent Comp, List<EntityUid> Targets)>();
        var dueKeys = new HashSet<(EntityUid Victim, float Interval)>();

        var query = EntityQueryEnumerator<PersonalDamageComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!IsArtifactAllowed(uid))
                continue;

            var targets = new List<EntityUid>();
            var parent = uid;
            while (!HasComp<MapComponent>(parent))
            {
                if (TerminatingOrDeleted(parent))
                    break;

                if (HasComp<PersonalDamageBlockComponent>(parent))
                    break;

                targets.Add(parent);
                parent = Transform(parent).ParentUid;
            }

            if (targets.Count == 0)
                continue;

            entries.Add((component, targets));

            if (component.NextDamage <= now)
            {
                foreach (var target in targets)
                    dueKeys.Add((target, component.Interval));
            }
        }

        if (dueKeys.Count == 0)
            return;

        var pending = new Dictionary<(EntityUid Target, bool IgnoreResistances, bool InterruptsDoAfters), DamageSpecifier>();
        var stamina = new Dictionary<EntityUid, float>();

        foreach (var (component, targets) in entries)
        {
            var fires = false;
            foreach (var target in targets)
            {
                if (!dueKeys.Contains((target, component.Interval)))
                    continue;
                fires = true;
                break;
            }

            if (!fires)
                continue;

            foreach (var target in targets)
            {
                var key = (target, component.IgnoreResistances, component.InterruptsDoAfters);
                pending[key] = pending.TryGetValue(key, out var acc)
                    ? acc + component.Damage
                    : new DamageSpecifier(component.Damage);

                stamina[target] = stamina.GetValueOrDefault(target) + component.StaminaDamage;
            }

            component.NextDamage = now + TimeSpan.FromSeconds(component.Interval <= 0 ? 1f : component.Interval);
        }

        foreach (var ((target, ignoreResistances, interruptsDoAfters), damage) in pending)
            _damageableSystem.TryChangeDamage(target, damage, ignoreResistances, interruptsDoAfters);

        foreach (var (target, amount) in stamina)
            _stamina.TakeStaminaDamage(target, amount);
    }

    private bool IsArtifactAllowed(EntityUid uid)
    {

        if (!TryComp<TagComponent>(uid, out var tagComp) || !_tag.HasTag(tagComp, "STArtifact"))
            return true;


        if (!TryComp<TransformComponent>(uid, out var xform) || !TryComp<MetaDataComponent>(uid, out var meta))
            return false;

        if (!_inventory.TryGetContainingSlot((uid, xform, meta), out var slotDef) || slotDef == null)
            return false;

        var name = slotDef.Name;
        return name == "artifact1" || name == "artifact2" || name == "artifact3" || name == "artifact4";
    }
}
