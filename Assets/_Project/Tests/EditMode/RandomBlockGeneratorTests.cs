using DemolitionBuster.Core;
using DemolitionBuster.Core.Tower;
using DemolitionBuster.Core.Utils;
using NUnit.Framework;

namespace DemolitionBuster.Tests
{
    public sealed class RandomBlockGeneratorTests
    {
        private static TowerSettings Settings() => new TowerSettings
        {
            safeStartBlocks = 4,
            minGapBetweenHazards = 1,
            minHazardChance = 0.4f,
            maxHazardChance = 0.8f,
            heightForMaxHazardChance = 104,
            coinChance = 0.2f,
            coinValue = 5,
        };

        [Test]
        public void SafeStart_NeverContainsHazards()
        {
            // A random source of 0 would put a hazard everywhere it is allowed.
            var generator = new RandomBlockGenerator(Settings(), new ConstantRandom(0f));

            for (int i = 0; i < 4; i++) Assert.That(generator.Next().HasHazard, Is.False, $"Block {i}");
            Assert.That(generator.Next().HasHazard, Is.True);
        }

        [Test]
        public void Hazards_AreAlwaysSeparatedByTheMinimumGap([Values(1, 2)] int gap)
        {
            TowerSettings settings = Settings();
            settings.minGapBetweenHazards = gap;
            settings.maxHazardChance = 1f;
            var generator = new RandomBlockGenerator(settings, new SystemRandomSource(1234));

            int sinceHazard = int.MaxValue;
            for (int i = 0; i < 2000; i++)
            {
                Block block = generator.Next();
                if (block.HasHazard)
                {
                    Assert.That(sinceHazard, Is.GreaterThanOrEqualTo(gap), $"Block {i}");
                    sinceHazard = 0;
                }
                else if (sinceHazard != int.MaxValue)
                {
                    sinceHazard++;
                }
            }
        }

        [Test]
        public void HazardChance_RampsLinearlyThenCaps()
        {
            var generator = new RandomBlockGenerator(Settings(), new ConstantRandom(0.5f));

            Assert.That(generator.HazardChanceAt(4), Is.EqualTo(0.4f).Within(1e-5));
            Assert.That(generator.HazardChanceAt(54), Is.EqualTo(0.6f).Within(1e-5));
            Assert.That(generator.HazardChanceAt(104), Is.EqualTo(0.8f).Within(1e-5));
            Assert.That(generator.HazardChanceAt(10_000), Is.EqualTo(0.8f).Within(1e-5));
        }

        [Test]
        public void Coins_OnlyAppearOnHazardFreeBlocks()
        {
            var generator = new RandomBlockGenerator(Settings(), new SystemRandomSource(99));
            int coins = 0;

            for (int i = 0; i < 2000; i++)
            {
                Block block = generator.Next();
                if (!block.HasCoin) continue;
                coins++;
                Assert.That(block.HasHazard, Is.False);
                Assert.That(block.CoinValue, Is.EqualTo(5));
            }
            Assert.That(coins, Is.GreaterThan(0));
        }

        [Test]
        public void Reset_ReplaysTheSafeStart()
        {
            var generator = new RandomBlockGenerator(Settings(), new ConstantRandom(0f));
            for (int i = 0; i < 10; i++) generator.Next();

            generator.Reset();

            Assert.That(generator.Next().HasHazard, Is.False);
        }

        [Test]
        public void GeneratedTower_IsAlwaysSurvivable()
        {
            // The gap rule means the base block and the next one never both carry a hazard,
            // so there is always a side that is safe now and after the descent. Play it for 5000 floors.
            var generator = new RandomBlockGenerator(Settings(), new SystemRandomSource(7));
            var tower = new TowerModel(13, generator);

            for (int i = 0; i < 5000; i++)
            {
                Side danger = tower[0].HasHazard ? tower[0].Hazard : tower[1].Hazard;
                Side safeSide = danger == Side.Left ? Side.Right : Side.Left;
                Assert.That(tower.Hit(safeSide).IsFatal, Is.False, $"Floor {i}");
            }
        }
    }
}
