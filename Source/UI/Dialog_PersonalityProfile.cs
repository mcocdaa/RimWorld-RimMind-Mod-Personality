using RimMind.Personality.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimMind.Personality.UI
{
    public class Dialog_PersonalityProfile : Window
    {
        private readonly Pawn _pawn;
        private PersonalityProfile _profile = null!;

        private string _editDesc = "";
        private string _editWork = "";
        private string _editSocial = "";
        private Vector2 _scrollNarrative;

        public Dialog_PersonalityProfile(Pawn pawn)
        {
            _pawn = pawn;
            doCloseX = true;
            draggable = true;
            closeOnAccept = false;
            closeOnCancel = true;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
        }

        public override Vector2 InitialSize => new Vector2(520f, 480f);

        public override void PreOpen()
        {
            base.PreOpen();
            _profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(_pawn) ?? new PersonalityProfile();
            _editDesc = _profile.description ?? "";
            _editWork = _profile.workTendencies ?? "";
            _editSocial = _profile.socialTendencies ?? "";
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = inRect.y;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 30f), "RimMind.Personality.UI.ProfileTitle".Translate(_pawn.LabelShort));
            y += 34f;
            Text.Font = GameFont.Small;

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 20f), "RimMind.Personality.UI.DescriptionLabel".Translate());
            y += 22f;
            _editDesc = Widgets.TextArea(new Rect(inRect.x, y, inRect.width, 64f), _editDesc);
            y += 68f;

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 20f), "RimMind.Personality.UI.WorkTendencies".Translate());
            y += 22f;
            _editWork = Widgets.TextArea(new Rect(inRect.x, y, inRect.width, 42f), _editWork);
            y += 46f;

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 20f), "RimMind.Personality.UI.SocialTendencies".Translate());
            y += 22f;
            _editSocial = Widgets.TextArea(new Rect(inRect.x, y, inRect.width, 42f), _editSocial);
            y += 46f;

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 20f), "RimMind.Personality.UI.AINarrative".Translate());
            y += 22f;
            string narrative = _profile.aiNarrative.NullOrEmpty()
                ? "RimMind.Personality.UI.NarrativeEmpty".Translate()
                : _profile.aiNarrative;
            float narrativeH = Mathf.Min(80f, Text.CalcHeight(narrative, inRect.width - 16f) + 4f);
            Rect narrativeOuterRect = new Rect(inRect.x, y, inRect.width, narrativeH);
            Widgets.BeginScrollView(narrativeOuterRect, ref _scrollNarrative,
                new Rect(0f, 0f, inRect.width - 16f, Text.CalcHeight(narrative, inRect.width - 16f)));
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            Widgets.Label(new Rect(0f, 0f, inRect.width - 16f, 9999f), narrative);
            GUI.color = Color.white;
            Widgets.EndScrollView();
            y += narrativeH + 6f;

            float btnW = (inRect.width - 8f) / 2f;
            float btnY = inRect.yMax - 36f;

            if (Widgets.ButtonText(new Rect(inRect.x, btnY, btnW, 30f), "RimMind.Personality.UI.Save".Translate()))
            {
                _profile.description = _editDesc.Trim();
                _profile.workTendencies = _editWork.Trim();
                _profile.socialTendencies = _editSocial.Trim();
                Messages.Message("RimMind.Personality.UI.ProfileSaved".Translate(_pawn.LabelShort),
                    MessageTypeDefOf.SilentInput, historical: false);
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.x + btnW + 8f, btnY, btnW, 30f), "RimMind.Personality.UI.Cancel".Translate()))
                Close();
        }
    }
}
