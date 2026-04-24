using System;

namespace RimMind.Personality
{
    public class PersonalityResultDto
    {
        public ThoughtEntryDto[] thoughts { get; set; } = Array.Empty<ThoughtEntryDto>();
        public string narrative { get; set; } = string.Empty;
        public PersonalityIdentityDto? identity { get; set; }
    }

    public class ThoughtEntryDto
    {
        public string type { get; set; } = "state";
        public string label { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public int intensity { get; set; }
        public int? duration_hours { get; set; } = null;
    }

    public class PersonalityIdentityDto
    {
        public string[]? motivations { get; set; }
        public string[]? traits { get; set; }
        public string[]? core_values { get; set; }
    }
}
