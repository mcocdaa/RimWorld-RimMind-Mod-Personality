using System.Collections.Generic;
using HarmonyLib;
using RimMind.Personality.Comps;
using RimWorld;
using Verse;

namespace RimMind.Personality.Patches
{
    [HarmonyPatch(typeof(Pawn), "Kill")]
    static class Patch_PersonalityDeath
    {
        static void Postfix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit)
        {
            if (!RimMindPersonalityMod.Settings.enableDeathTrigger) return;
            if (__instance.Map == null) return;

            var killedPawn = __instance;
            foreach (var pawn in __instance.Map.mapPawns.FreeColonists)
            {
                if (pawn == killedPawn) continue;
                var rel = pawn.relations?.DirectRelations;
                if (rel == null) continue;
                foreach (var dr in rel)
                {
                    if (dr.otherPawn == killedPawn)
                    {
                        var comp = pawn.GetComp<CompAIPersonality>();
                        if (comp != null)
                            comp.TriggerEvent($"{"RimMind.Memory.Trigger.RelationDeath".Translate(dr.def.LabelCap, killedPawn.Name.ToStringShort)}", TriggerEventType.Death);
                        break;
                    }
                }
            }
        }
    }
}
