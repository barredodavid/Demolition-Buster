using System;
using System.Collections.Generic;

namespace DemolitionBuster.Core.Progression
{
    /// <summary>
    /// The player's persistent state (wallet, best score, unlocked destroyers).
    /// Every mutation is saved immediately and raises <see cref="Changed"/>.
    /// </summary>
    public sealed class PlayerProgress
    {
        private readonly ISaveStorage _storage;
        private readonly SaveData _data;

        public int Coins => _data.coins;
        public int BestScore => _data.bestScore;
        public string SelectedItemId => _data.selectedItemId;
        public IReadOnlyList<string> OwnedItemIds => _data.ownedItemIds;

        public event Action Changed;

        public PlayerProgress(ISaveStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _data = storage.Load() ?? new SaveData();
            if (_data.ownedItemIds == null) _data.ownedItemIds = new List<string>();
            _data.coins = Math.Max(0, _data.coins);
        }

        public void AddCoins(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0) return;
            _data.coins += amount;
            Commit();
        }

        /// <summary>Records a finished run. Returns true when it beats the best score.</summary>
        public bool SubmitScore(int score)
        {
            if (score <= _data.bestScore) return false;
            _data.bestScore = score;
            Commit();
            return true;
        }

        public bool Owns(string itemId) => itemId != null && _data.ownedItemIds.Contains(itemId);

        /// <summary>Spends <paramref name="price"/> and unlocks the item in a single save.</summary>
        public bool TryBuy(string itemId, int price)
        {
            if (string.IsNullOrEmpty(itemId)) throw new ArgumentException("Item id is required.", nameof(itemId));
            if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
            if (Owns(itemId) || _data.coins < price) return false;

            _data.coins -= price;
            _data.ownedItemIds.Add(itemId);
            Commit();
            return true;
        }

        public void Select(string itemId)
        {
            if (!Owns(itemId)) throw new InvalidOperationException($"Cannot select '{itemId}': it is not owned.");
            if (_data.selectedItemId == itemId) return;
            _data.selectedItemId = itemId;
            Commit();
        }

        private void Commit()
        {
            _storage.Save(_data);
            Changed?.Invoke();
        }
    }
}
