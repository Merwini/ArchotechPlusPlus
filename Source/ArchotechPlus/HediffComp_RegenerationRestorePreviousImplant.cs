using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace ArchotechPlus;

public class HediffCompProperties_RegenerationRestorePreviousImplant : HediffCompProperties
{
	public IntRange disappearsAfterTicks;
	public HediffCompProperties_RegenerationRestorePreviousImplant()
	{
		compClass = typeof (HediffComp_RegenerationRestorePreviousImplant);
	}
}

public class HediffComp_RegenerationRestorePreviousImplant : HediffComp
{
	private int _ticksToDisappear;
	private int _ticksRemaining;

	public HediffCompProperties_RegenerationRestorePreviousImplant Props => (HediffCompProperties_RegenerationRestorePreviousImplant)props;
	public override bool CompShouldRemove => base.CompShouldRemove || _ticksRemaining <= 0;

	public override void CompPostMake()
	{
		base.CompPostMake();
		_ticksToDisappear = Props.disappearsAfterTicks.RandomInRange;
		_ticksRemaining = _ticksToDisappear;
	}

	public override void CompPostTick(ref float severityAdjustment)
	{
		_ticksRemaining--;
		parent.Severity = (float)_ticksRemaining / _ticksToDisappear;
	}

	public override void CompPostPostRemoved()
	{
		var hediff = Pawn.health.hediffSet.hediffs.FirstOrDefault(x => x is Hediff_Regeneration);
		if (hediff is Hediff_Regeneration hediffRegen
			&& hediffRegen.previousImplants != null
			&& hediffRegen.previousImplants.TryGetValue(this.parent.Part, out var implant))
			{
				Pawn.health.AddHediff(implant, this.parent.Part);
			}
		
		base.CompPostPostRemoved();
	}

	public override void CompPostMerged(Hediff other)
	{
		base.CompPostMerged(other);
		HediffComp_RegenerationRestorePreviousImplant incoming = other.TryGetComp<HediffComp_RegenerationRestorePreviousImplant>();
		if (incoming != null && incoming._ticksRemaining > _ticksRemaining)
		{
			_ticksRemaining = incoming._ticksRemaining;
			_ticksToDisappear = incoming._ticksToDisappear;
		}
	}

	public override void CompExposeData()
	{
		Scribe_Values.Look(ref _ticksRemaining, "ticksRemaining", 0);
		Scribe_Values.Look(ref _ticksToDisappear, "ticksToDisappear", 0);
	}

	public override string CompDebugString()
	{
		return "_ticksRemaining: " + _ticksRemaining;
	}
}
