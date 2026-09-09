using System.Collections.Generic;
using Epoch.Core.Commands;
using Epoch.Core.Domain;
using Epoch.Core.Effects;
using Epoch.Core.Events;

namespace Epoch.Core
{
    // Technical Plan sec.6. Interface names follow the plan's own snippets verbatim
    // (no "I" prefix) so that the code and the authority document read the same.
    // These are declarations only at M0; M1 supplies the implementations.

    /// <summary>Creates the opening authoritative state after verifying rules/content versions and hash.</summary>
    public interface RunInitializer
    {
        RunState Create(ValidatedContentSetRef content, SeedCode seed, OpponentSnapshotRef snapshot);
    }

    /// <summary>
    /// The single entry point to gameplay. Validate never mutates; Resolve runs the
    /// canonical turn pipeline (Core Spec sec.4.1) and returns immediately.
    /// </summary>
    public interface TurnResolver
    {
        ValidationResult Validate(RunState state, TurnCommand player, TurnCommand snapshot);

        TurnResolution Resolve(
            RunState state,
            TurnCommand player,
            TurnCommand snapshot,
            ValidatedContentSetRef content);
    }

    /// <summary>
    /// Three independent weighted type draws at indexed RNG addresses (Core Spec sec.6.0,
    /// sec.10.2). Never shares RNG consumption with presentation, policies, or logging.
    /// </summary>
    public interface OfferGenerator
    {
        CardOfferSet Generate(SeedState seed, int turn, ValidatedContentSetRef content);
    }

    public interface IncomeSystem
    {
        SystemResult Apply(RunState preState, Side side, EffectContext effects);
    }

    /// <summary>Simultaneous, one tile-step at a time, blocking re-checked before each step [Lock 11].</summary>
    public interface MovementSystem
    {
        SystemResult ResolveSimultaneous(RunState preMovement);
    }

    /// <summary>Resolves every co-occupied tile of all three lanes from one pre-damage snapshot.</summary>
    public interface CombatSystem
    {
        SystemResult ResolveAllCoOccupiedTiles(RunState preDamage, DamageTableRef table);
    }

    public interface ScoringSystem
    {
        SystemResult ResolveOwnershipAndScore(RunState postCombat);
    }

    /// <summary>
    /// Collects applicable effects, partitions ADD before MULTIPLY, sorts within each
    /// partition by priority then source type then stable source id, resolves, clamps,
    /// and rounds exactly once [Final Lock 4].
    /// </summary>
    public interface EffectResolver
    {
        FixedValue Resolve(EffectQuery query, IReadOnlyList<ActiveEffect> applicable);
    }

    /// <summary>
    /// Deterministic bounded draws only. Language- and engine-native RNG is forbidden
    /// in authoritative simulation [Lock 19].
    /// </summary>
    public interface DeterministicRng
    {
        uint NextUInt32();

        /// <summary>Rejection-sampled, unbiased. Modulo-only selection is not permitted (Core Spec sec.10.2).</summary>
        uint NextBounded(uint exclusiveUpperBound);
    }

    /// <summary>The result of one system step: a new state plus the events it emitted.</summary>
    public sealed record SystemResult(RunState State, IReadOnlyList<SimulationEvent> Events);
}
