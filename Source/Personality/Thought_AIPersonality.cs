using System;
using RimWorld;
using Verse;

namespace RimMind.Personality
{
    /// <summary>
    /// AI 生成的人格 Thought。
    /// 重写 MoodOffset() / LabelCap / Description，由 AI 响应动态决定内容。
    /// </summary>
    public class Thought_AIPersonality : Thought_Memory
    {
        public string aiLabel = string.Empty;
        public string aiDescription = string.Empty;
        public int aiIntensity;   // 原始值 -3~+3，超出范围由 MoodOffsetCalculator.CalcMoodOffset clamp

        /// <summary>
        /// 自定义持续时长（ticks）。-1 表示使用 def 默认值。
        /// 通过覆盖 DurationTicks 属性来控制 Thought 消逝时机。
        /// </summary>
        public int customDurationTicks = -1;

        public override int DurationTicks
            => customDurationTicks > 0 ? customDurationTicks : base.DurationTicks;

        public override string LabelCap
        {
            get
            {
                if (aiLabel.NullOrEmpty()) return base.LabelCap;
                bool prefix = RimMindPersonalityMod.Settings?.showLabelPrefix ?? true;
                return prefix ? $"[RimMind] {aiLabel.CapitalizeFirst()}" : aiLabel.CapitalizeFirst();
            }
        }

        public override string Description
            => aiDescription.NullOrEmpty() ? base.Description : aiDescription;

        /// <summary>
        /// 覆盖 XML 中的 baseMoodEffect，由 AI intensity 动态决定心情偏移量。
        /// </summary>
        public override float MoodOffset()
            => MoodOffsetCalculator.CalcMoodOffset(aiIntensity);

        public override void ExposeData()
        {
            base.ExposeData();
#pragma warning disable CS8601
            Scribe_Values.Look(ref aiLabel, "aiLabel", string.Empty);
            Scribe_Values.Look(ref aiDescription, "aiDesc", string.Empty);
#pragma warning restore CS8601
            Scribe_Values.Look(ref aiIntensity, "aiIntensity", 0);
            Scribe_Values.Look(ref customDurationTicks, "customDurationTicks", -1);
        }
    }
}
