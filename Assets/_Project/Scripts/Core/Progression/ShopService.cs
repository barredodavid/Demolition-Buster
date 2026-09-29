using System;
using System.Collections.Generic;
using System.Linq;

namespace DemolitionBuster.Core.Progression
{
    /// <summary>Anything that can be sold in the shop. Implemented by the DestroyerDefinition asset.</summary>
    public interface IShopItem
    {
        string Id { get; }
        int Price { get; }
    }

    public enum ShopItemState
    {
        Locked,
        Owned,
        Equipped,
    }

    public enum PurchaseResult
    {
        Success,
        AlreadyOwned,
        NotEnoughCoins,
    }

    /// <summary>Shop rules on top of <see cref="PlayerProgress"/>: free items, purchase and equipment.</summary>
    public sealed class ShopService
    {
        private readonly PlayerProgress _progress;
        private readonly IReadOnlyList<IShopItem> _items;

        public IReadOnlyList<IShopItem> Items => _items;

        public IShopItem EquippedItem => _items.FirstOrDefault(i => i.Id == _progress.SelectedItemId) ?? _items[0];

        public event Action<IShopItem> EquippedChanged;

        /// <param name="items">Catalog, in display order. The first item is the default one.</param>
        public ShopService(PlayerProgress progress, IReadOnlyList<IShopItem> items)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            if (items == null || items.Count == 0) throw new ArgumentException("The shop needs at least one item.", nameof(items));
            _items = items;
            EnsureValidState();
        }

        public ShopItemState GetState(IShopItem item)
        {
            if (!_progress.Owns(item.Id)) return ShopItemState.Locked;
            return item.Id == EquippedItem.Id ? ShopItemState.Equipped : ShopItemState.Owned;
        }

        public bool CanAfford(IShopItem item) => _progress.Coins >= item.Price;

        /// <summary>Buys the item and equips it right away, which is what players expect after paying.</summary>
        public PurchaseResult TryPurchase(IShopItem item)
        {
            if (_progress.Owns(item.Id)) return PurchaseResult.AlreadyOwned;
            if (!_progress.TryBuy(item.Id, item.Price)) return PurchaseResult.NotEnoughCoins;

            Equip(item);
            return PurchaseResult.Success;
        }

        public bool TryEquip(IShopItem item)
        {
            if (!_progress.Owns(item.Id)) return false;
            Equip(item);
            return true;
        }

        private void Equip(IShopItem item)
        {
            if (_progress.SelectedItemId == item.Id) return;
            _progress.Select(item.Id);
            EquippedChanged?.Invoke(item);
        }

        /// <summary>Unlocks free items and repairs the selection (e.g. an item removed from the catalog in an update).</summary>
        private void EnsureValidState()
        {
            foreach (IShopItem item in _items)
            {
                if (item.Price == 0 && !_progress.Owns(item.Id)) _progress.TryBuy(item.Id, 0);
            }

            bool selectionValid = _items.Any(i => i.Id == _progress.SelectedItemId) && _progress.Owns(_progress.SelectedItemId);
            if (!selectionValid)
            {
                IShopItem fallback = _items.FirstOrDefault(i => _progress.Owns(i.Id));
                if (fallback == null)
                {
                    // Catalog without any free item: grant the first one so the player always has a destroyer.
                    fallback = _items[0];
                    _progress.TryBuy(fallback.Id, 0);
                }
                _progress.Select(fallback.Id);
            }
        }
    }
}
