using RimMind.Application.Common.Interfaces.Extension;

namespace RimMind.Personality
{
    internal sealed class PersonalityActionSkipCheck : ISkipCheck
    {
        public string Id => "personality.action";
        public SkipCheckKind Kind => SkipCheckKind.Action;
        public bool ShouldSkip(in SkipCheckArgs args) => !RimMindPersonalityMod.Settings.enablePersonality;
    }
}
