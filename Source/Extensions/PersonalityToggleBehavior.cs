using RimMind.Application.Common.Interfaces.Extension;

namespace RimMind.Personality
{
    internal sealed class PersonalityToggleBehavior : IToggleBehavior
    {
        public string Id => "personality.toggle";
        public bool IsActive => RimMindPersonalityMod.Settings.enablePersonality;
        public void Toggle() => RimMindPersonalityMod.Settings.enablePersonality = !RimMindPersonalityMod.Settings.enablePersonality;
    }
}
