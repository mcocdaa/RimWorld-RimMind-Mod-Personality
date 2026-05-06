using RimMind.Contracts.Extension;

namespace RimMind.Personality
{
    internal sealed class PersonalityModCooldown : IModCooldown
    {
        public string Id => "Personality";
        public int CooldownTicks => 36000;
    }
}
