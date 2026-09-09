using System;
using System.Collections.Generic;
using System.Text;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Events;
using Epoch.Core.StateMachine;
using Epoch.Core.Systems;

namespace Epoch.Application.Runs
{
    /// <summary>
    /// Supplies one side's selection for a turn. The Snapshot is a **command source**,
    /// never an alternative simulation path (Technical Plan sec.15): it sees the same
    /// authoritative offers and its own legal state, and it never regenerates or filters
    /// the shared hand.
    /// </summary>
    public interface SelectionSource
    {
        Selection Select(RunState state, Side side, CardOfferSet offers, ValidatedContentSet content);
    }

    /// <summary>
    /// Replays a recorded list of 24 selections (Core Spec sec.5.14 OpponentSnapshot).
    ///
    /// A snapshot stores no board state, no unit positions and no resources - all of it
    /// is recomputed by replaying the selections against the seed, which is what makes a
    /// shareable seed code work at all (sec.10.5).
    /// </summary>
    public sealed class RecordedSelectionSource : SelectionSource
    {
        private readonly IReadOnlyList<Selection> _selections;

        public RecordedSelectionSource(IReadOnlyList<Selection> selections)
        {
            _selections = selections ?? throw new ArgumentNullException(nameof(selections));
        }

        public Selection Select(RunState state, Side side, CardOfferSet offers, ValidatedContentSet content)
        {
            Selection recorded = _selections[state.Turn - 1];

            // OS-2 [Lock 18]: with matching versions, an illegal recorded selection is a
            // determinism error, not a case to paper over. Test builds fail loudly; this
            // is a test/headless build.
            if (LegalitySystem.Validate(state, side, offers, recorded, content) != ValidationError.NONE)
            {
                throw new InvalidOperationException(
                    "Recorded snapshot selection is illegal on turn " + state.Turn +
                    " for " + side + ". This is a determinism error (Core Spec sec.5.14 OS-2).");
            }

            return recorded;
        }
    }

    /// <summary>
    /// Runs a complete 24-turn headless match and records the canonical trail.
    ///
    /// The coordinator owns orchestration and persistence policy, never gameplay
    /// outcomes (Technical Plan sec.3.4): it asks each side for a command, invokes the
    /// Core **once**, and stores what came back.
    /// </summary>
    public static class HeadlessMatch
    {
        public static MatchRecord Run(
            SeedCode seed,
            ValidatedContentSet content,
            SelectionSource player,
            SelectionSource snapshot)
        {
            RunState state = RunFactory.Create(seed, content);

            List<TurnRecord> turns = new List<TurnRecord>(RunState.TurnsPerRun);
            List<SimulationEvent> allEvents = new List<SimulationEvent>();
            int sequence = 0;

            for (int turn = 1; turn <= RunState.TurnsPerRun; turn++)
            {
                if (state.Turn != turn)
                {
                    throw new InvalidOperationException(
                        "Invariant SM-1: turns must execute exactly once, in order. Expected " +
                        turn + " but the state is on " + state.Turn + ".");
                }

                CardOfferSet offers = MatchResolver.GenerateOffers(state, content);

                Selection playerSelection = player.Select(state, Side.PLAYER, offers, content);
                Selection snapshotSelection = snapshot.Select(state, Side.SNAPSHOT, offers, content);

                ResolvedTurn resolved = MatchResolver.Resolve(
                    state, offers, playerSelection, snapshotSelection, content, sequence);

                if (!resolved.IsAccepted)
                {
                    throw new InvalidOperationException(
                        "Turn " + turn + " was rejected for " + resolved.RejectedSide +
                        ": " + resolved.Error + ".");
                }

                state = resolved.NextState!;
                sequence = resolved.NextSequence;

                string stateHash = StateHasher.Sha256Hex(resolved.CanonicalState);
                turns.Add(new TurnRecord(
                    turn,
                    offers,
                    playerSelection,
                    snapshotSelection,
                    resolved.CanonicalState,
                    stateHash,
                    resolved.Events));

                for (int i = 0; i < resolved.Events.Count; i++)
                {
                    allEvents.Add(resolved.Events[i]);
                }
            }

            if (state.Result is null)
            {
                throw new InvalidOperationException("A completed run must carry a MatchResult (Core Spec sec.3.2 S30).");
            }

            return new MatchRecord(seed, state, turns, allEvents, ReplayHash(turns));
        }

        /// <summary>
        /// One digest over every per-turn state hash, in turn order. RE-1: replaying the
        /// events from RUN_INIT must reproduce every one of them bit-for-bit.
        /// </summary>
        private static string ReplayHash(List<TurnRecord> turns)
        {
            StringBuilder text = new StringBuilder(turns.Count * 72);
            for (int i = 0; i < turns.Count; i++)
            {
                text.Append(turns[i].StateHash).Append('\n');
            }

            return StateHasher.Sha256Hex(text.ToString());
        }
    }

    /// <summary>One resolved turn's canonical trail.</summary>
    public sealed record TurnRecord(
        int Turn,
        CardOfferSet Offers,
        Selection PlayerSelection,
        Selection SnapshotSelection,
        string CanonicalState,
        string StateHash,
        IReadOnlyList<SimulationEvent> Events);

    /// <summary>
    /// A complete match: the inputs that produced it and every hash it must reproduce.
    /// MatchResult = f(rulesVersion, contentVersion, seed, playerSelections, snapshotId)
    /// must be pure and total (Core Spec sec.10.4).
    /// </summary>
    public sealed record MatchRecord(
        SeedCode Seed,
        RunState FinalState,
        IReadOnlyList<TurnRecord> Turns,
        IReadOnlyList<SimulationEvent> Events,
        string ReplayHash)
    {
        public MatchResult Result => FinalState.Result!;

        public IReadOnlyList<Selection> PlayerSelections()
        {
            List<Selection> selections = new List<Selection>(Turns.Count);
            for (int i = 0; i < Turns.Count; i++)
            {
                selections.Add(Turns[i].PlayerSelection);
            }

            return selections;
        }

        public IReadOnlyList<Selection> SnapshotSelections()
        {
            List<Selection> selections = new List<Selection>(Turns.Count);
            for (int i = 0; i < Turns.Count; i++)
            {
                selections.Add(Turns[i].SnapshotSelection);
            }

            return selections;
        }
    }
}
