using Epoch.Core.StateMachine;

namespace Epoch.Core.Domain
{
    /// <summary>
    /// The authoritative aggregate (Core Spec sec.5.2). Only fields whose shape is
    /// already fully locked are declared at M0; lanes, per-side state, the replay log
    /// and the result are added in M1 together with the systems that produce them.
    ///
    /// Invariant RS-1: Age == ((Turn - 1) / 6) + 1, always [Lock 1].
    /// </summary>
    public sealed record RunState(
        string RulesVersion,
        string ContentVersion,
        string ContentPoolHash,
        SeedState Seed,
        int Turn,
        int Age,
        RunPhase Phase)
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

        /// <summary>Invariant RS-1 [Lock 1]. Ages are fixed to turn ranges; nothing accelerates them.</summary>
        public static int AgeForTurn(int turn) => ((turn - 1) / TurnsPerAge) + 1;
    }

    /// <summary>Core Spec sec.5.15. Stream seeds are derived in M1 per sec.10.2.</summary>
    public readonly record struct SeedState(
        SeedCode MasterSeed,
        ulong StreamCardOffer,
        ulong StreamLaneMod,
        ulong StreamTieBreak,
        string Algorithm);
}
