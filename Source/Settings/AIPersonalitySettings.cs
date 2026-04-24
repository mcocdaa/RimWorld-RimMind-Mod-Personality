using Verse;

namespace RimMind.Personality
{
    public enum ThoughtDurationMode { Fixed, AIDecides }

    public class AIPersonalitySettings : ModSettings
    {
        public bool  enablePersonality    = true;
        public bool  showNotifications    = true;

        // 触发来源开关
        public bool  enableDailyEval      = true;
        public bool  enableInjuryTrigger  = true;
        public bool  enableSkillTrigger   = true;
        public bool  enableIncidentTrigger = true;
        public bool  enableDeathTrigger   = true;

        public float thoughtDurationHours = 24f;

        /// <summary>Fixed = 使用 thoughtDurationHours；AIDecides = 由 AI 在 JSON 中决定 duration_hours。</summary>
        public ThoughtDurationMode durationMode = ThoughtDurationMode.AIDecides;

        /// <summary>在心情面板的 Thought 标签前显示 [RimMind] 前缀。</summary>
        public bool showLabelPrefix = true;

        public bool enableShapingVote = true;

        public int requestExpireTicks = 30000;

        public int shapingHistoryMaxCount = 20;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enablePersonality,     "enablePersonality",     true);
            Scribe_Values.Look(ref showNotifications,     "showNotifications",     true);
            Scribe_Values.Look(ref enableDailyEval,       "enableDailyEval",       true);
            Scribe_Values.Look(ref enableInjuryTrigger,   "enableInjuryTrigger",   true);
            Scribe_Values.Look(ref enableSkillTrigger,    "enableSkillTrigger",    true);
            Scribe_Values.Look(ref enableIncidentTrigger, "enableIncidentTrigger", true);
            Scribe_Values.Look(ref enableDeathTrigger,    "enableDeathTrigger",    true);
            Scribe_Values.Look(ref thoughtDurationHours,  "thoughtDurationHours",  24f);
            Scribe_Values.Look(ref durationMode,          "durationMode",          ThoughtDurationMode.AIDecides);
            Scribe_Values.Look(ref showLabelPrefix,       "showLabelPrefix",       true);
            Scribe_Values.Look(ref requestExpireTicks,    "requestExpireTicks",    30000);
            Scribe_Values.Look(ref enableShapingVote,     "enableShapingVote",     true);
            Scribe_Values.Look(ref shapingHistoryMaxCount, "shapingHistoryMaxCount", 20);
        }
    }
}
