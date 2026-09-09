using System;
using System.Collections.Generic;
using Epoch.Core.StateMachine;

namespace Epoch.Core.Domain
{
    /// <summary>
    /// The authoritative aggregate (Core Spec sec.5.2). Everything needed to resolve a
    /// turn is here; nothing else may be consulted. Derived values are recomputed, never
    /// stored, so they can never become a second source of truth.
    ///
    /// Invariant RS-1: <c>Age == ((Turn - 1) / 6) + 1</c>, always [Lock 1]. Ages are fixed
    /// to turn ranges and nothing — ADVANCE included [Lock 2] — accelerates them.
    /// </summary>
    public sealed record RunState(
        string RulesVersion,
        string ContentVersion,
        string ContentPoolHash,
        SeedState Seed,
        int Turn,
        int Age,
        RunPhase Phase,
        IReadOnlyList<LaneState> Lanes,
        PlayerState Player,
        PlayerState Snapshot,
        int UnitSequence,
        int StructureSequence,
        MatchResult? Result)
    {
        /// <summary>Core Spec sec.2.1, all LOCKED.</summary>
        public const int TurnsPerRun = 24;

        public const int AgeCount = 4;

        public const int TurnsPerAge = 6;

        public const int CardsOfferedPerTurn = 3;

        public const int LaneCount = 3;

        public const int TilesPerLane = 5;

        public const int MaxUnitsPerLane = 3;

        public const int ContestedTileIndex = 3;

        /// <summary>Core Spec sec.2.2, LOCKED.</summary>
        public const int BaseIncomeGrowth = 3;

        public const int BaseIncomeInsight = 2;

        /// <summary>+1 Growth and +1 Insight per held contested tile (MR sec.2.1).</summary>
        public const int ContestedTileYield = 1;

        /// <summary>1 point per exclusively held contested tile [Lock 4].</summary>
        public const int PointsPerExclusiveContestedTile = 1;

        /// <summary>DERIVED: 24 turns x 3 lanes x 1 point.</summary>
        public const int MaxTheoreticalScore = 72;

        /// <summary>Invariant RS-1 [Lock 1].</summary>
        public static int AgeForTurn(int turn) => ((turn - 1) / TurnsPerAge) + 1;

        public PlayerState SideState(Side side) => side == Side.PLAYER ? Player : Snapshot;

        public RunState WithSideState(PlayerState updated) =>
            updated.Side == Side.PLAYER ? this with { Player = updated } : this with { Snapshot = updated };

        public LaneState Lane(LaneId id)
        {
            for (int i = 0; i < Lanes.Count; i++)
            {
                if (Lanes[i].Id == id)
                {
                    return Lanes[i];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(id));
        }

        public RunState WithLane(LaneState lane)
        {
            LaneState[] lanes = new LaneState[Lanes.Count];
            for (int i = 0; i < Lanes.Count; i++)
            {
                lanes[i] = Lanes[i].Id == lane.Id ? lane : Lanes[i];
            }

            return this with { Lanes = lanes };
        }

        /// <summary>The opposing side. There are exactly two.</summary>
        public static Side Opponent(Side side) => side == Side.PLAYER ? Side.SNAPSHOT : Side.PLAYER;
    }

    /// <summary>Core Spec sec.5.15.</summary>
    public readonly record struct SeedState(
        SeedCode MasterSeed,
        ulong StreamCardOffer,
        ulong StreamLaneMod,
        ulong StreamTieBreak,
        string Algorithm)
    {
        /// <summary>Pinned by rulesVersion 1.2.0-v1-final [Final Lock 5].</summary>
        public const string PinnedAlgorithm = "pcg32-xsh-rr-64-32+splitmix64-v13";
    }
}
