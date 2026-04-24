using System.Collections.Generic;
using Newtonsoft.Json;
using RimMind.Core;
using RimMind.Core.Client;
using RimMind.Core.Context;
using RimMind.Core.UI;
using RimMind.Personality.Data;
using RimWorld;
using Verse;

namespace RimMind.Personality
{
    public static class PersonalityThoughtMapper
    {
        public const string EvaluationSchema = SchemaRegistry.PersonalityOutput;

        public static float GetPersonalityBudget()
        {
            var ctx = RimMindCoreMod.Settings?.Context;
            if (ctx == null) return 0.6f;
            return ctx.ContextBudget;
        }

        private static readonly string[] SlotDefNames = new[]
        {
            "AIPersonality_Slot_0",
            "AIPersonality_Slot_1",
            "AIPersonality_Slot_2",
        };

        private const int TicksPerHour = 2500;

        public static void Apply(AIResponse response, Pawn pawn)
        {
            if (!response.Success)
            {
                Log.Warning($"[RimMind-Personality] Request failed ({pawn.Name.ToStringShort}): {response.Error}");
                return;
            }

            PersonalityResultDto? result;
            try
            {
                result = JsonConvert.DeserializeObject<PersonalityResultDto>(response.Content);
            }
            catch
            {
                result = null;
            }

            if (result == null)
            {
                Log.Warning($"[RimMind-Personality] Response parse failed ({pawn.Name.ToStringShort}):\n{response.Content}");
                return;
            }

            if (!result.narrative.NullOrEmpty())
            {
                var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
                if (profile != null)
                {
                    profile.aiNarrative             = result.narrative;
                    profile.lastNarrativeUpdateTick = Find.TickManager.TicksGame;
                }
            }

            if (result.identity != null)
            {
                var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
                if (profile != null)
                {
                    if (profile.agentIdentity == null)
                        profile.agentIdentity = new RimMind.Core.Agent.AgentIdentity();
                    if (result.identity.motivations != null)
                        profile.agentIdentity.Motivations = new List<string>(result.identity.motivations);
                    if (result.identity.traits != null)
                        profile.agentIdentity.PersonalityTraits = new List<string>(result.identity.traits);
                    if (result.identity.core_values != null)
                        profile.agentIdentity.CoreValues = new List<string>(result.identity.core_values);
                }
            }

            RemoveAllAIPersonalityThoughts(pawn);

            var settings = RimMindPersonalityMod.Settings;
            if (settings == null) return;
            bool showNotifications = settings.showNotifications;
            int slotIndex = 0;
            foreach (var entry in result.thoughts)
            {
                if (slotIndex >= SlotDefNames.Length) break;

                var thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(SlotDefNames[slotIndex]);
                if (thoughtDef == null)
                {
                    Log.Warning($"[RimMind-Personality] ThoughtDef '{SlotDefNames[slotIndex]}' not found.");
                    slotIndex++;
                    continue;
                }

                var thought = (Thought_AIPersonality)ThoughtMaker.MakeThought(thoughtDef);
                thought.aiLabel = entry.label;
                thought.aiDescription = entry.description;
                thought.aiIntensity = entry.intensity;
                thought.customDurationTicks = CalcDurationTicks(entry, settings);
                pawn.needs.mood.thoughts.memories.TryGainMemory(thought);

                if (RimMindPersonalityMod.Settings?.enableShapingVote == true)
                {
                    var capturedEntry = entry;

                    string optReinforce = "RimMind.Personality.Shaping.Reinforce".Translate();
                    string optSuppress  = "RimMind.Personality.Shaping.Suppress".Translate();
                    string optIgnore    = "RimMind.Personality.Shaping.Ignore".Translate();

                    RimMindAPI.RegisterPendingRequest(new RequestEntry
                    {
                        source = "personality",
                        pawn = pawn,
                        title = "RimMind.Personality.Shaping.NewTrait".Translate(),
                        description = $"{capturedEntry.label}: {capturedEntry.description}",
                        options = new[] { optReinforce, optSuppress, optIgnore },
                        expireTicks = settings?.requestExpireTicks ?? 30000,
                        callback = choice =>
                        {
                            string action = "ignored";
                            if (choice == optReinforce)
                                action = "reinforce";
                            else if (choice == optSuppress)
                                action = "suppress";

                            if (action != "ignored")
                            {
                                var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
                                if (profile != null)
                                {
                                    profile.AddShapingRecord(new ShapingRecord
                                    {
                                        label = capturedEntry.label,
                                        action = action,
                                        tick = Find.TickManager.TicksGame,
                                    }, settings?.shapingHistoryMaxCount ?? 20);
                                }
                            }
                        }
                    });
                }

                slotIndex++;
            }

            if (showNotifications && result.thoughts.Length > 0)
            {
                Messages.Message(
                    "RimMind.Personality.UI.PersonalityUpdated".Translate(pawn.Name.ToStringShort),
                    pawn,
                    MessageTypeDefOf.SilentInput,
                    historical: false);
            }
        }

        private static int CalcDurationTicks(ThoughtEntryDto entry, AIPersonalitySettings? settings)
        {
            if (settings == null) return TicksPerHour;
            if (settings.durationMode == ThoughtDurationMode.AIDecides
                && entry.duration_hours.HasValue
                && entry.duration_hours.Value > 0)
            {
                return System.Math.Clamp(entry.duration_hours.Value, 1, 24) * TicksPerHour;
            }
            return System.Math.Max(1, (int)(settings.thoughtDurationHours * TicksPerHour));
        }

        public static void RemoveAllAIPersonalityThoughts(Pawn pawn)
        {
            var memories = pawn.needs?.mood?.thoughts?.memories;
            if (memories == null) return;

            var toRemove = new System.Collections.Generic.List<Thought_Memory>();
            foreach (var t in memories.Memories)
            {
                if (IsAIPersonalityDef(t.def.defName))
                    toRemove.Add(t);
            }
            foreach (var t in toRemove)
                memories.RemoveMemory(t);
        }

        public static bool IsAIPersonalityDef(string defName)
        {
            foreach (var s in SlotDefNames)
                if (s == defName) return true;
            return false;
        }
    }
}
