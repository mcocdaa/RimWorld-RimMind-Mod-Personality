using RimMind.Core.Internal;
using RimMind.Personality;
using Xunit;

// 端到端纯逻辑流水线测试：JSON 解析 → DTO → 心情偏移计算
// 不依赖 RimWorld，不需要 Pawn / DefDatabase
namespace RimMind.Personality.Tests
{
    public class PersonalityPipelineTests
    {
        // ── 1. state thought → 心情偏移全链路 ────────────────────────────

        [Theory]
        [InlineData(-3, -10f)]
        [InlineData(-1,  -1f)]
        [InlineData( 0,   0f)]
        [InlineData( 1,  +1f)]
        [InlineData( 3, +10f)]
        public void Pipeline_StateThought_MoodOffsetMatchesIntensity(float intensity, float expected)
        {
            string json = $"<Personality>{{\"thoughts\":[{{\"type\":\"state\",\"label\":\"测试\",\"description\":\"描述\",\"intensity\":{intensity}}}],\"narrative\":\"叙事\"}}</Personality>";

            var result = JsonTagExtractor.Extract<PersonalityResultDto>(json, "Personality");

            Assert.NotNull(result);
            var thought = result!.thoughts[0];
            Assert.Equal("state", thought.type);
            Assert.Equal(intensity, thought.intensity, 0.001);
            Assert.Equal(expected, MoodOffsetCalculator.CalcMoodOffset(thought.intensity));
        }

        // ── 2. behavior thought 不影响心情（intensity 通常为 0，但 pipeline 不强制）──

        [Fact]
        public void Pipeline_BehaviorThought_TypeIsBehavior()
        {
            string json = "<Personality>{\"thoughts\":[{\"type\":\"behavior\",\"label\":\"渴望社交\",\"description\":\"想聊天\",\"intensity\":0}],\"narrative\":\"\"}</Personality>";

            var result = JsonTagExtractor.Extract<PersonalityResultDto>(json, "Personality");

            Assert.NotNull(result);
            Assert.Equal("behavior", result!.thoughts[0].type);
            // behavior thought 心情偏移为 0
            Assert.Equal(0f, MoodOffsetCalculator.CalcMoodOffset(result.thoughts[0].intensity));
        }

        // ── 3. 最多 3 个 thought（stackLimit=3），全部解析 ───────────────

        [Fact]
        public void Pipeline_ThreeThoughts_AllParsed()
        {
            string json = "<Personality>{\"thoughts\":[" +
                          "{\"type\":\"state\",\"label\":\"疲惫\",\"description\":\"累了\",\"intensity\":-1}," +
                          "{\"type\":\"state\",\"label\":\"渴望社交\",\"description\":\"孤独\",\"intensity\":-1}," +
                          "{\"type\":\"behavior\",\"label\":\"专注\",\"description\":\"投入工作\",\"intensity\":0}" +
                          "],\"narrative\":\"三种状态并存\"}</Personality>";

            var result = JsonTagExtractor.Extract<PersonalityResultDto>(json, "Personality");

            Assert.NotNull(result);
            Assert.Equal(3, result!.thoughts.Length);
            Assert.Equal(-1f, MoodOffsetCalculator.CalcMoodOffset(result.thoughts[0].intensity));
            Assert.Equal(-1f, MoodOffsetCalculator.CalcMoodOffset(result.thoughts[1].intensity));
            Assert.Equal( 0f, MoodOffsetCalculator.CalcMoodOffset(result.thoughts[2].intensity));
        }

        // ── 4. 超出范围的 intensity 在 CalcMoodOffset 中被 Clamp ─────────

        [Fact]
        public void Pipeline_OutOfRangeIntensity_ClampedInMoodCalc()
        {
            string json = "<Personality>{\"thoughts\":[{\"type\":\"state\",\"label\":\"极端\",\"description\":\"极限\",\"intensity\":99}],\"narrative\":\"\"}</Personality>";

            var result = JsonTagExtractor.Extract<PersonalityResultDto>(json, "Personality");

            Assert.NotNull(result);
            // DTO 保留原始值
            Assert.Equal(99f, result!.thoughts[0].intensity, 0.001);
            // 经过 CalcMoodOffset 后 Clamp 到 +10
            Assert.Equal(+10f, MoodOffsetCalculator.CalcMoodOffset(result.thoughts[0].intensity));
        }

        // ── 5. ThoughtEntryDto 默认值 ──────────────────────────────────────

        [Fact]
        public void ThoughtEntryDto_Defaults_AreCorrect()
        {
            var dto = new ThoughtEntryDto();
            Assert.Equal("state", dto.type);
            Assert.Equal(string.Empty, dto.label);
            Assert.Equal(string.Empty, dto.description);
            Assert.Equal(0f, dto.intensity, 0.001);
        }

        // ── 6. PersonalityResultDto 默认值 ────────────────────────────────

        [Fact]
        public void PersonalityResultDto_Defaults_AreCorrect()
        {
            var dto = new PersonalityResultDto();
            Assert.Empty(dto.thoughts);
            Assert.Equal(string.Empty, dto.narrative);
        }
    }
}
