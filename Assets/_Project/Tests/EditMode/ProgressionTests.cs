using System;
using System.Collections.Generic;
using DemolitionBuster.Core.Progression;
using NUnit.Framework;

namespace DemolitionBuster.Tests
{
    public sealed class ProgressionTests
    {
        private static readonly TestItem Worker = new TestItem("worker", 0);
        private static readonly TestItem Master = new TestItem("master", 250);
        private static readonly TestItem Robot = new TestItem("robot", 600);
        private static readonly IReadOnlyList<IShopItem> Catalog = new IShopItem[] { Worker, Master, Robot };

        [Test]
        public void Progress_PersistsEveryChange()
        {
            var storage = new InMemorySaveStorage();
            var progress = new PlayerProgress(storage);

            progress.AddCoins(30);
            progress.SubmitScore(12);

            var reloaded = new PlayerProgress(storage);
            Assert.That(reloaded.Coins, Is.EqualTo(30));
            Assert.That(reloaded.BestScore, Is.EqualTo(12));
        }

        [Test]
        public void SubmitScore_OnlyKeepsTheBest()
        {
            var progress = new PlayerProgress(new InMemorySaveStorage());

            Assert.That(progress.SubmitScore(20), Is.True);
            Assert.That(progress.SubmitScore(10), Is.False);
            Assert.That(progress.SubmitScore(20), Is.False);
            Assert.That(progress.BestScore, Is.EqualTo(20));
        }

        [Test]
        public void AddCoins_RejectsNegativeAmounts()
        {
            var progress = new PlayerProgress(new InMemorySaveStorage());
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.AddCoins(-1));
        }

        [Test]
        public void Shop_GrantsFreeItemsAndEquipsTheDefault()
        {
            var progress = new PlayerProgress(new InMemorySaveStorage());
            var shop = new ShopService(progress, Catalog);

            Assert.That(shop.GetState(Worker), Is.EqualTo(ShopItemState.Equipped));
            Assert.That(shop.GetState(Master), Is.EqualTo(ShopItemState.Locked));
            Assert.That(shop.EquippedItem, Is.SameAs(Worker));
        }

        [Test]
        public void Purchase_WithoutEnoughCoins_ChangesNothing()
        {
            var storage = new InMemorySaveStorage();
            var progress = new PlayerProgress(storage);
            var shop = new ShopService(progress, Catalog);
            progress.AddCoins(100);

            Assert.That(shop.TryPurchase(Master), Is.EqualTo(PurchaseResult.NotEnoughCoins));
            Assert.That(progress.Coins, Is.EqualTo(100));
            Assert.That(shop.GetState(Master), Is.EqualTo(ShopItemState.Locked));
        }

        [Test]
        public void Purchase_SpendsCoinsUnlocksAndEquips()
        {
            var progress = new PlayerProgress(new InMemorySaveStorage());
            var shop = new ShopService(progress, Catalog);
            IShopItem equipped = null;
            shop.EquippedChanged += item => equipped = item;
            progress.AddCoins(300);

            Assert.That(shop.TryPurchase(Master), Is.EqualTo(PurchaseResult.Success));

            Assert.That(progress.Coins, Is.EqualTo(50));
            Assert.That(shop.GetState(Master), Is.EqualTo(ShopItemState.Equipped));
            Assert.That(shop.GetState(Worker), Is.EqualTo(ShopItemState.Owned));
            Assert.That(equipped, Is.SameAs(Master));
            Assert.That(shop.TryPurchase(Master), Is.EqualTo(PurchaseResult.AlreadyOwned));
        }

        [Test]
        public void Equip_RequiresOwnership()
        {
            var progress = new PlayerProgress(new InMemorySaveStorage());
            var shop = new ShopService(progress, Catalog);

            Assert.That(shop.TryEquip(Robot), Is.False);
            Assert.That(shop.TryEquip(Worker), Is.True);
        }

        [Test]
        public void Shop_RepairsASelectionThatNoLongerExists()
        {
            var save = new SaveData { coins = 5, ownedItemIds = new List<string> { "removed_item" }, selectedItemId = "removed_item" };
            var progress = new PlayerProgress(new InMemorySaveStorage(save));

            var shop = new ShopService(progress, Catalog);

            Assert.That(shop.EquippedItem, Is.SameAs(Worker));
            Assert.That(progress.SelectedItemId, Is.EqualTo("worker"));
        }

        [Test]
        public void Progress_SurvivesALegacySaveWithoutOwnedList()
        {
            var save = new SaveData { coins = 10, ownedItemIds = null };

            var progress = new PlayerProgress(new InMemorySaveStorage(save));

            Assert.That(progress.Owns("worker"), Is.False);
            Assert.That(progress.TryBuy("worker", 0), Is.True);
        }
    }
}
