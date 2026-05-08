using UnityEngine;
using RimMind.Contracts.Extension;
using Verse;

namespace RimMind.Personality
{
    internal sealed class PersonalitySettingsTab : ISettingsTab
    {
        private readonly RimMindPersonalityMod _mod;
        public PersonalitySettingsTab(RimMindPersonalityMod mod) { _mod = mod; }
        public string Id => "personality";
        public string Label => "RimMind.Personality.Settings.TabLabel".Translate();
        public void Draw(Rect rect) => RimMindPersonalityMod.DrawSettingsContent(rect);
    }
}
