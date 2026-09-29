using System;
using DemolitionBuster.Core.Utils;

namespace DemolitionBuster.Core.Tower
{
    /// <summary>
    /// Default level design rules:
    /// a safe start, a forced gap after each hazard, and a hazard chance that ramps up with height.
    /// </summary>
    public sealed class RandomBlockGenerator : IBlockGenerator
    {
        private readonly TowerSettings _settings;
        private readonly IRandomSource _random;
        private int _height;
        private int _blocksSinceHazard;

        public RandomBlockGenerator(TowerSettings settings, IRandomSource random)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            Reset();
        }

        public void Reset()
        {
            _height = 0;
            _blocksSinceHazard = int.MaxValue;
        }

        public Block Next()
        {
            int height = _height++;
            if (height < _settings.safeStartBlocks)
            {
                return Block.Empty;
            }

            Side hazard = Side.None;
            if (_blocksSinceHazard >= _settings.minGapBetweenHazards && _random.NextFloat() < HazardChanceAt(height))
            {
                hazard = _random.NextFloat() < 0.5f ? Side.Left : Side.Right;
            }

            if (hazard != Side.None) _blocksSinceHazard = 0;
            else if (_blocksSinceHazard < int.MaxValue) _blocksSinceHazard++;

            int coins = hazard == Side.None && _random.NextFloat() < _settings.coinChance ? _settings.coinValue : 0;
            return new Block(hazard, coins);
        }

        public float HazardChanceAt(int height)
        {
            int ramp = Math.Max(1, _settings.heightForMaxHazardChance - _settings.safeStartBlocks);
            float t = Math.Min(1f, Math.Max(0f, (height - _settings.safeStartBlocks) / (float)ramp));
            return _settings.minHazardChance + (_settings.maxHazardChance - _settings.minHazardChance) * t;
        }
    }
}
