using System.Collections.Generic;
using System.Linq;
using RimMind.Core;
using RimMind.Core.Context;
using RimMind.Core.Prompt;
using HarmonyLib;
using RimMind.Core.UI;
using RimMind.Personality.Data;
using UnityEngine;
using Verse;

namespace RimMind.Personality
{
    public class RimMindPersonalityMod : Mod
    {
        public static AIPersonalitySettings Settings = null!;
        private static UnityEngine.Vector2 _scrollPos = UnityEngine.Vector2.zero;

        public RimMindPersonalityMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<AIPersonalitySettings>();
            new Harmony("mcocdaa.RimMindPersonality").PatchAll();

            RegisterContextProviders();
            RegisterAgentIdentityProvider();
            RimMindAPI.RegisterSettingsTab("personality", () => "RimMind.Personality.Settings.TabLabel".Translate(), DrawSettingsContent);
            RimMindAPI.RegisterModCooldown("Personality", () => 36000);
            Log.Message("[RimMind-Personality] Initialized.");
        }

        private static void RegisterAgentIdentityProvider()
        {
            RimMindAPI.RegisterAgentIdentityProvider(pawn =>
            {
                var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
                return profile?.agentIdentity;
            });
        }

        private static void RegisterContextProviders()
        {
            RimMindAPI.RegisterPawnContextProvider("personality_profile", pawn =>
            {
                var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
                if (profile == null || profile.IsEmpty) return null;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("RimMind.Personality.Context.ProfileHeader".Translate(pawn.Name.ToStringShort));
                if (!profile.description.NullOrEmpty())
                    sb.AppendLine(profile.description);
                if (!profile.workTendencies.NullOrEmpty())
                    sb.AppendLine("RimMind.Personality.Context.WorkTendencies".Translate(profile.workTendencies));
                if (!profile.socialTendencies.NullOrEmpty())
                    sb.AppendLine("RimMind.Personality.Context.SocialTendencies".Translate(profile.socialTendencies));
                if (!profile.aiNarrative.NullOrEmpty())
                    sb.AppendLine("RimMind.Personality.Context.RecentState".Translate(profile.aiNarrative));
                return sb.ToString().TrimEnd();
            });

            RimMindAPI.RegisterPawnContextProvider("personality_state", pawn =>
            {
                var memories = pawn.needs?.mood?.thoughts?.memories?.Memories;
                if (memories == null) return null;

                var sb = new System.Text.StringBuilder("RimMind.Personality.Context.StateHeader".Translate() + "\n");
                bool any = false;
                foreach (var t in memories)
                {
                    if (!Personality.PersonalityThoughtMapper.IsAIPersonalityDef(t.def.defName)) continue;

                    string desc = (t as Thought_AIPersonality)?.aiDescription ?? t.def.label;
                    float hours = t.DurationTicks / 2500f;
                    sb.AppendLine("RimMind.Personality.Context.StateEntry".Translate(desc, $"{hours:F1}"));
                    any = true;
                }
                return any ? sb.ToString().TrimEnd() : null;
            });

            RimMindAPI.RegisterPawnContextProvider("personality_shaping", pawn =>
            {
                var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
                if (profile?.playerShapingHistory == null || profile.playerShapingHistory.Count == 0)
                    return null;

                int maxCount = Settings?.shapingHistoryMaxCount ?? 20;
                var recent = profile.playerShapingHistory.Skip(System.Math.Max(0, profile.playerShapingHistory.Count - maxCount)).ToList();
                var sb = new System.Text.StringBuilder("RimMind.Personality.Context.ShapingHistoryHeader".Translate());
                foreach (var r in recent)
                {
                    string actionLabel = r.action switch
                    {
                        "reinforce" => "RimMind.Personality.ShapingAction.Reinforce".Translate(),
                        "suppress" => "RimMind.Personality.ShapingAction.Suppress".Translate(),
                        _ => "RimMind.Personality.ShapingAction.Ignore".Translate()
                    };
                    sb.AppendLine($"- {r.label}: {actionLabel}");
                }
                return sb.ToString().TrimEnd();
            });

            var personalityTaskInstruction = TaskInstructionBuilder.Build("RimMind.Personality.Prompt.TaskInstruction",
                "Role", "Goal", "Process", "Constraint", "Example", "Output", "Fallback",
                "EvalInstruction", "JsonFormatDirect", "LabelHint", "DescHint",
                "NarrativeHint", "DurationHint", "DiversityHint", "TriggerReason");

            ContextKeyRegistry.Register("personality_task", ContextLayer.L0_Static, 0.95f,
                pawn =>
                {
                    if (ContextKeyRegistry.CurrentScenario != ScenarioIds.Personality) return new List<ContextEntry>();
                    return new List<ContextEntry> { new ContextEntry(personalityTaskInstruction) };
                }, "RimMind.Personality");
        }

        public override string SettingsCategory() => "RimMind - Personality";

        public override void DoSettingsWindowContents(UnityEngine.Rect rect)
        {
            DrawSettingsContent(rect);
        }

        internal static void DrawSettingsContent(UnityEngine.Rect inRect)
        {
            UnityEngine.Rect contentArea = SettingsUIHelper.SplitContentArea(inRect);
            UnityEngine.Rect bottomBar = SettingsUIHelper.SplitBottomBar(inRect);

            float contentH = EstimateHeight();
            UnityEngine.Rect viewRect = new UnityEngine.Rect(0f, 0f, contentArea.width - 16f, contentH);
            Widgets.BeginScrollView(contentArea, ref _scrollPos, viewRect);

            var listing = new Verse.Listing_Standard();
            listing.Begin(viewRect);

            listing.CheckboxLabeled("RimMind.Personality.Settings.EnablePersonality".Translate(), ref Settings.enablePersonality,
                "RimMind.Personality.Settings.EnablePersonality.Desc".Translate());

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Personality.Settings.TriggerSources".Translate());
            listing.CheckboxLabeled("RimMind.Personality.Settings.DailyEval".Translate(), ref Settings.enableDailyEval,
                "RimMind.Personality.Settings.DailyEval.Desc".Translate());
            listing.CheckboxLabeled("RimMind.Personality.Settings.InjuryTrigger".Translate(), ref Settings.enableInjuryTrigger,
                "RimMind.Personality.Settings.InjuryTrigger.Desc".Translate());
            listing.CheckboxLabeled("RimMind.Personality.Settings.SkillTrigger".Translate(), ref Settings.enableSkillTrigger,
                "RimMind.Personality.Settings.SkillTrigger.Desc".Translate());
            listing.CheckboxLabeled("RimMind.Personality.Settings.IncidentTrigger".Translate(), ref Settings.enableIncidentTrigger,
                "RimMind.Personality.Settings.IncidentTrigger.Desc".Translate());
            listing.CheckboxLabeled("RimMind.Personality.Settings.DeathTrigger".Translate(), ref Settings.enableDeathTrigger,
                "RimMind.Personality.Settings.DeathTrigger.Desc".Translate());

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Personality.Settings.Section.Thought".Translate());

            listing.Label("RimMind.Personality.Settings.ThoughtDuration".Translate());
            GUI.color = UnityEngine.Color.gray;
            listing.Label("  " + "RimMind.Personality.Settings.ThoughtDuration.Desc".Translate());
            GUI.color = UnityEngine.Color.white;
            bool aiDecides = Settings.durationMode == ThoughtDurationMode.AIDecides;
            listing.CheckboxLabeled("RimMind.Personality.Settings.AIDecidesDuration".Translate(), ref aiDecides,
                "RimMind.Personality.Settings.AIDecidesDuration.Desc".Translate());
            Settings.durationMode = aiDecides ? ThoughtDurationMode.AIDecides : ThoughtDurationMode.Fixed;

            if (!aiDecides)
            {
                listing.Label("RimMind.Personality.Settings.FixedDuration".Translate($"{Settings.thoughtDurationHours:F0}"));
                GUI.color = UnityEngine.Color.gray;
                listing.Label("  " + "RimMind.Personality.Settings.FixedDuration.Desc".Translate());
                GUI.color = UnityEngine.Color.white;
                Settings.thoughtDurationHours = listing.Slider(Settings.thoughtDurationHours, 1f, 24f);
            }
            else
            {
                listing.Label("RimMind.Personality.Settings.AIDurationHint".Translate());
            }

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Personality.Settings.Section.Display".Translate());
            listing.CheckboxLabeled("RimMind.Personality.Settings.ShowNotifications".Translate(), ref Settings.showNotifications,
                "RimMind.Personality.Settings.ShowNotifications.Desc".Translate());
            listing.CheckboxLabeled("RimMind.Personality.Settings.ShowLabelPrefix".Translate(), ref Settings.showLabelPrefix,
                "RimMind.Personality.Settings.ShowLabelPrefix.Desc".Translate());

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Personality.Settings.Section.Request".Translate());
            listing.CheckboxLabeled("RimMind.Personality.Settings.EnableShapingVote".Translate(), ref Settings.enableShapingVote,
                "RimMind.Personality.Settings.EnableShapingVote.Desc".Translate());
            listing.Label("RimMind.Personality.Settings.RequestExpire".Translate($"{Settings.requestExpireTicks / 60000f:F2}"));
            GUI.color = UnityEngine.Color.gray;
            listing.Label("  " + "RimMind.Personality.Settings.RequestExpire.Desc".Translate());
            GUI.color = UnityEngine.Color.white;
            Settings.requestExpireTicks = (int)listing.Slider(Settings.requestExpireTicks, 3600f, 120000f);
            Settings.requestExpireTicks = (Settings.requestExpireTicks / 1500) * 1500;
            listing.Label("RimMind.Personality.Settings.ShapingHistoryMax".Translate($"{Settings.shapingHistoryMaxCount}"));
            GUI.color = UnityEngine.Color.gray;
            listing.Label("  " + "RimMind.Personality.Settings.ShapingHistoryMax.Desc".Translate());
            GUI.color = UnityEngine.Color.white;
            Settings.shapingHistoryMaxCount = (int)listing.Slider(Settings.shapingHistoryMaxCount, 10f, 200f);

            listing.End();
            Widgets.EndScrollView();

            SettingsUIHelper.DrawBottomBar(bottomBar, () =>
            {
                Settings.enablePersonality = true;
                Settings.showNotifications = true;
                Settings.showLabelPrefix = true;
                Settings.enableDailyEval = true;
                Settings.enableInjuryTrigger = true;
                Settings.enableSkillTrigger = true;
                Settings.enableIncidentTrigger = true;
                Settings.enableDeathTrigger = true;
                Settings.thoughtDurationHours = 24f;
                Settings.durationMode = ThoughtDurationMode.AIDecides;
                Settings.requestExpireTicks = 30000;
                Settings.enableShapingVote = true;
                Settings.shapingHistoryMaxCount = 20;
            });

            Settings.Write();
        }

        private static float EstimateHeight()
        {
            float h = 30f;
            h += 24f;
            h += 24f + 24f * 5;
            h += 24f + 24f + 32f;
            h += 24f + 24f;
            if (Settings.durationMode != ThoughtDurationMode.AIDecides)
                h += 24f + 32f;
            else
                h += 24f;
            h += 24f + 24f * 2;
            h += 24f + 24f + 32f;
            return h + 40f;
        }
    }
}
