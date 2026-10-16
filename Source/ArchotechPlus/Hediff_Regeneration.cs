using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace ArchotechPlus;

public class Hediff_Regeneration : Hediff_Level
{
    private const int AgeMultiplier = 10;
    private static long TargetAgeInTicks => ((long)ArchotechPlusSettings.TargetAge * GenDate.TicksPerYear) + 1800000L;

    private int _ticks;
    private int _ticksFullCharge;
    private int _healingCharges;
    private int _resurrectionCharges;
    internal Dictionary<BodyPartRecord, HediffDef> previousImplants;
    
    private BodyPartRecord _bodyPartRegenerationTarget;
    private Hediff _woundRegenerationTarget;
    private Hediff _illnessHealingTarget;
    

    private float PercentageCharged => (float)_ticks / _ticksFullCharge;
    private bool ResurrectorCanCharge => this.Severity >= 3 && ArchotechPlusSettings.RegeneratorResurrects && _resurrectionCharges < ArchotechPlusSettings.MaxResurrectionCharges;
    private bool HealerCanCharge => this.Severity >= 2 && _healingCharges < ArchotechPlusSettings.MaxHealingCharges;

    public override void PostMake()
    {
        ResetChargingTicks();

        base.PostMake();
    }

    public override void PostTick()
    {
        _ticks++;
        if (_ticks >= _ticksFullCharge)
        {
            ChargeRegenerator();
            ResetChargingTicks();
        }
        if (Find.TickManager.TicksGame % GenDate.TicksPerHour == 0)
        {
            HourlyTick();
        }

        base.PostTick();
    }

    private void HourlyTick()
    {
        if (IsPawnInjured() && TryUseHealingCharge())
        {
            if (TryHealMissingPart() || TryHealRandomPermanentWound() || TryHealRandomDisease())
            {
                IsPawnInjured();
            }
        }

        if(ArchotechPlusSettings.RegeneratorDeAge)
        {
            ReduceAge();
        }
    }

    private void ResetChargingTicks()
    {
        _ticks = 0;
        _ticksFullCharge = ArchotechPlusSettings.HealingRange.RandomInRange * GenDate.TicksPerHour;
    }

    private void ChargeRegenerator()
    {
        if (ResurrectorCanCharge)
        {
            _resurrectionCharges++;
        }
        else if (HealerCanCharge) // will charge if resurrector isn't unlocked or its charges are full
        {
            _healingCharges++;
        }
    }
    
    private bool IsPawnInjured()
    {
        _bodyPartRegenerationTarget = FindBiggestMissingBodyPart();
        _woundRegenerationTarget = FindRandomPermanentWound();
        _illnessHealingTarget = FindRandomDiseaseOrInjury();
        return _bodyPartRegenerationTarget != null || _woundRegenerationTarget != null || _illnessHealingTarget != null;
    }

    private bool TryUseHealingCharge()
    {
        if (_healingCharges <= 0)
        {
            return false;
        }

        _healingCharges--;
        return true;
    }

    private BodyPartRecord FindBiggestMissingBodyPart(float minCoverage = 0.0f)
    {
        BodyPartRecord bodyPartRecord = null;
        foreach (var partsCommonAncestor in this.pawn.health.hediffSet.GetMissingPartsCommonAncestors().Where(
            partsCommonAncestor =>
                (double) partsCommonAncestor.Part.coverageAbsWithChildren >= (double) minCoverage &&
                !this.pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(partsCommonAncestor.Part) &&
                (bodyPartRecord == null || (double) partsCommonAncestor.Part.coverageAbsWithChildren >
                    (double) bodyPartRecord.coverageAbsWithChildren)))
        {
            bodyPartRecord = partsCommonAncestor.Part;
        }
        return bodyPartRecord;
    }

    private Hediff FindRandomPermanentWound()
    {
        return this.pawn.health.hediffSet.hediffs.Where(hd => hd.def == HediffDefOf.ResurrectionPsychosis || hd.IsPermanent() || hd.def.chronic)
            .TryRandomElement(out var result) ? result : null;
    }   

    private Hediff FindRandomDiseaseOrInjury()
    {
        return this.pawn.health.hediffSet.hediffs.Where(hd => hd.def.tendable && hd.def != HediffDefOf.MissingBodyPart).TryRandomElement(out var result) ? result : null;
    } 
    
    public void RememberImplant(Hediff_Implant hediff_Implant, BodyPartRecord part)
    {
        previousImplants ??= new Dictionary<BodyPartRecord, HediffDef>();
        
        previousImplants[part] = hediff_Implant.def;
    }

    private bool TryHealMissingPart()
    {
        if (_bodyPartRegenerationTarget == null)
        {
            return false;
        }

        this.pawn.health.RestorePart(_bodyPartRegenerationTarget);
        this.pawn.health.AddHediff(ArchoTechPlusDefOf.RegenerationProgress, _bodyPartRegenerationTarget);
        if (PawnUtility.ShouldSendNotificationAbout(this.pawn))
        {
            Messages.Message("ArchotechPlusPartRegenerated".Translate((NamedArgument)_bodyPartRegenerationTarget.Label, this.pawn.Named("PAWN")), this.pawn, MessageTypeDefOf.PositiveEvent);
        }

        return true;
    }

    private bool TryHealRandomPermanentWound()
    {
        if (_woundRegenerationTarget == null)
        {
            return false;
        }

        pawn.health.RemoveHediff(_woundRegenerationTarget);

        if (PawnUtility.ShouldSendNotificationAbout(this.pawn))
        {
            Messages.Message("ArchotechPlusMessagePermanentWoundHealed".Translate((NamedArgument)_woundRegenerationTarget.Label, this.pawn.Named("PAWN")), this.pawn, MessageTypeDefOf.PositiveEvent);
        }

        return true;
    }

    private bool TryHealRandomDisease()
    {
        if (_illnessHealingTarget == null)
        {
            return false;
        }
        this.pawn.health.RemoveHediff(_illnessHealingTarget);
        if (PawnUtility.ShouldSendNotificationAbout(this.pawn))
        {
            Messages.Message("ArchotechPlusMessageDiseaseHealed".Translate((NamedArgument)_illnessHealingTarget.Label, this.pawn.Named("PAWN")), this.pawn, MessageTypeDefOf.PositiveEvent);
        }

        return true;
    }

    private void ReduceAge()
    {
        if (this.pawn.ageTracker.AgeBiologicalTicks < TargetAgeInTicks)
        {
            return;
        }

        this.pawn.ageTracker.AgeBiologicalTicks -= GenDate.TicksPerHour * AgeMultiplier;
    }

    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
    {
        if (!ArchotechPlusSettings.RegeneratorResurrects)
        {
            base.Notify_PawnDied(dinfo, culprit);
            return;
        }

        bool shouldMessage = PawnUtility.ShouldSendNotificationAbout(this.pawn);

        if (_resurrectionCharges <= 0)
        {
            if (shouldMessage)
            {
                Messages.Message("ArchotechPlusNoResurrectorCharges".Translate(this.pawn.Named("PAWN")), this.pawn, MessageTypeDefOf.NegativeEvent);
            }

            base.Notify_PawnDied(dinfo, culprit);
            return;
        }

        if (pawn.ParentHolder == null || pawn.MapHeld == null)
        {
            if (shouldMessage)
            {
                Messages.Message("ArchotechPlusResurrectorInvalid".Translate(this.pawn.Named("PAWN")), MessageTypeDefOf.NegativeEvent);
            }
            
            base.Notify_PawnDied(dinfo, culprit);
            return;
        }

        if (CreateResurrector())
        {
            SpendResurrectorCharge();
        }
        else
        {
            if (shouldMessage)
            {
                Messages.Message("ArchotechPlusResurrectorFailSpawn".Translate(this.pawn.Named("PAWN")), MessageTypeDefOf.NegativeEvent);
            }
        }

        base.Notify_PawnDied(dinfo, culprit);
    }

    private bool CreateResurrector()
    {
        var pos = pawn?.Corpse?.PositionHeld;
        var map = pawn?.Corpse?.MapHeld;

        if (map == null || pos == null || pos is not IntVec3 p || !p.InBounds(map))
            return false;

        var resurrectionTracker = (ThingWithComps) GenSpawn.Spawn(ThingDef.Named("ResurrectorTracker"), p, map);
        resurrectionTracker.GetComp<CompResurrector>().corpse = this.pawn.Corpse;

        return resurrectionTracker.Spawned;
    }

    private void SpendResurrectorCharge()
    {
        _resurrectionCharges--;
        _ticks = 0;
        if (!PawnUtility.ShouldSendNotificationAbout(this.pawn))
        {
            return;
        }

        Messages.Message("ArchotechPlusResurrectionChargeSpent".Translate(this.pawn.Named("PAWN")), this.pawn, MessageTypeDefOf.PositiveEvent);
    }

    public override string DebugString()
    {
        return base.DebugString() + "\nTicks: " + _ticks
                                + "\nTicksToFullCharge" + _ticksFullCharge
                                + "\nTargetAge: " + ArchotechPlusSettings.TargetAge;
    }

    private string CompTipStringBuilder()
    {
        var tipBuilder = new StringBuilder();
        if (this.Severity >= 3 && ArchotechPlusSettings.RegeneratorResurrects)
        {
            tipBuilder.AppendLine("ArchotechPlusResurrectionChargesCount".Translate(_resurrectionCharges));
        }

        if (this.Severity >= 2)
        {
            tipBuilder.AppendLine("ArchotechPlusHealingChargesCount".Translate(_healingCharges));

            string nextHealingTarget;
            if (_bodyPartRegenerationTarget != null)
            {
                nextHealingTarget = _bodyPartRegenerationTarget.LabelCap;
            }
            else if (_woundRegenerationTarget != null)
            {
                string wound = _woundRegenerationTarget.LabelCap;
                string bodyPart = _woundRegenerationTarget.Part != null ? _woundRegenerationTarget.Part.LabelCap : "ArchotechPlusWholeBody".Translate();
                nextHealingTarget = "ArchotechPlusWoundAndPart".Translate(wound, bodyPart);
            }
            else if (_illnessHealingTarget != null)
            {
                nextHealingTarget = _illnessHealingTarget.LabelCap;
            }
            else
            {
                nextHealingTarget = "None".Translate();
            }

            tipBuilder.AppendLine("ArchotechPlusNextHealingTarget".Translate(nextHealingTarget));
            tipBuilder.AppendLine();

            if (ResurrectorCanCharge || HealerCanCharge)
            {
                tipBuilder.Append("ArchotechPlusCharged".Translate(PercentageCharged.ToStringPercent()));
            }
        }
        
        return tipBuilder.ToString();
    }

    public override string TipStringExtra => this.Severity > 1 ? CompTipStringBuilder() : base.TipStringExtra;

    // public override IEnumerable<Gizmo> GetGizmos()
    // {
    //     foreach (var g in base.GetGizmos())
    //     {
    //         yield return g;
    //     }

    //     if (DebugSettings.godMode)
    //     {
    //         yield return new Command_Action
    //         {
    //             defaultLabel = "res charge next tick",
    //             action = () => _ticks = _ticksFullCharge - 1
    //         };
    //     }
    // }

    private List<BodyPartRecord> bodyPartsWorkingList;
    private List<HediffDef> defsWorkingList;
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _ticks, "ticksToHeal");
        Scribe_Values.Look(ref _ticksFullCharge, "ticksFullCharge");
        Scribe_Values.Look(ref _healingCharges, "healingCharges");
        Scribe_Values.Look(ref _resurrectionCharges, "resurrectionCharges");
        Scribe_Collections.Look(ref previousImplants, "previousImplants", LookMode.BodyPart, LookMode.Def, ref bodyPartsWorkingList, ref defsWorkingList);
    }
}
