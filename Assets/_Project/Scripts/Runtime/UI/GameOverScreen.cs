using System;
using DemolitionBuster.Core;
using UnityEngine.UIElements;

namespace DemolitionBuster.UI
{
    public sealed class GameOverScreen : UIScreen
    {
        private readonly Label _cause;
        private readonly Label _score;
        private readonly Label _best;
        private readonly Label _coins;
        private readonly VisualElement _newRecord;

        public event Action RetryClicked;
        public event Action MenuClicked;

        public GameOverScreen(VisualElement root) : base(root)
        {
            _cause = Require<Label>("gameover-cause");
            _score = Require<Label>("gameover-score");
            _best = Require<Label>("gameover-best");
            _coins = Require<Label>("gameover-coins");
            _newRecord = Require<VisualElement>("gameover-record");
            Require<Button>("gameover-retry").clicked += () => RetryClicked?.Invoke();
            Require<Button>("gameover-menu").clicked += () => MenuClicked?.Invoke();
        }

        public void SetResult(DeathCause cause, int score, int bestScore, bool isNewRecord, int coinsEarned)
        {
            _cause.text = DescribeCause(cause);
            _score.text = score.ToString();
            _best.text = $"RECORD  {bestScore}";
            _coins.text = $"+{coinsEarned}";
            _newRecord.EnableInClassList("hidden", !isNewRecord);
        }

        private static string DescribeCause(DeathCause cause)
        {
            switch (cause)
            {
                case DeathCause.Crushed: return "Écrasé par une poutre !";
                case DeathCause.Blocked: return "Droit dans la poutre !";
                case DeathCause.TimeOut: return "Trop lent, chef !";
                default: return string.Empty;
            }
        }
    }
}
