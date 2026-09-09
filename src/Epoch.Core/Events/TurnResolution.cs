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

    /// <summary>
    /// The canonically serialized commands for one turn, plus the canonical post-turn
    /// state text.
    ///
    /// It carries the **bytes**, not a digest. Hashing lives in Application: System.Security
    /// is a forbidden namespace in the Core (Technical Plan sec.3.1), so a Core type that
    /// claimed to hold a hash would be a type the Core could never populate.
    /// </summary>
    public sealed record CanonicalTurnRecord(
        int Turn,
        Domain.Selection Player,
        Domain.Selection Snapshot,
        string CanonicalState);
}
