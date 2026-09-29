using System;

namespace DemolitionBuster.Core.Utils
{
    /// <summary>Abstraction over randomness so gameplay rules stay deterministic under test.</summary>
    public interface IRandomSource
    {
        /// <summary>Returns a value in [0, 1).</summary>
        float NextFloat();
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SystemRandomSource() : this(Environment.TickCount) { }

        public SystemRandomSource(int seed)
        {
            _random = new Random(seed);
        }

        public float NextFloat() => (float)_random.NextDouble();
    }
}
