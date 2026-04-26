using System;
using System.Collections.Generic;
using RimMind.Core.Agent;
using RimWorld.Planet;
using Verse;

namespace RimMind.Personality.Data
{
    /// <summary>
    /// 每个 Pawn 的人格档案（玩家可编辑 + AI 每日更新）。
    /// 存储在 AIPersonalityWorldComponent，随存档保存。
    /// </summary>
    public class PersonalityProfile : IExposable
    {
        // ── 玩家可编辑 ────────────────────────────────────────────────────
        public string description = string.Empty;
        public string workTendencies = string.Empty;
        public string socialTendencies = string.Empty;

        // ── AI 生成（玩家可查看/覆盖） ────────────────────────────────────
        public string aiNarrative = string.Empty;

        // ── 元数据 ────────────────────────────────────────────────────────
        public bool rimTalkSynced;
        public int lastNarrativeUpdateTick;

        // ── 人格塑造投票 ──────────────────────────────────────────────────
        public List<ShapingRecord> playerShapingHistory = new List<ShapingRecord>();

        public AgentIdentity? agentIdentity;

        public void AddShapingRecord(ShapingRecord record, int maxCount)
        {
            playerShapingHistory.Add(record);
            int effectiveMax = Math.Max(maxCount, 1);
            if (playerShapingHistory.Count > effectiveMax)
                playerShapingHistory.RemoveRange(0, playerShapingHistory.Count - effectiveMax);
        }

        public bool IsEmpty =>
            description.NullOrEmpty() &&
            workTendencies.NullOrEmpty() &&
            socialTendencies.NullOrEmpty() &&
            aiNarrative.NullOrEmpty();

        public void ExposeData()
        {
            // Scribe_Values.Look 在 Nullable 模式下会给 string 字段赋 null（RimWorld 存档系统行为）
#pragma warning disable CS8601
            Scribe_Values.Look(ref description, "description", string.Empty);
            Scribe_Values.Look(ref workTendencies, "workTendencies", string.Empty);
            Scribe_Values.Look(ref socialTendencies, "socialTendencies", string.Empty);
            Scribe_Values.Look(ref aiNarrative, "aiNarrative", string.Empty);
#pragma warning restore CS8601
            Scribe_Values.Look(ref rimTalkSynced, "rimTalkSynced");
            Scribe_Values.Look(ref lastNarrativeUpdateTick, "lastNarrativeUpdateTick");
            Scribe_Collections.Look(ref playerShapingHistory, "playerShapingHistory", LookMode.Deep);
            playerShapingHistory ??= new List<ShapingRecord>();
            Scribe_Deep.Look(ref agentIdentity, "agentIdentity");
            agentIdentity ??= new AgentIdentity();
        }
    }

    /// <summary>
    /// WorldComponent：持有所有 Pawn 的 PersonalityProfile，随存档序列化。
    /// </summary>
    public class AIPersonalityWorldComponent : WorldComponent
    {
        private Dictionary<int, PersonalityProfile> _profiles = new Dictionary<int, PersonalityProfile>();

        private static AIPersonalityWorldComponent? _instance;
        public static AIPersonalityWorldComponent? Instance => _instance;

        public AIPersonalityWorldComponent(World world) : base(world)
        {
            _instance = this;
        }

        public PersonalityProfile GetOrCreate(Pawn pawn)
        {
            int id = pawn.thingIDNumber;
            if (!_profiles.TryGetValue(id, out var profile))
            {
                profile = new PersonalityProfile();
                _profiles[id] = profile;
            }
            return profile;
        }

        public bool TryGet(Pawn pawn, out PersonalityProfile? profile)
            => _profiles.TryGetValue(pawn.thingIDNumber, out profile);

        public void Remove(Pawn pawn) => _profiles.Remove(pawn.thingIDNumber);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _profiles, "profiles", LookMode.Value, LookMode.Deep);
            _profiles ??= new Dictionary<int, PersonalityProfile>();
        }
    }
}
