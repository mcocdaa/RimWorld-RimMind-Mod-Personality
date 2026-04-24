using LudeonTK;
using RimMind.Core;
using RimMind.Core.Context;
using RimMind.Personality.Comps;
using RimMind.Personality.Data;
using RimWorld;
using System.Text;
using Verse;

namespace RimMind.Personality.Debug
{
    [StaticConstructorOnStartup]
    public static class PersonalityDebugActions
    {
        [DebugAction("RimMind Personality", "Force Evaluate Selected Pawn",
            actionType = DebugActionType.Action)]
        public static void ForceEvaluateSelected()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null)
            {
                Log.Warning("[RimMind-Personality] Please select a pawn on the map first, then open the Dev menu.");
                return;
            }

            if (pawn.GetComp<CompAIPersonality>() == null)
            {
                Log.Warning($"[RimMind-Personality] {pawn.Name.ToStringShort} has no CompAIPersonality (non-humanlike?).");
                return;
            }

            if (!RimMindAPI.IsConfigured())
            {
                Log.Warning("[RimMind-Personality] API not configured. Please enter API Key in Mod settings first.");
                return;
            }

            Log.Message($"[RimMind-Personality] Sending evaluation request for {pawn.Name.ToStringShort}...");

            var ctxRequest = new ContextRequest
            {
                NpcId       = $"NPC-{pawn.ThingID}",
                Scenario    = ScenarioIds.Personality,
                Budget      = PersonalityThoughtMapper.GetPersonalityBudget(),
                CurrentQuery = "[Debug] Force evaluate",
                ExcludeKeys = new[] { "personality_state" },
                MaxTokens   = 300,
                Temperature = 0.8f,
            };

            var schema = PersonalityThoughtMapper.EvaluationSchema;

            RimMindAPI.RequestStructured(ctxRequest, schema, response =>
            {
                if (!response.Success)
                {
                    Log.Warning($"[RimMind-Personality] Evaluation failed ({pawn.Name.ToStringShort}): {response.Error}");
                    return;
                }

                Log.Message($"[RimMind-Personality] Received response ({pawn.Name.ToStringShort}):\n{response.Content}");
                PersonalityThoughtMapper.Apply(response, pawn);
            });
        }

        [DebugAction("RimMind Personality", "Show Personality State (selected)",
            actionType = DebugActionType.Action)]
        public static void ShowPersonalityState()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null)
            {
                Log.Warning("[RimMind-Personality] Please select a pawn first.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"=== {pawn.Name.ToStringShort} Personality State ===");

            sb.AppendLine($"[Diag] API configured: {RimMindAPI.IsConfigured()}");
            var comp = pawn.GetComp<CompAIPersonality>();
            sb.AppendLine($"[Diag] CompAIPersonality: {(comp != null ? "exists" : "missing")}");

            var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
            if (profile != null && !profile.IsEmpty)
            {
                sb.AppendLine("[Personality Profile]");
                if (!profile.description.NullOrEmpty())      sb.AppendLine($"  Description: {profile.description}");
                if (!profile.workTendencies.NullOrEmpty())   sb.AppendLine($"  Work tendencies: {profile.workTendencies}");
                if (!profile.socialTendencies.NullOrEmpty()) sb.AppendLine($"  Social tendencies: {profile.socialTendencies}");
                if (!profile.aiNarrative.NullOrEmpty())      sb.AppendLine($"  Recent narrative: {profile.aiNarrative}");
                float daysSince = (Find.TickManager.TicksGame - profile.lastNarrativeUpdateTick) / 60000f;
                sb.AppendLine($"  Last updated: {daysSince:F1} game days ago");
            }
            else
            {
                sb.AppendLine("[Personality Profile] (empty, not yet evaluated)");
            }

            sb.AppendLine("\n[Current Personality Thoughts]");
            var memories = pawn.needs?.mood?.thoughts?.memories?.Memories;
            bool any = false;
            if (memories != null)
            {
                foreach (var t in memories)
                {
                    if (!PersonalityThoughtMapper.IsAIPersonalityDef(t.def.defName)) continue;

                    var pt = t as Thought_AIPersonality;
                    float mood  = MoodOffsetCalculator.CalcMoodOffset(pt?.aiIntensity ?? 0);
                    float hours = t.DurationTicks / 2500f;
                    sb.AppendLine($"  [{t.def.defName}] {pt?.aiLabel ?? t.def.label} " +
                                  $"(intensity={pt?.aiIntensity ?? 0}, mood={mood:+0;-0}, {hours:F1}h remaining)");
                    if (!pt?.aiDescription.NullOrEmpty() ?? false)
                        sb.AppendLine($"    -> {pt!.aiDescription}");
                    any = true;
                }
            }
            if (!any) sb.AppendLine("  (No active personality thoughts)");

            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind Personality", "Clear Personality Thoughts (selected)",
            actionType = DebugActionType.Action)]
        public static void ClearPersonalityThoughts()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null)
            {
                Log.Warning("[RimMind-Personality] Please select a pawn first.");
                return;
            }

            var memories = pawn.needs?.mood?.thoughts?.memories;
            if (memories == null) return;

            var toRemove = new System.Collections.Generic.List<Thought_Memory>();
            foreach (var t in memories.Memories)
                if (PersonalityThoughtMapper.IsAIPersonalityDef(t.def.defName))
                    toRemove.Add(t);
            foreach (var t in toRemove) memories.RemoveMemory(t);

            Log.Message($"[RimMind-Personality] Cleared {toRemove.Count} personality thoughts for {pawn.Name.ToStringShort}.");
        }

        [DebugAction("RimMind Personality", "List Personality-Enabled Pawns",
            actionType = DebugActionType.Action)]
        public static void ListEnabledPawns()
        {
            var map = Find.CurrentMap;
            if (map == null) { Log.Warning("[RimMind-Personality] No active map."); return; }

            var sb = new StringBuilder("=== Pawns with personality system enabled ===\n");
            int count = 0;
            foreach (var p in map.mapPawns.FreeColonists)
            {
                if (p.GetComp<CompAIPersonality>() == null) continue;

                var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(p);
                int thoughtCount = 0;
                if (p.needs?.mood?.thoughts?.memories?.Memories != null)
                    foreach (var t in p.needs.mood.thoughts.memories.Memories)
                        if (PersonalityThoughtMapper.IsAIPersonalityDef(t.def.defName))
                            thoughtCount++;

                sb.AppendLine($"  {p.Name.ToStringShort}: profile={(profile != null && !profile.IsEmpty)}, activeThoughts={thoughtCount}");
                count++;
            }
            sb.AppendLine($"API configured: {RimMindAPI.IsConfigured()}  Total: {count} pawns");
            Log.Message(sb.ToString());
        }

        [DebugAction("RimMind Personality", "Reset Personality Profile (selected)",
            actionType = DebugActionType.Action)]
        public static void ResetPersonalityProfile()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null)
            {
                Log.Warning("[RimMind-Personality] Please select a pawn first.");
                return;
            }

            AIPersonalityWorldComponent.Instance?.Remove(pawn);
            Log.Message($"[RimMind-Personality] Reset personality profile for {pawn.Name.ToStringShort}.");
        }
    }
}
