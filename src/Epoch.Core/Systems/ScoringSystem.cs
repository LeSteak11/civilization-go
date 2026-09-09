using System.Collections.Generic;
using Epoch.Core.Domain;

namespace Epoch.Core.Systems
{
    /// <summary>
    /// Steps 12-14 of the canonical turn, Core Spec sec.8.4 and sec.11.2,
    /// [Lock 4], [Lock 15], [Lock 17 R5].
    ///
    /// Ownership is computed **once** per turn, after combat. Three consumers read it:
    /// this turn's score (immediately), and next turn's contested income and HIGHLAND
    /// bonus (both through <c>tile3HolderPrevTurn</c>). That single computation plus one
    /// persisted value is what removes the double-evaluation ambiguity the pre-Lock draft
    /// carried.
    /// </summary>
    public static class ScoringSystem
    {
        public static RunState Resolve(RunState state, List<OwnershipChange> changes)
        {
            int playerPoints = 0;
            int snapshotPoints = 0;
            LaneState[] lanes = new LaneState[state.Lanes.Count];

            for (int i = 0; i < state.Lanes.Count; i++)
            {
                LaneState lane = state.Lanes[i];

                bool playerHolds = HasUnitOnContestedTile(state.Player, lane.Id);
                bool snapshotHolds = HasUnitOnContestedTile(state.Snapshot, lane.Id);

                // M30: exclusively held means >= 1 living unit there and the opponent none.
                // M31 disputed and M32 unoccupied both award nothing, and ownership never
                // persists from a turn in which the tile was empty (Invariant OW-1).
                Side? ownedBy =
                    playerHolds && !snapshotHolds ? Side.PLAYER
                    : snapshotHolds && !playerHolds ? Side.SNAPSHOT
                    : (Side?)null;

                if (ownedBy == Side.PLAYER)
                {
                    playerPoints += RunState.PointsPerExclusiveContestedTile;
                }
                else if (ownedBy == Side.SNAPSHOT)
                {
                    snapshotPoints += RunState.PointsPerExclusiveContestedTile;
                }

                // R5: the engagement ends when tile 3 holds units of at most one side.
                // Incrementing the id clears every REACH guard in that lane at once,
                // without having to walk the units.
                bool engagementContinues = playerHolds && snapshotHolds;
                int engagementId = engagementContinues ? lane.EngagementId : lane.EngagementId + 1;

                if (lane.OwnedBy != ownedBy)
                {
                    changes.Add(new OwnershipChange(lane.Id, lane.OwnedBy, ownedBy));
                }

                lanes[i] = lane with
                {
                    OwnedBy = ownedBy,

                    // Step 14: persist for next turn's income and HIGHLAND check.
                    Tile3HolderPrevTurn = ownedBy,
                    EngagementId = engagementId,
                };
            }

            // Step 13. Invariant PS-4: monotonically non-decreasing, 0..3 per turn.
            // Invariant OW-2: the two sides' points in one turn sum to at most 3.
            return state with
            {
                Lanes = lanes,
                Player = state.Player with { Score = state.Player.Score + playerPoints },
                Snapshot = state.Snapshot with { Score = state.Snapshot.Score + snapshotPoints },
            };
        }

        private static bool HasUnitOnContestedTile(PlayerState side, LaneId lane)
        {
            for (int i = 0; i < side.Units.Count; i++)
            {
                UnitInstance unit = side.Units[i];
                if (unit.LaneId == lane && unit.TileIndex == LaneState.ContestedTile && unit.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>A lane's contested-tile ownership changing hands, for the replay event stream.</summary>
    public readonly record struct OwnershipChange(LaneId Lane, Side? From, Side? To);
}
