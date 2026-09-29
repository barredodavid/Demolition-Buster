using System;

namespace DemolitionBuster.Core.Timer
{
    /// <summary>Tuning values for the time bar. Serialized by Unity inside the GameConfig asset.</summary>
    [Serializable]
    public sealed class TimerSettings
    {
        /// <summary>Capacity of the time bar, in seconds.</summary>
        public float maxTime = 6f;

        /// <summary>Time in the bar when a run starts.</summary>
        public float startTime = 3f;

        /// <summary>Seconds refilled by each successful hit.</summary>
        public float refillPerHit = 0.3f;

        /// <summary>Drain speed at the start of a run (seconds lost per second).</summary>
        public float baseDrainRate = 0.9f;

        /// <summary>Extra drain speed added by each hit: the player must tap faster and faster.</summary>
        public float drainRateIncreasePerHit = 0.012f;

        public float maxDrainRate = 2.6f;
    }
}
