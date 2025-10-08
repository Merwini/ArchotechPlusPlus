using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace ArchotechPlus
{
    [StaticConstructorOnStartup]
    public static class CortexRestControl
    {
        static CortexRestControl()
        {
            CheckIfRestNeedShouldBeDisabled();
        }

        internal static void CheckIfRestNeedShouldBeDisabled()
        {
            foreach (HediffStage stage in ArchoTechPlusDefOf.ArchotechCortex.stages)
            {
                if (!ArchotechPlusSettings.CortexDisablesRestNeed && stage.disablesNeeds.Contains(NeedDefOf.Rest))
                {
                    DisableRestNeed(stage);
                }
                else if (ArchotechPlusSettings.CortexDisablesRestNeed && !stage.disablesNeeds.Contains(NeedDefOf.Rest))
                {
                    EnableRestNeed(stage);
                }
            }
        }

        private static void EnableRestNeed(HediffStage stage)
        {
            stage.disablesNeeds.Add(NeedDefOf.Rest);
        }

        private static void DisableRestNeed(HediffStage stage)
        {
            stage.disablesNeeds.Remove(NeedDefOf.Rest);
        }
    }
}
