using System.Collections.Generic;
using Epoch.Core.Content;

namespace Epoch.Core.Domain
{
    /// <summary>
    /// Core Spec sec.5.3.
    ///
    /// Invariant PS-1: Growth and Insight are never negative; costs are never paid into deficit.
    /// Invariant PS-2: at most one KEYSTONE perk.
    /// Invariant PS-3: no duplicate perkId for a side [Lock 13].
    /// Invariant PS-4: score is monotonically non-decreasing, 0..3 per turn [Lock 4].
    /// </summary>
    public sealed record PlayerState(
        Side Side,
        int Growth,
        int Insight,
        int Score,
        IReadOnlyList<StructureInstance> Structures,
        IReadOnlyList<UnitInstance> Units,
        IReadOnlyList<CardDefinition> Perks,
        bool KeystoneTaken,
        int EntryTile)
    {
        /// <summary>[Lock 3].</summary>
        public const int StartingGrowth = 8;

        /// <summary>[Lock 3].</summary>
        public const int StartingInsight = 2;

        /// <summary>[Lock 8]: PLAYER units enter at tile 1.</summary>
        public const int PlayerEntryTile = 1;

        /// <summary>[Lock 8]: SNAPSHOT units enter at tile 5.</summary>
        public const int SnapshotEntryTile = 5;

        /// <summary>
        /// Diagnostic threshold only (Core Spec sec.7.1 E16, [Final Lock 6]).
        /// Resources have **no gameplay cap**; this never clamps or mutates match state.
        /// </summary>
        public const int ResourceWarningThreshold = 999;

        public static PlayerState NewRun(Side side) =>
            new PlayerState(
                side,
                StartingGrowth,
                StartingInsight,
                0,
                new List<StructureInstance>(),
                new List<UnitInstance>(),
                new List<CardDefinition>(),
                false,
                side == Side.PLAYER ? PlayerEntryTile : SnapshotEntryTile);

        public int Resource(ResourceType resource) =>
            resource == ResourceType.GROWTH ? Growth : Insight;

        public PlayerState WithResource(ResourceType resource, int value) =>
            resource == ResourceType.GROWTH ? this with { Growth = value } : this with { Insight = value };

        /// <summary>The direction this side's units advance: PLAYER +1 (toward tile 5), SNAPSHOT -1.</summary>
        public int AdvanceDirection => Side == Side.PLAYER ? 1 : -1;

        /// <summary>The tile a REACH unit must stand on to support a tile-3 clash [Lock 17 R1], mirrored per side.</summary>
        public int ReachSupportTile => Side == Side.PLAYER ? 2 : 4;

        public bool HasPerk(StableId perkId)
        {
            for (int i = 0; i < Perks.Count; i++)
            {
                if (Perks[i].CardId == perkId)
                {
                    return true;
                }
            }

            return false;
        }

        public int UnitsInLane(LaneId lane)
        {
            int count = 0;
            for (int i = 0; i < Units.Count; i++)
            {
                if (Units[i].LaneId == lane)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
