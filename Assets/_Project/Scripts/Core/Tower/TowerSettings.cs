using System;

namespace DemolitionBuster.Core.Tower
{
    /// <summary>Tuning values for the tower. Serialized by Unity inside the GameConfig asset.</summary>
    [Serializable]
    public sealed class TowerSettings
    {
        /// <summary>Blocks kept alive at once. Must cover the tallest supported screen.</summary>
        public int visibleBlocks = 13;

        /// <summary>Hazard-free blocks at the bottom of a fresh tower, so the first taps are free.</summary>
        public int safeStartBlocks = 4;

        /// <summary>Hazard-free blocks forced after each hazard. 1 guarantees readable patterns.</summary>
        public int minGapBetweenHazards = 1;

        public float minHazardChance = 0.45f;
        public float maxHazardChance = 0.85f;

        /// <summary>Tower height at which the hazard chance reaches its maximum.</summary>
        public int heightForMaxHazardChance = 150;

        /// <summary>Chance for a hazard-free block to carry a coin.</summary>
        public float coinChance = 0.12f;

        public int coinValue = 5;
    }
}
