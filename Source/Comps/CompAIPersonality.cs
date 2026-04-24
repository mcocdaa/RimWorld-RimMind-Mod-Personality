using RimMind.Core;
using RimMind.Core.Comps;
using RimMind.Core.Context;
using RimMind.Personality.Data;
using RimWorld;
using Verse;

namespace RimMind.Personality.Comps
{
    public enum TriggerEventType { Injury, Skill, Incident, Death }

    public class CompProperties_AIPersonality : CompProperties
    {
        public CompProperties_AIPersonality()
        {
            compClass = typeof(CompAIPersonality);
        }
    }

    /// <summary>
    /// 挂载到 Pawn 的 ThingComp，负责触发 AI 人格评估。
    /// 支持每日定时触发和事件驱动触发（外部 Patch 通过 TriggerEvent 注入）。
    /// </summary>
    public class CompAIPersonality : ThingComp
    {
        private const int DailyInterval = 60000;
        private const int JitterRange = 3000;
        private const int EventCooldownTicks = 1200;

        private bool _hasPendingRequest;
        private int _lastEventTick = -EventCooldownTicks;
        private int _pendingRequestTick;
        private string? _pendingEventContext;
        private int _dailyJitter = -1;

        private Pawn Pawn => (Pawn)parent;
        private AIPersonalitySettings Settings => RimMindPersonalityMod.Settings;

        private int GetDailyJitter()
        {
            if (_dailyJitter < 0)
                _dailyJitter = new System.Random(Pawn.thingIDNumber ^ 0x3C3C3C3C).Next(-JitterRange, JitterRange + 1);
            return _dailyJitter;
        }

        public override void CompTick()
        {
            if (!Settings.enablePersonality) return;
            if (RimMindAPI.IsConfigured() == false) return;
            if (CompPawnAgent.IsAgentActive(Pawn)) return;
            if (_hasPendingRequest)
            {
                if (Find.TickManager.TicksGame - _pendingRequestTick > 60000)
                {
                    Log.Warning($"[RimMind-Personality] Pending request timeout for {Pawn.Name.ToStringShort}, resetting.");
                    _hasPendingRequest = false;
                }
                else
                {
                    return;
                }
            }
            if (!IsEligible()) return;

            bool dailyFire = Settings.enableDailyEval && Pawn.IsHashIntervalTick(DailyInterval + GetDailyJitter());
            bool eventFire = _pendingEventContext != null &&
                             Find.TickManager.TicksGame - _lastEventTick >= EventCooldownTicks;

            if (!dailyFire && !eventFire) return;

            string? eventCtx = _pendingEventContext;
            _pendingEventContext = null;
            _lastEventTick = Find.TickManager.TicksGame;
            _hasPendingRequest = true;
            _pendingRequestTick = Find.TickManager.TicksGame;

            // ContextEngine + RequestStructured 路径
            var ctxRequest = new ContextRequest
            {
                NpcId = $"NPC-{Pawn.ThingID}",
                Scenario = ScenarioIds.Personality,
                Budget = PersonalityThoughtMapper.GetPersonalityBudget(),
                CurrentQuery = eventCtx,
                ExcludeKeys = new[] { PersonalityThoughtMapper.DefaultExcludeKey },
                MaxTokens = PersonalityThoughtMapper.DefaultMaxTokens,
                Temperature = PersonalityThoughtMapper.DefaultTemperature,
            };

            var schema = PersonalityThoughtMapper.EvaluationSchema;

            RimMindAPI.RequestStructured(ctxRequest, schema, response =>
            {
                _hasPendingRequest = false;
                PersonalityThoughtMapper.Apply(response, Pawn);
            });
        }

        /// <summary>
        /// 从外部 Patch（受伤、技能升级、事件等）触发一次人格评估。
        /// </summary>
        public void TriggerEvent(string context, TriggerEventType eventType = TriggerEventType.Incident)
        {
            if (!Settings.enablePersonality) return;

            bool enabled = eventType switch
            {
                TriggerEventType.Injury => Settings.enableInjuryTrigger,
                TriggerEventType.Skill => Settings.enableSkillTrigger,
                TriggerEventType.Incident => Settings.enableIncidentTrigger,
                TriggerEventType.Death => Settings.enableDeathTrigger,
                _ => true,
            };
            if (!enabled) return;

            _pendingEventContext = context;
        }

        private bool IsEligible() =>
            Pawn.IsFreeNonSlaveColonist &&
            !Pawn.Dead &&
            Pawn.Map != null &&
            Pawn.needs?.mood != null;

        // ContextEngine 接管，不再手动构建 SystemPrompt

        // 从 ContextSettings 读取人格场景预算
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref _lastEventTick, "lastEventTick", -EventCooldownTicks);
            Scribe_Values.Look(ref _dailyJitter, "dailyJitter", -1);
        }
    }
}
