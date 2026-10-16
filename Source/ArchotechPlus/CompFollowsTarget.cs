using RimWorld;
using Verse;

namespace ArchotechPlus;

public class CompFollowsTarget : ThingComp
{
    public Thing Target => parent.GetComp<CompResurrector>()?.corpse;
    private CompFleckEmitter compFleckEmitter;
    public CompProperties_FollowsTarget Props => (CompProperties_FollowsTarget) props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        if (compFleckEmitter == null)
        {
            compFleckEmitter = parent.GetComp<CompFleckEmitter>();
        }

        base.PostSpawnSetup(respawningAfterLoad);
    }

    public override void CompTick()
    {
        // if parent was destroyed in the same tick by the other comp
        if (parent.DestroyedOrNull())
            return;

        var target = Target;
        if (target.DestroyedOrNull())
        {
            parent.Destroy();
            return;
        }

        var targetMap = target.MapHeld;
        var targetPosition = target.PositionHeld;

        // target corpse is not on a map. trick CompFleckEmitter into not emitting ticks, so parent becomes 'invisible'
        if (targetMap == null || !targetPosition.InBounds(targetMap))
        {
            compFleckEmitter.ticksSinceLastEmitted = 0;
            return;
        }

        // if target is on a different map, despawn and respawn on that map
        if (parent.Map != targetMap)
        {
            if (parent.Spawned)
            {
                parent.DeSpawn();
            }

            GenSpawn.Spawn(parent, targetPosition, targetMap, parent.Rotation, respawningAfterLoad: true);

            return;
        }

        if (parent.Position != targetPosition)
        {
            parent.Position = targetPosition;
        }
    }
}
