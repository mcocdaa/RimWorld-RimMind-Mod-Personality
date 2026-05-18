using System;
using Xunit;

namespace RimMind.Personality.Tests
{
    public class PersonalityThoughtMapper_PureTests
    {
        private static readonly string[] SlotDefNames = new[]
        {
            "AIPersonality_Slot_0",
            "AIPersonality_Slot_1",
            "AIPersonality_Slot_2",
        };

        private const int TicksPerHour = 2500;

        private static bool IsAIPersonalityDef(string defName)
        {
            foreach (var s in SlotDefNames)
                if (s == defName) return true;
            return false;
        }

        private static int CalcDurationTicks_AIDecides(float durationHours)
        {
            if (durationHours > 0)
                return (int)(Math.Clamp(durationHours, 1f, 24f) * TicksPerHour);
            return Math.Max(1, (int)(1f * TicksPerHour));
        }

        private static int CalcDurationTicks_Fixed(float thoughtDurationHours)
        {
            return Math.Max(1, (int)(thoughtDurationHours * TicksPerHour));
        }

        [Fact]
        public void IsAIPersonalityDef_Slot0_ReturnsTrue()
        {
            Assert.True(IsAIPersonalityDef("AIPersonality_Slot_0"));
        }

        [Fact]
        public void IsAIPersonalityDef_Slot1_ReturnsTrue()
        {
            Assert.True(IsAIPersonalityDef("AIPersonality_Slot_1"));
        }

        [Fact]
        public void IsAIPersonalityDef_Slot2_ReturnsTrue()
        {
            Assert.True(IsAIPersonalityDef("AIPersonality_Slot_2"));
        }

        [Fact]
        public void IsAIPersonalityDef_OtherDef_ReturnsFalse()
        {
            Assert.False(IsAIPersonalityDef("SomeOtherThought"));
        }

        [Fact]
        public void IsAIPersonalityDef_Null_ReturnsFalse()
        {
            Assert.False(IsAIPersonalityDef(null!));
        }

        [Fact]
        public void CalcDurationTicks_AIDecides_1Hour()
        {
            int ticks = CalcDurationTicks_AIDecides(1f);
            Assert.Equal(2500, ticks);
        }

        [Fact]
        public void CalcDurationTicks_AIDecides_24Hours()
        {
            int ticks = CalcDurationTicks_AIDecides(24f);
            Assert.Equal(60000, ticks);
        }

        [Fact]
        public void CalcDurationTicks_AIDecides_ClampedBelow1()
        {
            int ticks = CalcDurationTicks_AIDecides(0.5f);
            Assert.Equal(2500, ticks);
        }

        [Fact]
        public void CalcDurationTicks_AIDecides_ClampedAbove24()
        {
            int ticks = CalcDurationTicks_AIDecides(48f);
            Assert.Equal(60000, ticks);
        }

        [Fact]
        public void CalcDurationTicks_Fixed_2Hours()
        {
            int ticks = CalcDurationTicks_Fixed(2f);
            Assert.Equal(5000, ticks);
        }

        [Fact]
        public void CalcDurationTicks_Fixed_ZeroHours_Min1()
        {
            int ticks = CalcDurationTicks_Fixed(0f);
            Assert.Equal(1, ticks);
        }

        [Fact]
        public void Constants_SlotDefNames_Count3()
        {
            Assert.Equal(3, SlotDefNames.Length);
        }

        [Fact]
        public void Constants_TicksPerHour_2500()
        {
            Assert.Equal(2500, TicksPerHour);
        }
    }
}
