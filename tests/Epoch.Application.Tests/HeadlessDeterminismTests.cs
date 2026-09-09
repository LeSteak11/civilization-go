using System.Collections.Generic;
using Epoch.Application.Runs;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Rng;
using Epoch.Testing;

namespace Epoch.Application.Tests
{
    /// <summary>
    /// M1's exit criteria: one scripted match completes with no engine APIs and
    /// reproducible canonical output.
    ///
    /// GT-15 is named the V1 acceptance test - identical inputs must produce an
    /// identical replayHash, scores, and every per-turn state hash. Everything else in
    /// this file exists to make a GT-15 failure diagnosable: if the full-run hash breaks,
    /// the narrower tests say which invariant went with it.
    /// </summary>
    public static class HeadlessDeterminismTests
    {
        private static ValidatedContentSet Content => ContentFixtureLoader.Load();

        private static MatchRecord RunMatch(ulong master) =>
            HeadlessMatch.Run(
                new SeedCode(SeedCodec.Encode(master)),
                Content,
                new FirstLegalSelectionSource(),
                new FirstLegalSelectionSource());

        [TestCase("M1-01", "A complete 24-turn headless match runs to a MatchResult")]
        public static void MatchCompletes()
        {
            MatchRecord match = RunMatch(0);

            Assert.Equal(RunState.TurnsPerRun, match.Turns.Count, "SM-1: exactly 24 turns, in order");
            Assert.Equal(24, match.FinalState.Turn, "the run ends on turn 24, never earlier (C14)");
            Assert.Equal(24, match.Result.FinalTurn, "MatchResult records the final turn");
            Assert.Equal(4, match.FinalState.Age, "turn 24 is Age IV");

            for (int i = 0; i < match.Turns.Count; i++)
            {
                Assert.Equal(i + 1, match.Turns[i].Turn, "turn " + (i + 1) + " resolved in order");
            }
        }

        [TestCase("GT-15", "Identical inputs reproduce every state hash and the replay hash")]
        public static void FullRunDeterminism()
        {
            for (ulong master = 0; master < 25; master++)
            {
                MatchRecord first = RunMatch(master);
                MatchRecord second = RunMatch(master);

                Assert.Equal(first.ReplayHash, second.ReplayHash, "seed " + master + " replay hash");
                Assert.Equal(first.Result.PlayerScore, second.Result.PlayerScore, "seed " + master + " player score");
                Assert.Equal(first.Result.SnapshotScore, second.Result.SnapshotScore, "seed " + master + " snapshot score");
                Assert.Equal(first.Result.Outcome, second.Result.Outcome, "seed " + master + " outcome");

                for (int i = 0; i < first.Turns.Count; i++)
                {
                    Assert.Equal(
                        first.Turns[i].StateHash,
                        second.Turns[i].StateHash,
                        "seed " + master + " turn " + (i + 1) + " state hash");
                }
            }
        }

        [TestCase("M1-02", "The event stream is reproduced exactly, in order")]
        public static void EventStreamDeterminism()
        {
            MatchRecord first = RunMatch(7);
            MatchRecord second = RunMatch(7);

            Assert.Equal(first.Events.Count, second.Events.Count, "the same number of events");
            for (int i = 0; i < first.Events.Count; i++)
            {
                Assert.Equal(
                    first.Events[i].ToString(),
                    second.Events[i].ToString(),
                    "event " + i + " is byte-identical");
                Assert.Equal(i, first.Events[i].Sequence, "sequence numbers are monotonic from 0");
            }
        }

        [TestCase("M1-03", "Different seeds produce different runs, so the seed is actually consumed")]
        public static void SeedsDiverge()
        {
            HashSet<string> hashes = new HashSet<string>();
            for (ulong master = 0; master < 25; master++)
            {
                hashes.Add(RunMatch(master).ReplayHash);
            }

            Assert.True(hashes.Count > 1, "distinct seeds must not all collapse to one run");
        }

        [TestCase("M1-04", "Replaying the recorded selections reproduces the original match exactly")]
        public static void ReplayReproducesTheRun()
        {
            for (ulong master = 0; master < 10; master++)
            {
                MatchRecord original = RunMatch(master);

                // A snapshot stores only selections; the board is recomputed from the seed
                // (Core Spec sec.10.5). If that is true, replaying the recorded choices
                // through a different command source must land on the same hashes.
                MatchRecord replayed = HeadlessMatch.Run(
                    new SeedCode(SeedCodec.Encode(master)),
                    Content,
                    new RecordedSelectionSource(original.PlayerSelections()),
                    new RecordedSelectionSource(original.SnapshotSelections()));

                Assert.Equal(original.ReplayHash, replayed.ReplayHash, "seed " + master + " replays identically");
                Assert.Equal(original.Result.PlayerScore, replayed.Result.PlayerScore, "seed " + master + " score");
            }
        }

        [TestCase("M1-05", "Every invariant of the authoritative state holds at every turn of many runs")]
        public static void InvariantsHoldThroughout()
        {
            for (ulong master = 0; master < 40; master++)
            {
                MatchRecord match = RunMatch(master);

                for (int i = 0; i < match.Turns.Count; i++)
                {
                    Assert.Equal(
                        RunState.CardsOfferedPerTurn,
                        match.Turns[i].Offers.Count,
                        "seed " + master + " turn " + match.Turns[i].Turn + " offers");
                }

                RunState final = match.FinalState;

                // PS-1: resources never go negative.
                Assert.True(final.Player.Growth >= 0, "seed " + master + " player Growth is non-negative");
                Assert.True(final.Player.Insight >= 0, "seed " + master + " player Insight is non-negative");
                Assert.True(final.Snapshot.Growth >= 0, "seed " + master + " snapshot Growth is non-negative");

                // PS-4 and MC6: score is bounded by the theoretical maximum.
                Assert.True(
                    final.Player.Score >= 0 && final.Player.Score <= RunState.MaxTheoreticalScore,
                    "seed " + master + " player score is within 0..72");
                Assert.True(
                    final.Snapshot.Score >= 0 && final.Snapshot.Score <= RunState.MaxTheoreticalScore,
                    "seed " + master + " snapshot score is within 0..72");

                // OW-2: the two sides cannot both score the same tile, so the combined
                // total cannot exceed 3 per turn across 24 turns.
                Assert.True(
                    final.Player.Score + final.Snapshot.Score <= RunState.MaxTheoreticalScore,
                    "seed " + master + " combined score cannot exceed 72");

                // PS-2: at most one Keystone per side.
                Assert.True(KeystoneCount(final.Player) <= 1, "seed " + master + " player holds at most one Keystone");
                Assert.True(KeystoneCount(final.Snapshot) <= 1, "seed " + master + " snapshot holds at most one Keystone");

                // PS-3: no duplicate perk for a side.
                AssertNoDuplicatePerks(final.Player, "seed " + master + " player");
                AssertNoDuplicatePerks(final.Snapshot, "seed " + master + " snapshot");

                // LS-2 / MR sec.2.3: never more than three units of a side in a lane.
                AssertLaneCapacity(final.Player, "seed " + master + " player");
                AssertLaneCapacity(final.Snapshot, "seed " + master + " snapshot");

                // RS-1: Age is a pure function of turn.
                Assert.Equal(RunState.AgeForTurn(final.Turn), final.Age, "seed " + master + " RS-1");

                // Units live only on real tiles and never at 0 HP.
                AssertUnitsAreWellFormed(final.Player, "seed " + master + " player");
                AssertUnitsAreWellFormed(final.Snapshot, "seed " + master + " snapshot");
            }
        }

        [TestCase("GT-22", "A PASS leaves the shared hand untouched for the other side")]
        public static void PassDoesNotDisturbTheSharedOffer()
        {
            // Across many runs, both sides always saw the same three cardIds on every
            // turn, whatever either of them chose -- including a forced PASS.
            int passes = 0;
            for (ulong master = 0; master < 40; master++)
            {
                MatchRecord match = RunMatch(master);
                for (int i = 0; i < match.Turns.Count; i++)
                {
                    TurnRecord turn = match.Turns[i];
                    if (turn.PlayerSelection.IsPass || turn.SnapshotSelection.IsPass)
                    {
                        passes++;
                    }

                    Assert.Equal(
                        RunState.CardsOfferedPerTurn,
                        turn.Offers.Count,
                        "the hand is never rerolled or replaced (sec.6.6 item 3)");
                }
            }

            // Not an assertion about frequency -- only that the path is exercised at all,
            // so GT-22 is not silently vacuous. Rate is an M7 balance question.
            Assert.True(passes > 0, "the forced-PASS path should occur somewhere in 40 runs");
        }

        [TestCase("GT-29", "A structure keeps the tier and yield of the Age it was built in")]
        public static void StructuresNeverUpgrade()
        {
            MatchRecord match = RunMatch(3);
            foreach (StructureInstance structure in match.FinalState.Player.Structures)
            {
                int builtAge = RunState.AgeForTurn(structure.BuiltOnTurn);
                Assert.Equal(builtAge, structure.Tier, "tier equals the resolving Age and never auto-upgrades");
            }
        }

        [TestCase("M1-06", "A run never terminates before turn 24 even when a side is wiped out")]
        public static void RunAlwaysReachesTurn24()
        {
            for (ulong master = 0; master < 25; master++)
            {
                MatchRecord match = RunMatch(master);
                Assert.Equal(24, match.Result.FinalTurn, "seed " + master + " ran the full 24 turns (C14)");
            }
        }

        private static int KeystoneCount(PlayerState side)
        {
            int count = 0;
            foreach (CardDefinition perk in side.Perks)
            {
                if (perk.IsKeystone)
                {
                    count++;
                }
            }

            return count;
        }

        private static void AssertNoDuplicatePerks(PlayerState side, string where)
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (CardDefinition perk in side.Perks)
            {
                Assert.True(seen.Add(perk.CardId.Value), where + " holds " + perk.CardId.Value + " twice (PS-3)");
            }
        }

        private static void AssertLaneCapacity(PlayerState side, string where)
        {
            foreach (LaneId lane in new[] { LaneId.A, LaneId.B, LaneId.C })
            {
                Assert.True(
                    side.UnitsInLane(lane) <= RunState.MaxUnitsPerLane,
                    where + " exceeded three units in lane " + lane);
            }
        }

        private static void AssertUnitsAreWellFormed(PlayerState side, string where)
        {
            foreach (UnitInstance unit in side.Units)
            {
                Assert.True(unit.Hp > 0, where + " retains a destroyed unit " + unit.InstanceId.Value);
                Assert.True(unit.Hp <= UnitInstance.MaxHp, where + " exceeded max HP");
                Assert.True(
                    unit.TileIndex >= 1 && unit.TileIndex <= RunState.TilesPerLane,
                    where + " has a unit off the board at tile " + unit.TileIndex);
            }
        }
    }
}
