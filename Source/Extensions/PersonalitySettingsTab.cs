using UnityEngine;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Presentation.Settings;
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
