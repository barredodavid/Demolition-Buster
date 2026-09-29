using System;

namespace DemolitionBuster.Core.Tower
{
    /// <summary>Immutable description of one floor of the tower.</summary>
    public readonly struct Block : IEquatable<Block>
    {
        public static readonly Block Empty = new Block(Side.None, 0);

        public Side Hazard { get; }
        public int CoinValue { get; }

        public bool HasHazard => Hazard != Side.None;
        public bool HasCoin => CoinValue > 0;

        public Block(Side hazard, int coinValue = 0)
        {
            if (coinValue < 0) throw new ArgumentOutOfRangeException(nameof(coinValue));
            Hazard = hazard;
            CoinValue = coinValue;
        }

        public bool Equals(Block other) => Hazard == other.Hazard && CoinValue == other.CoinValue;
        public override bool Equals(object obj) => obj is Block other && Equals(other);
        public override int GetHashCode() => ((int)Hazard * 397) ^ CoinValue;
        public override string ToString() => $"Block(hazard: {Hazard}, coins: {CoinValue})";
    }
}
