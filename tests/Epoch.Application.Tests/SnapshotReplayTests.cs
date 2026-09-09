using System;
using System.Collections.Generic;
using Epoch.Application.Runs;
using Epoch.Application.Snapshot;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Rng;
using Epoch.Core.StateMachine;
using Epoch.Testing;

namespace Epoch.Application.Tests
{
    /// <summary>
    /// M3 exit criteria: close/reopen/replay/restart reproduce the expected run.
    /// </summary>
    public static class SnapshotReplayTests
    {
        private static ValidatedContentSet Content => ContentFixtureLoader.Load();

        private static SeedCode Seed(ulong master) => new SeedCode(SeedCodec.Encode(master));

        private static MatchRecord Play(ulong master) =>
            HeadlessMatch.Run(
                Seed(master),
                Content,
                new FirstLegalSelectionSource(),
                new LastLegalSelectionSource());

        [TestCase("M3-01", "A recorded Snapshot replays to its recorded score (Invariant OS-1)")]
        public static void SnapshotReplaysToRecordedScore()
        {
            for (ulong master = 0; master < 6; master++)
            {
                MatchRecord original = Play(master);
                OpponentSnapshot snapshot = RunSession.RecordSnapshot("SNAP-" + master, original, Content);

                MatchRecord against = RunSession.PlayAgainst(
                    Seed(master), Content, snapshot, new FirstLegalSelectionSource(), RunFactory.RulesVersion);

                Assert.Equal(
                    snapshot.RecordedScore,
                    against.Result.SnapshotScore,
                    "seed " + master + ": a snapshot must reproduce its recorded score exactly");
                Assert.Equal(
                    original.ReplayHash,
                    against.ReplayHash,
                    "seed " + master + ": and the whole run must be identical");
            }
        }

        [TestCase("GT-14", "A snapshot from another rules or content version is not selectable")]
        public static void VersionGuard()
        {
            OpponentSnapshot good = RunSession.RecordSnapshot("SNAP-OK", Play(1), Content);

            Assert.True(
                SnapshotLibrary.IsSelectable(good, RunFactory.RulesVersion, Content),
                "a matching snapshot is selectable");

            Assert.Equal(
                SnapshotRejection.RulesVersionMismatch,
                SnapshotLibrary.CheckSelectable(good with { RulesVersion = "1.1.0-lock1" }, RunFactory.RulesVersion, Content),
                "GT-14: a stale rulesVersion is rejected");

            Assert.Equal(
                SnapshotRejection.ContentVersionMismatch,
                SnapshotLibrary.CheckSelectable(good with { ContentVersion = "0.9.0-old" }, RunFactory.RulesVersion, Content),
                "a stale contentVersion is rejected");

            Assert.Equal(
                SnapshotRejection.WrongSelectionCount,
                SnapshotLibrary.CheckSelectable(good with { Selections = new List<Selection>() }, RunFactory.RulesVersion, Content),
                "a snapshot must carry exactly 24 selections");

            // [Lock 18]: the run is never created, rather than created and then corrected.
            Assert.Throws<InvalidOperationException>(
                () => RunSession.PlayAgainst(
                    Seed(1),
                    Content,
                    good with { RulesVersion = "1.1.0-lock1" },
                    new FirstLegalSelectionSource(),
                    RunFactory.RulesVersion),
                "an unselectable snapshot must prevent the run, not degrade it");
        }

        [TestCase("M3-02", "A recorded run replays to identical hashes and result (Invariant RE-1)")]
        public static void ReplayVerification()
        {
            for (ulong master = 0; master < 6; master++)
            {
                Assert.True(
                    RunSession.VerifyReplay(Play(master), Content),
                    "seed " + master + " must replay to identical per-turn and replay hashes");
            }
        }

        [TestCase("M3-03", "Same-seed restart reproduces the lane layout and all 72 offers")]
        public static void SameSeedRestart()
        {
            for (ulong master = 0; master < 6; master++)
            {
                // A restart is a fresh run on the same seed. The board and the offers are
                // functions of the seed alone, so they must match even though the players
                // are free to choose differently.
                RunState first = RunFactory.Create(Seed(master), Content);
                RunState second = RunFactory.Create(Seed(master), Content);

                for (int i = 0; i < first.Lanes.Count; i++)
                {
                    Assert.Equal(
                        first.Lanes[i].Modifier,
                        second.Lanes[i].Modifier,
                        "seed " + master + " lane " + first.Lanes[i].Id + " modifier");
                }

                for (int turn = 1; turn <= RunState.TurnsPerRun; turn++)
                {
                    RunState atTurn = first with { Turn = turn, Age = RunState.AgeForTurn(turn) };
                    CardOfferSet a = MatchResolver.GenerateOffers(atTurn, Content);
                    CardOfferSet b = MatchResolver.GenerateOffers(atTurn, Content);
                    for (int slot = 0; slot < a.Count; slot++)
                    {
                        Assert.Equal(
                            a[slot].CardId.Value,
                            b[slot].CardId.Value,
                            "seed " + master + " turn " + turn + " slot " + slot);
                    }
                }
            }
        }

        [TestCase("M3-04", "Resume from a turn-level checkpoint reproduces the original run exactly")]
        public static void CheckpointResume()
        {
            foreach (int at in new[] { 1, 7, 13, 23 })
            {
                MatchRecord original = Play(4);
                RunCheckpoint checkpoint = RunSession.Checkpoint(original, Content, at);

                Assert.Equal(at, checkpoint.TurnsCompleted, "the checkpoint covers " + at + " turns");
                Assert.Equal(at, checkpoint.PlayerSelections.Count, "and carries one command per completed turn");

                MatchRecord resumed = RunSession.Resume(
                    checkpoint,
                    Content,
                    new FirstLegalSelectionSource(),
                    new LastLegalSelectionSource(),
                    RunFactory.RulesVersion);

                Assert.Equal(
                    original.ReplayHash,
                    resumed.ReplayHash,
                    "resuming at turn " + at + " must land on the original run");
                Assert.Equal(
                    original.Result.PlayerScore,
                    resumed.Result.PlayerScore,
                    "resuming at turn " + at + " must reproduce the score");
            }
        }

        [TestCase("M3-05", "A checkpoint from different content is refused rather than resumed")]
        public static void CheckpointVersionGuard()
        {
            RunCheckpoint checkpoint = RunSession.Checkpoint(Play(2), Content, 5);

            Assert.Throws<InvalidOperationException>(
                () => RunSession.Resume(
                    checkpoint with { ContentHash = "sha256:something-else" },
                    Content,
                    new FirstLegalSelectionSource(),
                    new LastLegalSelectionSource(),
                    RunFactory.RulesVersion),
                "a checkpoint written under different content must not silently resume");
        }
    }
}
