using Xunit;
using RimMind.Personality;

// 测试纯逻辑层，不依�?RimWorld
namespace RimMind.Personality.Tests
{
    public class MoodOffsetTests
    {
        // ── 七档标准映射 ──────────────────────────────────────────────────

        [Theory]
        [InlineData(-3, -10f)]
        [InlineData(-2,  -3f)]
        [InlineData(-1,  -1f)]
        [InlineData( 0,   0f)]
        [InlineData( 1,  +1f)]
        [InlineData( 2,  +3f)]
        [InlineData( 3, +10f)]
        public void CalcMoodOffset_StandardRange_ReturnsCorrectValue(int intensity, float expected)
        {
            float result = MoodOffsetCalculator.CalcMoodOffset(intensity);
            Assert.Equal(expected, result);
        }

        // ── 超出范围：自�?Clamp ──────────────────────────────────────────

        [Fact]
        public void CalcMoodOffset_IntensityAboveMax_ClampsToPlus10()
        {
            float result = MoodOffsetCalculator.CalcMoodOffset(99);
            Assert.Equal(+10f, result);
        }

        [Fact]
        public void CalcMoodOffset_IntensityBelowMin_ClampsToMinus10()
        {
            float result = MoodOffsetCalculator.CalcMoodOffset(-99);
            Assert.Equal(-10f, result);
        }

        // ── 边界�?────────────────────────────────────────────────────────

        [Fact]
        public void CalcMoodOffset_MinBoundary_ReturnsMinus10()
        {
            Assert.Equal(-10f, MoodOffsetCalculator.CalcMoodOffset(-3));
        }

        [Fact]
        public void CalcMoodOffset_MaxBoundary_ReturnsPlus10()
        {
            Assert.Equal(+10f, MoodOffsetCalculator.CalcMoodOffset(3));
        }
    }
}
