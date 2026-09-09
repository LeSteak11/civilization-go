namespace Epoch.Core
{
    // Technical Plan sec.6. Interface names follow the plan's own snippets verbatim
    // (no "I" prefix) so that the code and the authority document read the same.
    //
    // M1 implements the simulation as static systems rather than injected services:
    // every one of them is a pure function of authoritative state, so an instance would
    // carry no field a determinism bug could hide in. The one genuine abstraction the
    // Core still needs is the RNG contract, because "no native RNG" [Lock 19] is a rule
    // about a capability rather than about a call site.
    //
    // Concrete entry points:
    //   StateMachine.RunFactory       - S00-S04, run creation and lane assignment
    //   StateMachine.MatchResolver    - the canonical turn pipeline, steps 1-16
    //   Systems.OfferGeneration       - shared indexed offers
    //   Systems.LegalitySystem        - validation gates G1-G5 and the forced-PASS rule
    //   Systems.IncomeSystem          - step 7
    //   Systems.MovementSystem        - step 8
    //   Systems.CombatSystem          - steps 10-11, every co-occupied tile
    //   Systems.ScoringSystem         - steps 12-14
    //   Systems.EffectResolution      - the ActiveEffect total order
    //   Serialization.CanonicalState  - canonical bytes for hashing outside the Core

    /// <summary>
    /// Deterministic bounded draws only. Language- and engine-native RNG is forbidden
    /// in authoritative simulation [Lock 19]; the architecture guard bans System.Random
    /// from this assembly outright.
    /// </summary>
    public interface DeterministicRng
    {
        uint NextUInt32();

        /// <summary>Rejection-sampled, unbiased. Modulo-only selection is not permitted (Core Spec sec.10.2).</summary>
        uint NextBounded(uint exclusiveUpperBound);
    }
}
