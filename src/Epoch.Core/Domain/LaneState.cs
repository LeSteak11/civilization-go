namespace Epoch.Core.Domain
{
    /// <summary>
    /// Core Spec sec.5.6. Tiles hold no state of their own in V1 — units carry their own
    /// lane and tile index, and structures are lane-scoped and occupy no tile [Lock 9] —
    /// so a lane is its modifier plus the three pieces of locked per-lane memory.
    ///
    /// Invariant OW-1: <see cref="OwnedBy"/> is null or a single Side, never both, and it
    /// never persists from a turn in which the tile was empty.
    /// </summary>
    public sealed record LaneState(
        LaneId Id,
        LaneModifier Modifier,
        Side? OwnedBy,
        Side? Tile3HolderPrevTurn,
        int EngagementId)
    {
        /// <summary>MR sec.1.3: tile 3 is the only contested tile in a lane.</summary>
        public const int ContestedTile = 3;

        /// <summary>COAST units advance 2 tiles per turn instead of 1 (MR sec.1.3).</summary>
        public int PushDistance => Modifier == LaneModifier.COAST ? 2 : 1;

        public static LaneState New(LaneId id, LaneModifier modifier) =>
            new LaneState(id, modifier, null, null, 0);
    }
}
