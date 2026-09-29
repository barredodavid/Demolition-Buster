using UnityEngine.UIElements;

namespace DemolitionBuster.UI
{
    public sealed class HudScreen : UIScreen
    {
        private const float LowTimeThreshold = 0.25f;
        private const long ScorePopMilliseconds = 90;

        private readonly VisualElement _timeFill;
        private readonly Label _score;
        private readonly Label _coins;
        private readonly Label _hint;

        public HudScreen(VisualElement root) : base(root)
        {
            _timeFill = Require<VisualElement>("timebar-fill");
            _score = Require<Label>("hud-score");
            _coins = Require<Label>("hud-coins");
            _hint = Require<Label>("hud-hint");
        }

        public void SetTime(float normalized)
        {
            _timeFill.style.width = Length.Percent(normalized * 100f);
            _timeFill.EnableInClassList("timebar__fill--low", normalized < LowTimeThreshold);
        }

        public void SetScore(int score, bool animate)
        {
            _score.text = score.ToString();
            if (!animate) return;

            // USS transition does the tween: add the class, remove it a few frames later.
            _score.AddToClassList("hud__score--pop");
            _score.schedule.Execute(() => _score.RemoveFromClassList("hud__score--pop")).ExecuteLater(ScorePopMilliseconds);
        }

        public void SetCoins(int coins) => _coins.text = coins.ToString();

        public void SetHintVisible(bool visible) => _hint.EnableInClassList("hidden", !visible);
    }
}
