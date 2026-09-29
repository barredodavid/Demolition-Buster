namespace DemolitionBuster.Core.Tower
{
    public enum HitOutcome
    {
        /// <summary>The base block was destroyed and the player is safe.</summary>
        Cleared,
        /// <summary>The base block was destroyed, but the next block's hazard came down on the player.</summary>
        Crushed,
        /// <summary>The player stepped into a hazard sticking out of the base block. Nothing was destroyed.</summary>
        Blocked,
    }

    public readonly struct HitResult
    {
        public HitOutcome Outcome { get; }

        /// <summary>The block that was destroyed. <see cref="Block.Empty"/> when <see cref="Outcome"/> is Blocked.</summary>
        public Block DestroyedBlock { get; }

        public bool IsFatal => Outcome != HitOutcome.Cleared;
        public bool DestroyedABlock => Outcome != HitOutcome.Blocked;

        public HitResult(HitOutcome outcome, Block destroyedBlock)
        {
            Outcome = outcome;
            DestroyedBlock = destroyedBlock;
        }
    }
}
