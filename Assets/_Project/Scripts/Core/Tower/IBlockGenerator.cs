namespace DemolitionBuster.Core.Tower
{
    /// <summary>
    /// Produces the blocks stacked on top of the tower, from bottom to top.
    /// Separating content generation from the tower mechanics keeps both small and testable.
    /// </summary>
    public interface IBlockGenerator
    {
        /// <summary>Restarts generation from the ground floor.</summary>
        void Reset();

        Block Next();
    }
}
