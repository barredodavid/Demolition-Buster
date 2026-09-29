using System.Collections.Generic;
using DemolitionBuster.Core.Progression;
using DemolitionBuster.Core.Tower;
using DemolitionBuster.Core.Utils;

namespace DemolitionBuster.Tests
{
    /// <summary>Returns the given blocks in order, then empty blocks forever.</summary>
    internal sealed class ScriptedBlockGenerator : IBlockGenerator
    {
        private readonly IReadOnlyList<Block> _blocks;
        private int _index;

        public ScriptedBlockGenerator(params Block[] blocks)
        {
            _blocks = blocks;
        }

        public void Reset() => _index = 0;

        public Block Next() => _index < _blocks.Count ? _blocks[_index++] : Block.Empty;
    }

    internal sealed class ConstantRandom : IRandomSource
    {
        private readonly float _value;

        public ConstantRandom(float value)
        {
            _value = value;
        }

        public float NextFloat() => _value;
    }

    internal sealed class TestItem : IShopItem
    {
        public string Id { get; }
        public int Price { get; }

        public TestItem(string id, int price)
        {
            Id = id;
            Price = price;
        }
    }
}
