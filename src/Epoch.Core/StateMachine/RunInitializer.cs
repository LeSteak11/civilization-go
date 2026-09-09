using System;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Rng;
using Epoch.Core.Systems;

namespace Epoch.Core.StateMachine
{
    /// <summary>
    /// States S00-S04 of the run state machine (Core Spec sec.3.1): RUN_INIT, SEED_INIT,
    /// SNAPSHOT_LOAD, LANE_MODIFIER_ASSIGN, RUN_READY.
    ///
    /// Version compatibility is checked **before** any state exists: a snapshot whose
    /// rulesVersion or contentVersion differs is not selectable and the run is never
    /// created [Lock 18], and content compiled against a different rules version is the
    /// same class of failure. Refusing to create the run is the whole point - there is no
    /// migration path, and old snapshots are archived rather than upgraded.
    /// </summary>
    public static class RunFactory
    {
        public const string RulesVersion = "1.2.0-v1-final";

        public static RunState Create(SeedCode seed, ValidatedContentSet content)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (!string.Equals(content.CompatibleRulesVersion, RulesVersion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Content declares compatibleRulesVersion '" + content.CompatibleRulesVersion +
                    "' but the engine implements '" + RulesVersion +
                    "'. The run is not created (Core Spec sec.10.3, [Lock 18]).");
            }

            // S01: derive the three independent indexed streams from the master seed.
            SeedState seedState = IndexedRng.DeriveStreams(seed, SeedState.PinnedAlgorithm);

            // S03: exactly one RIVER, one HIGHLAND and one COAST, shared by both sides.
            LaneState[] lanes = LaneModifierAssignment.Assign(seedState);

            return new RunState(
                RulesVersion,
                content.ContentVersion,
                content.ContentHash,
                seedState,
                1,
                1,
                RunPhase.RUN_READY,
                lanes,
                PlayerState.NewRun(Side.PLAYER),
                PlayerState.NewRun(Side.SNAPSHOT),
                0,
                0,
                null);
        }
    }
}
