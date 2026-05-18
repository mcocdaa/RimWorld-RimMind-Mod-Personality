using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace RimMind.Personality.Tests
{
    public class PersonalityProfileTests
    {
        [Fact]
        public void IsEmpty_AllFieldsDefault_True()
        {
            var profile = new TestProfile();
            Assert.True(profile.IsEmpty);
        }

        [Fact]
        public void IsEmpty_WithDescription_False()
        {
            var profile = new TestProfile { description = "brave" };
            Assert.False(profile.IsEmpty);
        }

        [Fact]
        public void IsEmpty_WithWorkTendencies_False()
        {
            var profile = new TestProfile { workTendencies = "crafting" };
            Assert.False(profile.IsEmpty);
        }

        [Fact]
        public void IsEmpty_WithSocialTendencies_False()
        {
            var profile = new TestProfile { socialTendencies = "friendly" };
            Assert.False(profile.IsEmpty);
        }

        [Fact]
        public void IsEmpty_WithAiNarrative_False()
        {
            var profile = new TestProfile { aiNarrative = "A brave colonist" };
            Assert.False(profile.IsEmpty);
        }

        [Fact]
        public void AddShapingRecord_WithinMaxCount()
        {
            var profile = new TestProfile();
            profile.AddShapingRecord(new TestShapingRecord { label = "test", action = "action1" }, 5);
            Assert.Single(profile.playerShapingHistory);
        }

        [Fact]
        public void AddShapingRecord_ExceedsMaxCount_OldestEvicted()
        {
            var profile = new TestProfile();
            for (int i = 0; i < 5; i++)
                profile.AddShapingRecord(new TestShapingRecord { label = $"r{i}", action = $"a{i}" }, 3);

            Assert.Equal(3, profile.playerShapingHistory.Count);
            Assert.Equal("r2", profile.playerShapingHistory[0].label);
            Assert.Equal("r4", profile.playerShapingHistory[2].label);
        }

        [Fact]
        public void AddShapingRecord_MaxCountOne_KeepsOnlyLast()
        {
            var profile = new TestProfile();
            profile.AddShapingRecord(new TestShapingRecord { label = "first" }, 1);
            profile.AddShapingRecord(new TestShapingRecord { label = "second" }, 1);

            Assert.Single(profile.playerShapingHistory);
            Assert.Equal("second", profile.playerShapingHistory[0].label);
        }

        [Fact]
        public void AddShapingRecord_MaxCountZero_ClampedToOne()
        {
            var profile = new TestProfile();
            profile.AddShapingRecord(new TestShapingRecord { label = "first" }, 0);
            profile.AddShapingRecord(new TestShapingRecord { label = "second" }, 0);

            Assert.Single(profile.playerShapingHistory);
        }

        private class TestProfile
        {
            public string? description;
            public string? workTendencies;
            public string? socialTendencies;
            public string? aiNarrative;
            public List<TestShapingRecord> playerShapingHistory = new List<TestShapingRecord>();

            public bool IsEmpty => string.IsNullOrEmpty(description)
                && string.IsNullOrEmpty(workTendencies)
                && string.IsNullOrEmpty(socialTendencies)
                && string.IsNullOrEmpty(aiNarrative);

            public void AddShapingRecord(TestShapingRecord record, int maxCount)
            {
                int effectiveMax = Math.Max(1, maxCount);
                playerShapingHistory.Add(record);
                while (playerShapingHistory.Count > effectiveMax)
                    playerShapingHistory.RemoveAt(0);
            }
        }

        private class TestShapingRecord
        {
            public string label = "";
            public string? action;
        }
    }
}
