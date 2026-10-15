using RimWorld;
using UnityEngine;
using Verse;

namespace ArchotechPlus;

public class CompResurrector : ThingComp
{
    private const int TicksPerHour = 2500;
    
    private int _ticksToResurrection;
    public Corpse corpse;
    private Pawn Pawn => corpse.InnerPawn;

    public CompProperties_Resurrector Props => (CompProperties_Resurrector) props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        if (!respawningAfterLoad)
        {
            _ticksToResurrection = ArchotechPlusSettings.ResurrectionRange.RandomInRange * TicksPerHour;
        }

        if (respawningAfterLoad && corpse == null)
        {
            Log.Error($"CompResurrector corpse is missing. Removing.");
            parent.Destroy();
            return;
        }

        if (respawningAfterLoad && corpse.InnerPawn == null)
        {
            Log.Error($"CompResurrector parent corpse has no pawn associated");
            parent.Destroy();
            return;
        }

        base.PostSpawnSetup(respawningAfterLoad);
    }

    public override void CompTick()
    {
        _ticksToResurrection--;

        if (_ticksToResurrection > 0)
            return;

        if (ResurrectionConditionsMet() && CorpseRemovedFromContainers())
        {
            ResurrectionUtility.TryResurrectWithSideEffects(Pawn);
            if (Pawn.Spawned && !Pawn.Dead)
            {
                MessageWasResurrectionSuccessful(true);
                parent.Destroy();
            }
            else
            {
                MessageWasResurrectionSuccessful(false);
                parent.Destroy();
                return;
            }
        }
    }

    private bool CorpseRemovedFromContainers()
    {
        if (corpse.ParentHolder is Building_Casket casket)
        {
            casket.EjectContents();
        }

        if (corpse.Spawned)
            return true;

        return false;
    }
    
    private bool ResurrectionConditionsMet()
    {
        if (!corpse.DestroyedOrNull() && corpse.InnerPawn.health.hediffSet.HasHead)
            return true;

        MessageWasResurrectionSuccessful(false);
        parent.Destroy();

        return false;
    }

    private void MessageWasResurrectionSuccessful(bool successful)
    {
        if (successful)
        {
            Messages.Message(
                "ArchotechPlusSuccessfulResurrection".Translate(Pawn.Named("PAWN")), new LookTargets(Pawn), MessageTypeDefOf.PositiveEvent);
        }
        else
        {
            Messages.Message(
                "ArchotechPlusFailedResurrection".Translate((NamedArgument)parent.LabelCap,
                    (NamedArgument)corpse.InnerPawn.LabelShort,
                    Pawn.Named("PAWN")), MessageTypeDefOf.NegativeEvent);
        }
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref _ticksToResurrection, "ticksToResurrection", 600);
        Scribe_References.Look(ref corpse, "targetPawn");

        base.PostExposeData();
    }
}
