using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimMind.Personality.Comps;
using RimWorld;
using Verse;

namespace RimMind.Personality.Patches
{
    [HarmonyPatch(typeof(SkillRecord), "Learn")]
    static class Patch_PersonalitySkill
    {
        internal static readonly Dictionary<object, int> PreLevels = new Dictionary<object, int>();

        static void Prefix(SkillRecord __instance)
        {
            if (!RimMindPersonalityMod.Settings.enableSkillTrigger) return;
            PreLevels[__instance] = __instance.levelInt;
        }

        static void Postfix(SkillRecord __instance)
        {
            if (!RimMindPersonalityMod.Settings.enableSkillTrigger) return;
            if (!PreLevels.TryGetValue(__instance, out int preLevel)) return;

            try
            {
                if (__instance.levelInt <= preLevel) return;

                foreach (var map in Find.Maps)
                {
                    foreach (var pawn in map.mapPawns.FreeColonists)
                    {
                        var skill = pawn.skills?.GetSkill(__instance.def);
                        if (skill == null) continue;
                        if (skill == __instance)
                        {
                            var comp = pawn.GetComp<CompAIPersonality>();
                            if (comp != null)
                                comp.TriggerEvent($"{"RimMind.Memory.Trigger.SkillUp".Translate(__instance.def.LabelCap, preLevel, __instance.levelInt)}", TriggerEventType.Skill);
                            return;
                        }
                    }
                }
            }
            finally
            {
                PreLevels.Remove(__instance);
            }
        }
    }
}
