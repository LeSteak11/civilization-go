using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Epoch.Application.Runs;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Rng;
using Epoch.Testing;

namespace Epoch.Application.Tests
{
    /// <summary>
    /// Complete-run goldens: fixed content, fixed seed, fixed commands, recorded result
    /// and hashes (Technical Plan sec.9.1).
    ///
    /// GT-15 proves a run is reproducible **within** a process. This file proves it
    /// across processes and builds, because the expected values live on disk rather than
    /// being recomputed alongside the thing they check. That is the difference between
    /// testing determinism and merely testing that a function is a function.
    ///
    /// The fixture is never regenerated automatically. If these fail, either a rule
    /// changed - in which case the change is the news, and the fixture is regenerated
    /// deliberately with `--emit-goldens` - or determinism broke.
    /// </summary>
    public static class GoldenReplayTests
    {
        private const int GoldenSeedCount = 12;

        private static ValidatedContentSet Content => ContentFixtureLoader.Load();

        private static string GoldenPath =>
            Path.Combine(FixturePaths.Root, "golden", "m1_replay_hashes.json");

        /// <summary>
        /// Deliberately asymmetric. Two identical policies mirror each other exactly and
        /// every match ends 0-0 with every contested tile disputed, which would leave the
        /// goldens silent about scoring, ownership persistence and contested income.
        /// </summary>
        private static MatchRecord RunMatch(ulong master) =>
            HeadlessMatch.Run(
                new SeedCode(SeedCodec.Encode(master)),
                Content,
                new FirstLegalSelectionSource(),
                new LastLegalSelectionSource());

        [TestCase("GT-15c", "Recorded golden runs reproduce their committed hashes and scores")]
        public static void GoldensStillHold()
        {
            if (!File.Exists(GoldenPath))
            {
                throw new AssertionException(
                    "The golden fixture is missing: " + GoldenPath +
                    ". Regenerate it deliberately with: dotnet run --project tests/Epoch.Application.Tests -- --emit-goldens");
            }

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(GoldenPath));
            JsonElement root = document.RootElement;

            Assert.Equal(
                "1.2.0-v1-final",
                root.GetProperty("rulesVersion").GetString(),
                "the fixture speaks for this rules version");

            JsonElement runs = root.GetProperty("runs");
            Assert.Equal(GoldenSeedCount, runs.GetArrayLength(), "every recorded seed is still covered");

            // Guard against a vacuous fixture: if every recorded run were 0-0 these
            // goldens would pass while saying nothing about scoring at all.
            int scoring = 0;
            foreach (JsonElement run in runs.EnumerateArray())
            {
                if (run.GetProperty("playerScore").GetInt32() + run.GetProperty("snapshotScore").GetInt32() > 0)
                {
                    scoring++;
                }
            }

            Assert.True(scoring > 0, "the golden set must contain runs that actually score");

            foreach (JsonElement expected in runs.EnumerateArray())
            {
                string seedText = expected.GetProperty("seed").GetString()!;
                MatchRecord actual = HeadlessMatch.Run(
                    new SeedCode(seedText),
                    Content,
                    new FirstLegalSelectionSource(),
                    new LastLegalSelectionSource());

                Assert.Equal(
                    expected.GetProperty("replayHash").GetString(),
                    actual.ReplayHash,
                    "seed " + seedText + " replay hash");
                Assert.Equal(
                    expected.GetProperty("playerScore").GetInt32(),
                    actual.Result.PlayerScore,
                    "seed " + seedText + " player score");
                Assert.Equal(
                    expected.GetProperty("snapshotScore").GetInt32(),
                    actual.Result.SnapshotScore,
                    "seed " + seedText + " snapshot score");
                Assert.Equal(
                    expected.GetProperty("outcome").GetString(),
                    actual.Result.Outcome.ToString(),
                    "seed " + seedText + " outcome");
                Assert.Equal(
                    expected.GetProperty("finalStateHash").GetString(),
                    actual.Turns[actual.Turns.Count - 1].StateHash,
                    "seed " + seedText + " final state hash");
                Assert.Equal(
                    expected.GetProperty("eventCount").GetInt32(),
                    actual.Events.Count,
                    "seed " + seedText + " event count");
                Assert.Equal(
                    expected.GetProperty("laneModifiers").GetString(),
                    LaneLayout(actual.FinalState),
                    "seed " + seedText + " lane modifier layout");
            }
        }

        /// <summary>Regenerates the fixture. Invoked only by an explicit command-line flag.</summary>
        public static void EmitGoldens()
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\n");
            json.Append("  \"$schema\": \"epoch.fixture.golden-replays/1\",\n");
            json.Append("  \"rulesVersion\": \"1.2.0-v1-final\",\n");
            json.Append("  \"contentVersion\": \"").Append(Content.ContentVersion).Append("\",\n");
            json.Append("  \"contentHash\": \"").Append(Content.ContentHash).Append("\",\n");
            json.Append("  \"selectionPolicy\": \"PLAYER=FirstLegalSelectionSource, SNAPSHOT=LastLegalSelectionSource\",\n");
            json.Append("  \"note\": \"Complete-run goldens for M1. The two sides use deliberately different legal-choice policies so that scoring, ownership and contested income are actually exercised; neither reads future offers or consumes RNG, so a run is a pure function of seed and content. Never regenerate these to make a failing test pass: a change here means a rules or content change, and needs saying so. M2 replaces the placeholder contentHash with the real canonical hash, which will change every value below exactly once, for a documented reason.\",\n");
            json.Append("  \"runs\": [\n");

            for (ulong master = 0; master < GoldenSeedCount; master++)
            {
                MatchRecord match = RunMatch(master);
                if (master > 0)
                {
                    json.Append(",\n");
                }

                json.Append("    { \"seed\": \"").Append(match.Seed.Text).Append('"');
                json.Append(", \"replayHash\": \"").Append(match.ReplayHash).Append('"');
                json.Append(", \"finalStateHash\": \"").Append(match.Turns[match.Turns.Count - 1].StateHash).Append('"');
                json.Append(", \"playerScore\": ").Append(match.Result.PlayerScore);
                json.Append(", \"snapshotScore\": ").Append(match.Result.SnapshotScore);
                json.Append(", \"outcome\": \"").Append(match.Result.Outcome).Append('"');
                json.Append(", \"eventCount\": ").Append(match.Events.Count);
                json.Append(", \"laneModifiers\": \"").Append(LaneLayout(match.FinalState)).Append("\" }");
            }

            json.Append("\n  ]\n}\n");

            Directory.CreateDirectory(Path.GetDirectoryName(GoldenPath)!);
            File.WriteAllText(GoldenPath, json.ToString(), new UTF8Encoding(false));
            Console.WriteLine("Wrote " + GoldenSeedCount + " golden runs to " + GoldenPath);
        }

        private static string LaneLayout(RunState state)
        {
            StringBuilder layout = new StringBuilder();
            for (int i = 0; i < state.Lanes.Count; i++)
            {
                if (i > 0)
                {
                    layout.Append('-');
                }

                layout.Append(state.Lanes[i].Modifier.ToString());
            }

            return layout.ToString();
        }
    }
}
