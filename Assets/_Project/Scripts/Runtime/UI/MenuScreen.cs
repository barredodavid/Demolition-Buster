using System;
using UnityEngine.UIElements;

namespace DemolitionBuster.UI
{
    public sealed class MenuScreen : UIScreen
    {
        private readonly Label _best;
        private readonly Label _coins;

        public event Action PlayClicked;
        public event Action ShopClicked;

        public MenuScreen(VisualElement root) : base(root)
        {
            _best = Require<Label>("menu-best");
            _coins = Require<Label>("menu-coins");
            Require<Button>("menu-play").clicked += () => PlayClicked?.Invoke();
            Require<Button>("menu-shop").clicked += () => ShopClicked?.Invoke();
        }

        public void SetStats(int bestScore, int coins)
        {
            _best.text = $"RECORD  {bestScore}";
            _coins.text = coins.ToString();
        }
    }
}
