using System;

namespace DemolitionBuster.Core.Tower
{
    /// <summary>
    /// The infinite tower. Only a fixed window of blocks exists at any time (a ring buffer):
    /// destroying the base block recycles its slot as the new top block.
    /// Level 0 is the base block, standing at the player's height.
    /// </summary>
    public sealed class TowerModel
    {
        private readonly IBlockGenerator _generator;
        private readonly Block[] _blocks;
        private int _baseSlot;

        /// <summary>Number of blocks kept in memory (and displayed).</summary>
        public int Count => _blocks.Length;

        public int DestroyedCount { get; private set; }

        public TowerModel(int visibleBlocks, IBlockGenerator generator)
        {
            if (visibleBlocks < 2) throw new ArgumentOutOfRangeException(nameof(visibleBlocks), "A tower needs at least 2 blocks.");
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _blocks = new Block[visibleBlocks];
            Reset();
        }

        /// <param name="level">0 = base block, Count - 1 = top block.</param>
        public Block this[int level]
        {
            get
            {
                if (level < 0 || level >= _blocks.Length) throw new ArgumentOutOfRangeException(nameof(level));
                return _blocks[(_baseSlot + level) % _blocks.Length];
            }
        }

        public void Reset()
        {
            _generator.Reset();
            _baseSlot = 0;
            DestroyedCount = 0;
            for (int i = 0; i < _blocks.Length; i++)
            {
                _blocks[i] = _generator.Next();
            }
        }

        /// <summary>Removes the hazards of the lowest <paramref name="levels"/> floors (coins are kept).</summary>
        public void ClearHazards(int levels)
        {
            for (int level = 0; level < levels && level < _blocks.Length; level++)
            {
                int slot = (_baseSlot + level) % _blocks.Length;
                _blocks[slot] = new Block(Side.None, _blocks[slot].CoinValue);
            }
        }

        /// <summary>The player strikes the base block from <paramref name="playerSide"/>.</summary>
        public HitResult Hit(Side playerSide)
        {
            if (playerSide == Side.None) throw new ArgumentException("The player must stand on a side.", nameof(playerSide));

            Block baseBlock = this[0];

            // Moving next to a hazard that is already at ground level is fatal: nothing gets destroyed.
            if (baseBlock.Hazard == playerSide)
            {
                return new HitResult(HitOutcome.Blocked, Block.Empty);
            }

            // The freed base slot becomes the new top of the tower, then everything moves down one level.
            _blocks[_baseSlot] = _generator.Next();
            _baseSlot = (_baseSlot + 1) % _blocks.Length;
            DestroyedCount++;

            bool crushed = this[0].Hazard == playerSide;
            return new HitResult(crushed ? HitOutcome.Crushed : HitOutcome.Cleared, baseBlock);
        }
    }
}
