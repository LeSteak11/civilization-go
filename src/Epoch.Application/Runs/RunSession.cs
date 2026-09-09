using System;
using System.Collections.Generic;
using Epoch.Application.Snapshot;
using Epoch.Core.Content;
using Epoch.Core.Domain;

namespace Epoch.Application.Runs
{
    /// <summary>
    /// A turn-level checkpoint (Implementation Lock decision 7: after every completed turn).
    ///
    /// <para>It records the run's identity and the commands taken so far - not the board.
    /// That is the same principle a Snapshot uses (Core Spec sec.10.5): the state is a pure
    /// function of seed, content and commands, so storing commands is sufficient and
    /// storing the board would be a second source of truth that could disagree with the
    /// first. It also means resume needs no state deserializer to keep in step with the
    /// domain model.</para>
    ///
    /// <para><see cref="StateHash"/> is the hash of the authoritative state at the moment
    /// the checkpoint was taken. Resume recomputes it and refuses to continue if it
    /// differs, so a checkpoint written by different rules or content fails loudly instead
    /// of silently resuming into a divergent run.</para>
    /// </summary>
    public sealed record RunCheckpoint(
        string RulesVersion,
        string ContentVersion,
        string ContentHash,
        SeedCode Seed,
        int TurnsCompleted,
        IReadOnlyList<Selection> PlayerSelections,
        IReadOnlyList<Selection> SnapshotSelections,
        string StateHash);

    /// <summary>
    /// Replays recorded selections for the turns a checkpoint covers, then hands over to a
    /// live source for the rest of the run.
    ///
    /// This is all "resume" needs: the recorded prefix reproduces the exact board the
    /// checkpoint was taken on, and play continues from there.
    /// </summary>
    public sealed class PrefixSelectionSource : SelectionSource
    {
        private readonly IReadOnlyList<Selection> _recorded;
        private readonly SelectionSource _live;

        public PrefixSelectionSource(IReadOnlyList<Selection> recorded, SelectionSource live)
        {
            _recorded = recorded ?? throw new ArgumentNullException(nameof(recorded));
            _live = live ?? throw new ArgumentNullException(nameof(live));
        }

        public Selection Select(RunState state, Side side, CardOfferSet offers, ValidatedContentSet content) =>
            state.Turn <= _recorded.Count
                ? _recorded[state.Turn - 1]
                : _live.Select(state, side, offers, content);
    }

    /// <summary>
    /// Orchestration for a run: start it, checkpoint it, resume it, replay it, restart it
    /// on the same seed, and record an opponent from it.
    ///
    /// It owns persistence policy and never a gameplay outcome (Technical Plan sec.3.4);
    /// every method here ultimately calls <see cref="HeadlessMatch.Run"/> once.
    /// </summary>
    public static class RunSession
    {
        /// <summary>
        /// Play a run against a version-guarded Snapshot. Throws if the snapshot is not
        /// selectable - the run is never created [Lock 18].
        /// </summary>
        public static MatchRecord PlayAgainst(
            SeedCode seed,
            ValidatedContentSet content,
            OpponentSnapshot snapshot,
            SelectionSource player,
            string rulesVersion)
        {
            SnapshotRejection rejection = SnapshotLibrary.CheckSelectable(snapshot, rulesVersion, content);
            if (rejection != SnapshotRejection.None)
            {
                throw new InvalidOperationException(
                    "Snapshot '" + snapshot.SnapshotId + "' is not selectable for this run: " + rejection +
                    " (Core Spec sec.5.14 OS-2, [Lock 18]). The run is not created.");
            }

            return HeadlessMatch.Run(seed, content, player, new RecordedSelectionSource(snapshot.Selections));
        }

        /// <summary>
        /// Record a completed run's opponent side as a reusable Snapshot.
        /// Invariant OS-1: replaying it against the same seed and versions must reproduce
        /// <see cref="OpponentSnapshot.RecordedScore"/> exactly.
        /// </summary>
        public static OpponentSnapshot RecordSnapshot(string snapshotId, MatchRecord match, ValidatedContentSet content) =>
            new OpponentSnapshot(
                snapshotId,
                match.FinalState.RulesVersion,
                content.ContentVersion,
                match.Seed,
                match.SnapshotSelections(),
                match.Result.SnapshotScore);

        /// <summary>The checkpoint that would be written after <paramref name="turnsCompleted"/> turns.</summary>
        public static RunCheckpoint Checkpoint(MatchRecord match, ValidatedContentSet content, int turnsCompleted)
        {
            if (turnsCompleted < 1 || turnsCompleted > match.Turns.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(turnsCompleted));
            }

            List<Selection> player = new List<Selection>(turnsCompleted);
            List<Selection> snapshot = new List<Selection>(turnsCompleted);
            for (int i = 0; i < turnsCompleted; i++)
            {
                player.Add(match.Turns[i].PlayerSelection);
                snapshot.Add(match.Turns[i].SnapshotSelection);
            }

            return new RunCheckpoint(
                match.FinalState.RulesVersion,
                content.ContentVersion,
                content.ContentHash,
                match.Seed,
                turnsCompleted,
                player,
                snapshot,
                match.Turns[turnsCompleted - 1].StateHash);
        }

        /// <summary>
        /// Resume from a checkpoint: replay its recorded prefix, then continue with the
        /// live sources. The checkpoint's own state hash is verified along the way, so a
        /// checkpoint that does not belong to this rules/content pair fails loudly.
        /// </summary>
        public static MatchRecord Resume(
            RunCheckpoint checkpoint,
            ValidatedContentSet content,
            SelectionSource player,
            SelectionSource snapshot,
            string rulesVersion)
        {
            if (!string.Equals(checkpoint.RulesVersion, rulesVersion, StringComparison.Ordinal) ||
                !string.Equals(checkpoint.ContentVersion, content.ContentVersion, StringComparison.Ordinal) ||
                !string.Equals(checkpoint.ContentHash, content.ContentHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Checkpoint for seed '" + checkpoint.Seed.Text +
                    "' was written under different rules or content and cannot be resumed.");
            }

            MatchRecord resumed = HeadlessMatch.Run(
                checkpoint.Seed,
                content,
                new PrefixSelectionSource(checkpoint.PlayerSelections, player),
                new PrefixSelectionSource(checkpoint.SnapshotSelections, snapshot));

            string reached = resumed.Turns[checkpoint.TurnsCompleted - 1].StateHash;
            if (!string.Equals(reached, checkpoint.StateHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Resume diverged at turn " + checkpoint.TurnsCompleted + ": expected state hash " +
                    checkpoint.StateHash + " but reached " + reached + ".");
            }

            return resumed;
        }

        /// <summary>
        /// Verify a recorded run reproduces itself: same replay hash, same per-turn state
        /// hashes, same result. This is Invariant RE-1 as a callable check.
        /// </summary>
        public static bool VerifyReplay(MatchRecord original, ValidatedContentSet content)
        {
            MatchRecord replayed = HeadlessMatch.Run(
                original.Seed,
                content,
                new RecordedSelectionSource(original.PlayerSelections()),
                new RecordedSelectionSource(original.SnapshotSelections()));

            if (!string.Equals(replayed.ReplayHash, original.ReplayHash, StringComparison.Ordinal))
            {
                return false;
            }

            for (int i = 0; i < original.Turns.Count; i++)
            {
                if (!string.Equals(replayed.Turns[i].StateHash, original.Turns[i].StateHash, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return replayed.Result.PlayerScore == original.Result.PlayerScore &&
                   replayed.Result.SnapshotScore == original.Result.SnapshotScore;
        }
    }
}
