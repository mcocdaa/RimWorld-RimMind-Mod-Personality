using HarmonyLib;
using RimMind.Personality.Comps;
using RimWorld;
using Verse;

namespace RimMind.Personality.Patches
{
    [HarmonyPatch(typeof(IncidentWorker), "TryExecuteWorker")]
    static class Patch_PersonalityIncident
    {
        static void Postfix(IncidentWorker __instance, IncidentParms parms, bool __result)
        {
            if (!__result) return;
            if (!RimMindPersonalityMod.Settings.enableIncidentTrigger) return;
            if (parms.target is not Map map) return;

            var def = __instance.def;
            if (def == null || def.category == null) return;
            var catName = def.category.defName;
            if (catName != "ThreatBig" && catName != "ThreatSmall") return;

            foreach (var pawn in map.mapPawns.FreeColonists)
            {
                var comp = pawn.GetComp<CompAIPersonality>();
                if (comp != null)
                    comp.TriggerEvent($"{"RimMind.Storyteller.Context.IncidentOccurred".Translate(def.LabelCap)}", TriggerEventType.Incident);
            }
        }
    }
}
