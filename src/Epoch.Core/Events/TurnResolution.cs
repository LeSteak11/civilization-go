using System.Collections.Generic;

namespace Epoch.Core.Events
{
    /// <summary>
    /// What a resolved turn returns: the next authoritative state, the ordered event
    /// list, and the canonical record appended to the replay (Technical Plan sec.4, sec.6).
    /// Resolution completes before any animation begins.
    /// </summary>
    public sealed record TurnResolution(
        Domain.RunState NextState,
        IReadOnlyList<SimulationEvent> Events,
        CanonicalTurnRecord CommandRecord);

    /// <summary>The canonically serialized commands for one turn, plus the post-turn state hash.</summary>
    public sealed record CanonicalTurnRecord(
        int Turn,
        Commands.CardSelectionCommand Player,
        Commands.CardSelectionCommand Snapshot,
        string StateHash);
}
