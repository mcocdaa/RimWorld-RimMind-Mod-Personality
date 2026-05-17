using System;
using RimMind.Application.Features.Json;
using RimMind.Personality;
using Xunit;

namespace RimMind.Personality.Tests
{
    public class PersonalityResultParseTests
    {
        [Fact]
        public void Parse_ValidResponse_ReturnsPersonalityResult()
        {
            string input = "AI 叙述文字...\n<Personality>{\"thoughts\":[" +
                           "{\"type\":\"state\",\"label\":\"三天挖矿受够了\",\"description\":\"Alice感到疲惫\",\"intensity\":-1}" +
                           "],\"narrative\":\"Alice陷入疲惫\"}</Personality>";

            var result = JsonTagExtractor.Extract<PersonalityResultDto>(input, "Personality");

            Assert.NotNull(result);
            Assert.Equal("Alice陷入疲惫", result!.narrative);
            Assert.Single(result.thoughts);
            Assert.Equal("state", result.thoughts[0].type);
            Assert.Equal("三天挖矿受够了", result.thoughts[0].label);
            Assert.Equal("Alice感到疲惫", result.thoughts[0].description);
            Assert.Equal(-1, result.thoughts[0].intensity);
        }

        [Fact]
        public void Parse_MultipleThoughts_ReturnsAll()
        {
            string input = "<Personality>{\"thoughts\":[" +
                           "{\"type\":\"state\",\"label\":\"疲惫\",\"description\":\"累了\",\"intensity\":-1}," +
                           "{\"type\":\"behavior\",\"label\":\"渴望社交\",\"description\":\"想聊天\",\"intensity\":0}" +
                           "],\"narrative\":\"略\"}</Personality>";

            var result = JsonTagExtractor.Extract<PersonalityResultDto>(input, "Personality");

            Assert.NotNull(result);
            Assert.Equal(2, result!.thoughts.Length);
            Assert.Equal("behavior", result.thoughts[1].type);
            Assert.Equal("渴望社交", result.thoughts[1].label);
        }

        [Fact]
        public void Parse_MissingTag_ReturnsNull()
        {
            string input = "AI 只输出了纯文字，没有标签";
            var result = JsonTagExtractor.Extract<PersonalityResultDto>(input, "Personality");
            Assert.Null(result);
        }

        [Fact]
        public void Parse_MalformedJson_ReturnsNull()
        {
            string input = "<Personality>{not valid}</Personality>";
            var result = JsonTagExtractor.Extract<PersonalityResultDto>(input, "Personality");
            Assert.Null(result);
        }

        [Fact]
        public void Parse_EmptyThoughtsArray_ReturnsEmptyArray()
        {
            string input = "<Personality>{\"thoughts\":[],\"narrative\":\"平静的一天\"}</Personality>";
            var result = JsonTagExtractor.Extract<PersonalityResultDto>(input, "Personality");

            Assert.NotNull(result);
            Assert.Empty(result!.thoughts);
            Assert.Equal("平静的一天", result.narrative);
        }

        [Fact]
        public void Parse_IntensityOutOfRange_ParsesRawValue()
        {
            string input = "<Personality>{\"thoughts\":[" +
                           "{\"type\":\"state\",\"label\":\"极端\",\"description\":\"极端状态\",\"intensity\":99}" +
                           "],\"narrative\":\"极端\"}</Personality>";

            var result = JsonTagExtractor.Extract<PersonalityResultDto>(input, "Personality");

            Assert.NotNull(result);
            Assert.Equal(99f, result!.thoughts[0].intensity, 0.001);
            Assert.Equal(+10f, MoodOffsetCalculator.CalcMoodOffset(result.thoughts[0].intensity));
        }

        [Fact]
        public void Parse_MissingNarrative_ReturnsEmptyString()
        {
            string input = "<Personality>{\"thoughts\":[]}</Personality>";
            var result = JsonTagExtractor.Extract<PersonalityResultDto>(input, "Personality");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result!.narrative);
        }
    }
}
