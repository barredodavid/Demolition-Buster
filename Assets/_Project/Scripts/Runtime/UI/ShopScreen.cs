using System;
using System.Collections.Generic;
using DemolitionBuster.Config;
using DemolitionBuster.Core.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace DemolitionBuster.UI
{
    /// <summary>
    /// Shop presenter: builds one row per destroyer and forwards purchases to the <see cref="ShopService"/>.
    /// Rows are generated from code, so adding a destroyer only means adding an asset to the catalog.
    /// </summary>
    public sealed class ShopScreen : UIScreen
    {
        private const long DeniedFeedbackMilliseconds = 350;

        private readonly ScrollView _list;
        private readonly Label _coins;
        private readonly List<Row> _rows = new List<Row>();

        private ShopService _shop;
        private PlayerProgress _progress;

        public event Action CloseClicked;

        public ShopScreen(VisualElement root) : base(root)
        {
            _list = Require<ScrollView>("shop-list");
            _coins = Require<Label>("shop-coins");
            Require<Button>("shop-close").clicked += () => CloseClicked?.Invoke();
        }

        public void Bind(ShopService shop, PlayerProgress progress, IReadOnlyList<DestroyerDefinition> destroyers)
        {
            _shop = shop;
            _progress = progress;

            _list.Clear();
            _rows.Clear();
            foreach (DestroyerDefinition destroyer in destroyers)
            {
                var row = new Row(destroyer);
                row.ActionButton.clicked += () => HandleRowClicked(row);
                _rows.Add(row);
                _list.Add(row.Element);
            }
            Refresh();
        }

        public void Refresh()
        {
            _coins.text = _progress.Coins.ToString();
            foreach (Row row in _rows)
            {
                row.Refresh(_shop.GetState(row.Destroyer), _shop.CanAfford(row.Destroyer));
            }
        }

        private void HandleRowClicked(Row row)
        {
            switch (_shop.GetState(row.Destroyer))
            {
                case ShopItemState.Locked:
                    if (_shop.TryPurchase(row.Destroyer) == PurchaseResult.NotEnoughCoins) row.PlayDenied();
                    break;
                case ShopItemState.Owned:
                    _shop.TryEquip(row.Destroyer);
                    break;
            }
            Refresh();
        }

        /// <summary>One shop entry: a layered character preview, texts and an action button.</summary>
        private sealed class Row
        {
            public DestroyerDefinition Destroyer { get; }
            public VisualElement Element { get; }
            public Button ActionButton { get; }

            private readonly Label _actionLabel;
            private readonly VisualElement _priceIcon;

            public Row(DestroyerDefinition destroyer)
            {
                Destroyer = destroyer;
                Element = new VisualElement();
                Element.AddToClassList("shop-item");

                var preview = new VisualElement();
                preview.AddToClassList("shop-item__preview");
                AddLayer(preview, destroyer.IdleFrame, Color.white);
                Element.Add(preview);

                var info = new VisualElement();
                info.AddToClassList("shop-item__info");
                var title = new Label(destroyer.DisplayName);
                title.AddToClassList("shop-item__name");
                var description = new Label(destroyer.Description);
                description.AddToClassList("shop-item__description");
                info.Add(title);
                info.Add(description);
                Element.Add(info);

                // Button content is built from children (label + coin icon) instead of Button.text.
                ActionButton = new Button();
                ActionButton.AddToClassList("button");
                ActionButton.AddToClassList("shop-item__action");
                _actionLabel = new Label { pickingMode = PickingMode.Ignore };
                _actionLabel.AddToClassList("shop-item__action-label");
                _priceIcon = new VisualElement { pickingMode = PickingMode.Ignore };
                _priceIcon.AddToClassList("coin-icon");
                ActionButton.Add(_actionLabel);
                ActionButton.Add(_priceIcon);
                Element.Add(ActionButton);
            }

            public void Refresh(ShopItemState state, bool affordable)
            {
                _actionLabel.text = state switch
                {
                    ShopItemState.Equipped => "ÉQUIPÉ",
                    ShopItemState.Owned => "CHOISIR",
                    _ => Destroyer.Price.ToString(),
                };
                _priceIcon.EnableInClassList("hidden", state != ShopItemState.Locked);
                ActionButton.SetEnabled(state != ShopItemState.Equipped);
                ActionButton.EnableInClassList("button--primary", state == ShopItemState.Owned || (state == ShopItemState.Locked && affordable));
                Element.EnableInClassList("shop-item--equipped", state == ShopItemState.Equipped);
            }

            public void PlayDenied()
            {
                Element.AddToClassList("shop-item--denied");
                Element.schedule.Execute(() => Element.RemoveFromClassList("shop-item--denied")).ExecuteLater(DeniedFeedbackMilliseconds);
            }

            private static void AddLayer(VisualElement parent, Sprite sprite, Color tint)
            {
                if (sprite == null) return;
                var layer = new VisualElement { pickingMode = PickingMode.Ignore };
                layer.AddToClassList("shop-item__layer");
                layer.style.backgroundImage = new StyleBackground(sprite);
                layer.style.unityBackgroundImageTintColor = tint;
                parent.Add(layer);
            }
        }
    }
}
