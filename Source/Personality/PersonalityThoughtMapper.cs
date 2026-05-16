using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Client;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Interfaces.UI;
using RimMind.Application.Common.Models.UI;
using RimMind.Presentation;
using RimMind.Presentation.Settings;
using RimMind.Infrastructure.Services.Clients;
using RimMind.Application.Features.Json;
using RimMind.Application.Features.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Infrastructure.UI;
using RimMind.Presentation.Context;
using RimMind.Personality.Data;
using RimWorld;
using Verse;

namespace RimMind.Personality
{
    public static class PersonalityThoughtMapper
    {
        public static readonly string EvaluationSchema = SchemaRegistry.PersonalityOutput;
        public const string DefaultExcludeKey = "personality_state";
        public const int DefaultMaxTokens = 600;
        public const float DefaultTemperature = 0.8f;

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

        public static void Apply(Result<AIResponse, RimMindError> result, Pawn pawn)
        {
            if (result.IsErr)
            {
                RimMindErrors.Warn($"[RimMind-Personality] Request failed ({pawn.Name.ToStringShort}): {result.Error}");
                return;
            }

            var response = result.Value;

            PersonalityResultDto? dto;
            try
            {
                string content = response.Content ?? "";
                dto = JsonConvert.DeserializeObject<PersonalityResultDto>(content);
            }
            catch
            {
                dto = null;
            }

            if (dto == null)
            {
                string? trimmed = JsonRepairHelper.TryRepairTruncatedJson(response.Content ?? "");
                if (trimmed != null)
                {
                    try
                    {
                        dto = JsonConvert.DeserializeObject<PersonalityResultDto>(trimmed);
                    }
                    catch { }
                }
            }

            if (dto == null)
            {
                RimMindErrors.Warn($"[RimMind-Personality] Response parse failed ({pawn.Name.ToStringShort}):\n{response.Content}");
                return;
            }

            var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);

            if (!dto.narrative.NullOrEmpty() && profile != null)
            {
                profile.aiNarrative = dto.narrative;
                profile.lastNarrativeUpdateTick = Find.TickManager.TicksGame;
            }

            if (dto.identity != null && profile != null)
            {
                if (profile.agentIdentity == null)
                    profile.agentIdentity = new RimMind.Presentation.Agent.AgentIdentity();
                if (dto.identity.motivations != null)
                    profile.agentIdentity.Motivations = new List<string>(dto.identity.motivations);
                if (dto.identity.traits != null)
                    profile.agentIdentity.PersonalityTraits = new List<string>(dto.identity.traits);
                if (dto.identity.core_values != null)
                    profile.agentIdentity.CoreValues = new List<string>(dto.identity.core_values);
            }

            RemoveAllAIPersonalityThoughts(pawn);

            var settings = RimMindPersonalityMod.Settings;
            if (settings == null) return;
            bool showNotifications = settings.showNotifications;
            int slotIndex = 0;
            dto.thoughts ??= Array.Empty<ThoughtEntryDto>();
            foreach (var entry in dto.thoughts)
            {
                if (slotIndex >= SlotDefNames.Length) break;

                var thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(SlotDefNames[slotIndex]);
                if (thoughtDef == null)
                {
                    RimMindErrors.Warn($"[RimMind-Personality] ThoughtDef '{SlotDefNames[slotIndex]}' not found.");
                    slotIndex++;
                    continue;
                }

                var thought = (Thought_AIPersonality)ThoughtMaker.MakeThought(thoughtDef);
                thought.aiLabel = entry.label;
                thought.aiDescription = entry.description;
                thought.aiIntensity = (int)entry.intensity;
                thought.customDurationTicks = CalcDurationTicks(entry, settings);
                pawn.needs.mood.thoughts.memories.TryGainMemory(thought);

                if (RimMindPersonalityMod.Settings?.enableShapingVote == true)
                {
                    var capturedEntry = entry;

                    string optReinforce = "RimMind.Personality.Shaping.Reinforce".Translate();
                    string optSuppress = "RimMind.Personality.Shaping.Suppress".Translate();
                    string optIgnore = "RimMind.Personality.Shaping.Ignore".Translate();

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

            if (showNotifications && dto.thoughts.Length > 0)
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
                return (int)(Math.Clamp(entry.duration_hours.Value, 1f, 24f) * TicksPerHour);
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
