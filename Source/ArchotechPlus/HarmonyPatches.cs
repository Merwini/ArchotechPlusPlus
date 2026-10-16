using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ArchotechPlus;

[StaticConstructorOnStartup]
public static class HarmonyInit
{
	public static Harmony harmonyInstance;
	static HarmonyInit()
	{
		harmonyInstance = new Harmony("ArchotechPlus.Mod");
		harmonyInstance.PatchAll();
	}
}

// Need to recursively check for artifical child Parts
// When Hediff_MissingPart is added to parent part, it calls RestorePart on itself, which clears Hediffs from children before adding Hediff_Missing to them
// So their implants are not preserved
// For example, if the pawn has a bionic hand, and their arm is destroyed, the hand will not be remembered
// For simplicity, assume that any Parts we need to remember will be at the same recursive depth
[HarmonyPatch(typeof(HediffSet), "AddDirect")]
public static class HediffSet_Patch
{
	public static void Prefix(HediffSet __instance, Hediff hediff, DamageInfo? dinfo = null, DamageWorker.DamageResult damageResult = null)
	{
		if (hediff is not Hediff_MissingPart || hediff.Part == null)
			return;

		var hediffRegen = __instance.hediffs.OfType<Hediff_Regeneration>().FirstOrDefault();
		if (hediffRegen == null)
			return;

		var implants = __instance.hediffs.OfType<Hediff_Implant>().ToList();
		var partsToCheck = new List<BodyPartRecord> { hediff.Part };

		while (partsToCheck.Count > 0)
		{
			var matches = implants
				.Where(implant => partsToCheck.Contains(implant.Part))
				.ToList();

			if (matches.Count > 0)
			{
				foreach (var implant in matches)
				{
					hediffRegen.RememberImplant(implant, implant.Part);
				}

				return;
			}

			partsToCheck = partsToCheck
				.SelectMany(part => part.GetDirectChildParts())
				.ToList();
		}
	}
}

[HarmonyPatch(typeof(Need_Joy), "FallPerInterval", MethodType.Getter)]
public static class FallPerInterval_Patch
{
	public static void Postfix(Need_Joy __instance, Pawn ___pawn, ref float __result)
	{
		if (!ArchotechPlusSettings.CortexDisablesRecreation)
		{
			return;
		}
		var hediff = ___pawn.health?.hediffSet.GetFirstHediffOfDef(HediffDef.Named("ArchotechCortex"));
		if (hediff != null)
		{
			__result *= 0f;
		}
	}
}
