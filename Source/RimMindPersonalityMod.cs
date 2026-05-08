using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts.Context;
using RimMind.Contracts.Extension;
using RimMind.Core;
using RimMind.Kernel.Context;
using RimMind.Kernel.Prompt;
using HarmonyLib;
using RimMind.Adapters.UI;
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
            RimMindAPI.Extensions<ISettingsTab>().Register(new PersonalitySettingsTab(this));
            RimMindAPI.Extensions<IToggleBehavior>().Register(new PersonalityToggleBehavior());
            RimMindAPI.Extensions<IModCooldown>().Register(new PersonalityModCooldown());
            RimMindAPI.Extensions<ISkipCheck>().Register(new PersonalityActionSkipCheck());
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
            ContextKeyRegistry.Register("personality_profile", ContextLayer.L3_State, 0.25f,
                pawnObj =>
                {
                    if (ContextKeyRegistry.CurrentScenario != ScenarioIds.Personality) return new List<ContextEntry>();
                    var pawn = pawnObj as Pawn;
                    if (pawn == null) return new List<ContextEntry>();
                    var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
                    if (profile == null || profile.IsEmpty) return new List<ContextEntry>();

                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("RimMind.Personality.Context.ProfileHeader".Translate(pawn.Name?.ToStringShort ?? ""));
                    if (!profile.description.NullOrEmpty())
                        sb.AppendLine(profile.description);
                    if (!profile.workTendencies.NullOrEmpty())
                        sb.AppendLine("RimMind.Personality.Context.WorkTendencies".Translate(profile.workTendencies));
                    if (!profile.socialTendencies.NullOrEmpty())
                        sb.AppendLine("RimMind.Personality.Context.SocialTendencies".Translate(profile.socialTendencies));
                    if (!profile.aiNarrative.NullOrEmpty())
                        sb.AppendLine("RimMind.Personality.Context.RecentState".Translate(profile.aiNarrative));
                    return new List<ContextEntry> { new ContextEntry(sb.ToString().TrimEnd()) };
                }, "RimMind.Personality");

            ContextKeyRegistry.Register("personality_state", ContextLayer.L3_State, 0.2f,
                pawnObj =>
                {
                    if (ContextKeyRegistry.CurrentScenario != ScenarioIds.Personality) return new List<ContextEntry>();
                    var pawn = pawnObj as Pawn;
                    if (pawn == null) return new List<ContextEntry>();
                    var memories = pawn.needs?.mood?.thoughts?.memories?.Memories;
                    if (memories == null) return new List<ContextEntry>();

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
                    return any ? new List<ContextEntry> { new ContextEntry(sb.ToString().TrimEnd()) } : new List<ContextEntry>();
                }, "RimMind.Personality");

            ContextKeyRegistry.Register("personality_shaping", ContextLayer.L3_State, 0.15f,
                pawnObj =>
                {
                    if (ContextKeyRegistry.CurrentScenario != ScenarioIds.Personality) return new List<ContextEntry>();
                    var pawn = pawnObj as Pawn;
                    if (pawn == null) return new List<ContextEntry>();
                    var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
                    if (profile?.playerShapingHistory == null || profile.playerShapingHistory.Count == 0)
                        return new List<ContextEntry>();

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
                    return new List<ContextEntry> { new ContextEntry(sb.ToString().TrimEnd()) };
                }, "RimMind.Personality");

            var personalityTaskInstruction = TaskInstructionBuilder.Build("RimMind.Personality.Prompt.TaskInstruction",
                "Role", "Goal", "Process", "Constraint", "Example", "Output", "Fallback",
                "EvalInstruction", "JsonFormatDirect", "LabelHint", "DescHint",
                "NarrativeHint", "DurationHint", "DiversityHint", "TriggerReason");

            ContextKeyRegistry.Register("personality_task", ContextLayer.L0_Static, 0.95f,
                pawnObj =>
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

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Personality.Settings.Section.Timing".Translate());

            listing.Label("RimMind.Personality.Settings.DailyInterval".Translate($"{Settings.dailyIntervalTicks / 2500f:F1}"));
            GUI.color = UnityEngine.Color.gray;
            listing.Label("  " + "RimMind.Personality.Settings.DailyInterval.Desc".Translate());
            GUI.color = UnityEngine.Color.white;
            Settings.dailyIntervalTicks = (int)listing.Slider(Settings.dailyIntervalTicks, 12000f, 120000f);
            Settings.dailyIntervalTicks = (Settings.dailyIntervalTicks / 3000) * 3000;

            listing.Label("RimMind.Personality.Settings.JitterRange".Translate($"{Settings.jitterRangeTicks / 2500f:F1}"));
            GUI.color = UnityEngine.Color.gray;
            listing.Label("  " + "RimMind.Personality.Settings.JitterRange.Desc".Translate());
            GUI.color = UnityEngine.Color.white;
            Settings.jitterRangeTicks = (int)listing.Slider(Settings.jitterRangeTicks, 0f, 12000f);
            Settings.jitterRangeTicks = (Settings.jitterRangeTicks / 600) * 600;

            listing.Label("RimMind.Personality.Settings.EventCooldown".Translate($"{Settings.eventCooldownTicks / 2500f:F1}"));
            GUI.color = UnityEngine.Color.gray;
            listing.Label("  " + "RimMind.Personality.Settings.EventCooldown.Desc".Translate());
            GUI.color = UnityEngine.Color.white;
            Settings.eventCooldownTicks = (int)listing.Slider(Settings.eventCooldownTicks, 600f, 6000f);
            Settings.eventCooldownTicks = (Settings.eventCooldownTicks / 300) * 300;

            listing.Label("RimMind.Personality.Settings.RequestTimeout".Translate($"{Settings.requestTimeoutTicks / 2500f:F1}"));
            GUI.color = UnityEngine.Color.gray;
            listing.Label("  " + "RimMind.Personality.Settings.RequestTimeout.Desc".Translate());
            GUI.color = UnityEngine.Color.white;
            Settings.requestTimeoutTicks = (int)listing.Slider(Settings.requestTimeoutTicks, 12000f, 120000f);
            Settings.requestTimeoutTicks = (Settings.requestTimeoutTicks / 3000) * 3000;

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
                Settings.dailyIntervalTicks = 60000;
                Settings.jitterRangeTicks = 3000;
                Settings.eventCooldownTicks = 1200;
                Settings.requestTimeoutTicks = 60000;
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
            h += 24f + (24f + 24f + 32f) * 4;
            return h + 40f;
        }
    }
}
