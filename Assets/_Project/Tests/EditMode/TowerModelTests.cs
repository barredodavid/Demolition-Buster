using System;
using DemolitionBuster.Core;
using DemolitionBuster.Core.Tower;
using NUnit.Framework;

namespace DemolitionBuster.Tests
{
    public sealed class TowerModelTests
    {
        private static readonly Block HazardLeft = new Block(Side.Left);
        private static readonly Block HazardRight = new Block(Side.Right);

        [Test]
        public void Hit_FromSafeSide_DestroysBaseAndLowersTower()
        {
            var tower = new TowerModel(3, new ScriptedBlockGenerator(Block.Empty, HazardRight, Block.Empty, new Block(Side.None, 5)));

            HitResult result = tower.Hit(Side.Left);

            Assert.That(result.Outcome, Is.EqualTo(HitOutcome.Cleared));
            Assert.That(result.IsFatal, Is.False);
            Assert.That(tower.DestroyedCount, Is.EqualTo(1));
            Assert.That(tower[0], Is.EqualTo(HazardRight), "Former level 1 is now the base.");
            Assert.That(tower[2], Is.EqualTo(new Block(Side.None, 5)), "A new block was generated on top.");
        }

        [Test]
        public void Hit_TowardsHazardAtBaseLevel_IsBlockedAndDestroysNothing()
        {
            var tower = new TowerModel(3, new ScriptedBlockGenerator(HazardRight));

            HitResult result = tower.Hit(Side.Right);

            Assert.That(result.Outcome, Is.EqualTo(HitOutcome.Blocked));
            Assert.That(result.DestroyedABlock, Is.False);
            Assert.That(tower.DestroyedCount, Is.Zero);
            Assert.That(tower[0], Is.EqualTo(HazardRight));
        }

        [Test]
        public void Hit_WhenHazardDescendsOnPlayerSide_IsCrushed()
        {
            var tower = new TowerModel(3, new ScriptedBlockGenerator(Block.Empty, HazardLeft));

            HitResult result = tower.Hit(Side.Left);

            Assert.That(result.Outcome, Is.EqualTo(HitOutcome.Crushed));
            Assert.That(result.IsFatal, Is.True);
            Assert.That(result.DestroyedABlock, Is.True);
        }

        [Test]
        public void Hit_WhenHazardDescendsOnOppositeSide_IsSafe()
        {
            var tower = new TowerModel(3, new ScriptedBlockGenerator(Block.Empty, HazardLeft));

            Assert.That(tower.Hit(Side.Right).Outcome, Is.EqualTo(HitOutcome.Cleared));
        }

        [Test]
        public void ManyHits_RingBufferPreservesGenerationOrder()
        {
            var blocks = new Block[20];
            for (int i = 0; i < blocks.Length; i++) blocks[i] = new Block(Side.None, i);
            var tower = new TowerModel(4, new ScriptedBlockGenerator(blocks));

            for (int i = 0; i < 10; i++) tower.Hit(Side.Left);

            for (int level = 0; level < tower.Count; level++)
            {
                Assert.That(tower[level].CoinValue, Is.EqualTo(10 + level));
            }
        }

        [Test]
        public void Reset_RestoresInitialTower()
        {
            var tower = new TowerModel(3, new ScriptedBlockGenerator(Block.Empty, HazardRight, Block.Empty));
            tower.Hit(Side.Left);

            tower.Reset();

            Assert.That(tower.DestroyedCount, Is.Zero);
            Assert.That(tower[1], Is.EqualTo(HazardRight));
        }

        [Test]
        public void Hit_WithoutSide_Throws()
        {
            var tower = new TowerModel(3, new ScriptedBlockGenerator());
            Assert.Throws<ArgumentException>(() => tower.Hit(Side.None));
        }

        [Test]
        public void Indexer_OutOfRange_Throws()
        {
            var tower = new TowerModel(3, new ScriptedBlockGenerator());
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = tower[3]);
        }
    }
}
