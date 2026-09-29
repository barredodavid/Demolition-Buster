namespace DemolitionBuster.Core
{
    /// <summary>A side of the tower: where the player stands or where a hazard sticks out.</summary>
    public enum Side
    {
        None = 0,
        Left = 1,
        Right = 2,
    }

    public static class SideExtensions
    {
        public static Side Opposite(this Side side)
        {
            switch (side)
            {
                case Side.Left: return Side.Right;
                case Side.Right: return Side.Left;
                default: return Side.None;
            }
        }

        /// <summary>-1 for left, +1 for right, 0 for none. Handy for positioning views.</summary>
        public static int Sign(this Side side)
        {
            switch (side)
            {
                case Side.Left: return -1;
                case Side.Right: return 1;
                default: return 0;
            }
        }
    }
}
