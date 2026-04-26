using System;

namespace RimMind.Personality
{
    /// <summary>
    /// 将 AI 返回的 intensity（-3~+3）映射为 RimWorld 心情偏移值。
    /// 纯静态方法，无 RimWorld 依赖，可在单元测试中直接使用。
    /// </summary>
    public static class MoodOffsetCalculator
    {
        // -3→-10, -2→-3, -1→-1, 0→0, 1→+1, 2→+3, 3→+10
        private static readonly float[] MoodTable = { -10f, -3f, -1f, 0f, +1f, +3f, +10f };

        /// <summary>
        /// 根据 AI intensity 值计算 RimWorld 心情偏移量。
        /// intensity 超出 [-3, 3] 时自动 clamp。
        /// </summary>
        public static float CalcMoodOffset(float intensity)
        {
            int idx = Math.Max(0, Math.Min((int)intensity + 3, MoodTable.Length - 1));
            return MoodTable[idx];
        }
    }
}
