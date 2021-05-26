using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
namespace ArchotechPlus
{
    public class HediffCompProperties_ArchotechConversion : HediffCompProperties
    {
        public HediffCompProperties_ArchotechConversion()
        {
            compClass = typeof(HediffComp_ArchotechConversion);
        }
    }
    public class HediffComp_ArchotechConversion : HediffComp
    {
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            foreach (var implant in DefDatabase<RecipeDef>.AllDefs.Where(x => x.addsHediff != null && x.addsHediff.defName.ToLower().Contains("archotech")))
            {
                if (implant.addsHediff != this.Def)
                {
                    if (implant.appliedOnFixedBodyParts != null)
                    {
                        foreach (var partDef in implant.appliedOnFixedBodyParts)
                        {
                            var parts = Pawn.RaceProps.body.GetPartsWithDef(partDef);
                            if (parts != null)
                            {
                                foreach (var part in parts)
                                {
                                    Pawn.health.RestorePart(part);
                                    Pawn.health.AddHediff(implant.addsHediff, part);
                                }
                            }
                        }
                    }
                    else
                    {
                        Pawn.health.AddHediff(implant.addsHediff);
                    }
                }
            }
        }
    }
}
