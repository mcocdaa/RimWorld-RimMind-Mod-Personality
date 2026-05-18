using HarmonyLib;
using RimMind.Personality.Comps;
using RimWorld;
using Verse;

namespace RimMind.Personality.Patches
{
    [HarmonyPatch(typeof(HediffSet), "AddDirect")]
    static class Patch_PersonalityInjury
    {
        static void Postfix(HediffSet __instance, Hediff hediff)
        {
            if (!RimMindPersonalityMod.Settings.enableInjuryTrigger) return;
            if (hediff == null) return;
            if (hediff.def.isBad == false) return;
            if (hediff.Severity < 0.2f) return;
            var pawn = __instance.pawn;
            if (pawn == null || !pawn.IsFreeNonSlaveColonist) return;

            var comp = pawn.GetComp<CompAIPersonality>();
            if (comp == null) return;
            comp.TriggerEvent($"{"RimMind.Memory.Trigger.Contracted".Translate(hediff.LabelCap, "RimMind.Memory.Trigger.FullBody".Translate())}", TriggerEventType.Injury);
        }
    }
}
