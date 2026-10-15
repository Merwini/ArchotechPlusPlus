using RimWorld;
using UnityEngine;
using Verse;

namespace ArchotechPlus;

public class CompResurrector : ThingComp
{
    private const int TicksPerHour = 2500;
    
    private int _ticksToResurrection;
    public Corpse corpse;

    public CompProperties_Resurrector Props => (CompProperties_Resurrector) props;

    public bool CorpseIsInContainer => corpse.StoringThing() != null;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        if (!respawningAfterLoad)
        {
            _ticksToResurrection = ArchotechPlusSettings.ResurrectionRange.RandomInRange * TicksPerHour;
        }

        if (parent.GetType() == typeof(Corpse))
        {
            corpse = (Corpse)parent;
        }

        base.PostSpawnSetup(respawningAfterLoad);
    }

    public override void CompTick()
    {
        _ticksToResurrection--;
        if (_ticksToResurrection > 0)
        {
            return;
        }

        if (ResurrectionConditionsMet() && CorpseRemovedFromContainers())
        {
            MessageWasResurrectionSuccessful(true);
            ResurrectionUtility.TryResurrectWithSideEffects(corpse.InnerPawn);
        }
        parent.Destroy();
    }

    private bool CorpseRemovedFromContainers()
    {
        if (CorpseIsInContainer && corpse.StoringThing().GetType().IsSubclassOf(typeof(Building_Casket)))
        {
            var container = (Building_Casket) corpse.StoringThing();
            container.EjectContents();
        }
        if (!CorpseIsInContainer)
        {
            return true;
        }

        Debug.Log("Error: Corpse is in miscellaneous that could not be processed container.");
        return false;
    }
    
    private bool ResurrectionConditionsMet()
    {
        if (!corpse.DestroyedOrNull() && corpse.InnerPawn.health.hediffSet.HasHead)
        {
            return true;
        }

        MessageWasResurrectionSuccessful(false);
        return false;
    }

    private void MessageWasResurrectionSuccessful(bool successful)
    {
        if (successful)
        {
            Messages.Message(
                "ArchotechPlusSuccessfulResurrection".Translate((NamedArgument)parent.LabelCap,
                    (NamedArgument)corpse.InnerPawn.LabelShort,
                    corpse.InnerPawn.Named("PAWN")), corpse, MessageTypeDefOf.PositiveEvent);
        }
        else
        {
            Messages.Message(
                "ArchotechPlusFailedResurrection".Translate((NamedArgument)parent.LabelCap,
                    (NamedArgument)corpse.InnerPawn.LabelShort,
                    corpse.InnerPawn.Named("PAWN")), corpse, MessageTypeDefOf.PositiveEvent);
        }
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref _ticksToResurrection, "ticksToResurrection", 600);
        Scribe_References.Look(ref corpse, "targetPawn");
        base.PostExposeData();
    }
}
