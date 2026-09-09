using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Epoch.Testing;

namespace Epoch.Core.Tests
{
    /// <summary>
    /// The normative constants imported at M0 are checked against the specification's own
    /// printed reference values. If a fixture is edited, these fail. The damage table in
    /// particular is generated once, offline, and must never be recomputed at runtime
    /// ([Lock 7]); this is the check that it was generated correctly.
    /// </summary>
    public static class FixtureIntegrityTests
    {
        private const string ExpectedRulesVersion = "1.2.0-v1-final";

        [TestCase("FIX-01", "Damage table has 81 integer entries over delta [-40, +40]")]
        public static void DamageTableShape()
        {
            JsonElement root = Load("damage_table.json");

            Assert.Equal(ExpectedRulesVersion, root.GetProperty("rulesVersion").GetString(), "the table is versioned with the rules");
            Assert.Equal(-40, root.GetProperty("deltaMin").GetInt32(), "clamp lower bound");
            Assert.Equal(40, root.GetProperty("deltaMax").GetInt32(), "clamp upper bound");

            int[] damage = root.GetProperty("damageByDelta").EnumerateArray().Select(e => e.GetInt32()).ToArray();
            Assert.Equal(81, damage.Length, "81 entries, delta in [-40, +40] (Core Spec sec.9.3)");
        }

        [TestCase("FIX-02", "Damage table reproduces every reference value the spec prints")]
        public static void DamageTableMatchesSpecReferences()
        {
            int[] damage = Load("damage_table.json")
                .GetProperty("damageByDelta")
                .EnumerateArray()
                .Select(e => e.GetInt32())
                .ToArray();

            // Core Spec sec.9.3 reference row, plus the values used by TV-01..TV-14 in sec.9.4.
            (int Delta, int Expected)[] references =
            {
                (-40, 9), (-25, 13), (-15, 17), (-8, 20), (-7, 21), (-3, 23),
                (0, 25), (3, 27), (5, 28), (6, 29), (7, 30), (8, 31), (9, 31),
                (12, 34), (15, 36), (16, 37), (17, 38), (25, 47), (40, 68),
            };

            foreach ((int delta, int expected) in references)
            {
                Assert.Equal(
                    expected,
                    damage[delta + 40],
                    string.Format(CultureInfo.InvariantCulture, "damage at delta {0} (Core Spec sec.9.3)", delta));
            }
        }

        [TestCase("FIX-03", "Damage rises monotonically with delta and is symmetric about 25 at delta 0")]
        public static void DamageTableIsMonotonic()
        {
            int[] damage = Load("damage_table.json")
                .GetProperty("damageByDelta")
                .EnumerateArray()
                .Select(e => e.GetInt32())
                .ToArray();

            for (int i = 1; i < damage.Length; i++)
            {
                Assert.True(
                    damage[i] >= damage[i - 1],
                    string.Format(CultureInfo.InvariantCulture, "damage must never fall as delta rises (index {0})", i));
            }

            Assert.Equal(25, damage[40], "delta 0 is the 25-damage baseline: four clashes to kill a 100 HP unit");
            Assert.Equal(9, damage[0], "minimum damage; there is no minimum-1 override rule");
            Assert.Equal(68, damage[80], "maximum damage at the clamp");
        }

        [TestCase("FIX-04", "Age table matches the locked Age progression")]
        public static void AgeTableMatchesSpec()
        {
            JsonElement root = Load("age_table.json");
            JsonElement[] ages = root.GetProperty("ages").EnumerateArray().ToArray();
            Assert.Equal(4, ages.Length, "four Ages of six turns");

            (int PerkCost, int UnitPower, int UnitCost, int BuildCost)[] expected =
            {
                (2, 10, 6, 8),
                (3, 12, 8, 11),
                (5, 14, 11, 16),
                (9, 17, 16, 22),
            };

            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(i + 1, ages[i].GetProperty("index").GetInt32(), "Age index");
                Assert.Equal(expected[i].PerkCost, ages[i].GetProperty("perkCost").GetInt32(), "perk cost in Insight");
                Assert.Equal(expected[i].UnitPower, ages[i].GetProperty("unitPower").GetInt32(), "unit Power");
                Assert.Equal(expected[i].UnitCost, ages[i].GetProperty("unitCost").GetInt32(), "unit cost in Growth");
                Assert.Equal(expected[i].BuildCost, ages[i].GetProperty("buildCost").GetInt32(), "build cost in Growth");
                Assert.Equal(i + 1, ages[i].GetProperty("structureTier").GetInt32(), "structure tier equals the resolving Age");
            }

            Assert.Equal(8, root.GetProperty("startingGrowth").GetInt32(), "[Lock 3]");
            Assert.Equal(2, root.GetProperty("startingInsight").GetInt32(), "[Lock 3]");
            Assert.Equal(100, root.GetProperty("unitMaxHp").GetInt32(), "[Lock 5]");
            Assert.Equal(1, root.GetProperty("pointsPerExclusiveContestedTile").GetInt32(), "[Lock 4]");
            Assert.Equal(72, root.GetProperty("maxTheoreticalScore").GetInt32(), "24 turns x 3 lanes x 1 point");
        }

        [TestCase("FIX-05", "Offer weight bands cover all 24 turns and each sums to 100")]
        public static void OfferWeightsAreTotalAndComplete()
        {
            JsonElement[] bands = Load("offer_weights.json").GetProperty("bands").EnumerateArray().ToArray();

            int expectedTurn = 1;
            foreach (JsonElement band in bands)
            {
                int first = band.GetProperty("firstTurn").GetInt32();
                int last = band.GetProperty("lastTurn").GetInt32();
                int sum = band.GetProperty("build").GetInt32()
                        + band.GetProperty("train").GetInt32()
                        + band.GetProperty("advance").GetInt32();

                Assert.Equal(expectedTurn, first, "bands must be contiguous with no gap or overlap");
                Assert.True(last >= first, "a band may not run backwards");
                Assert.Equal(100, sum, string.Format(CultureInfo.InvariantCulture, "weights for turns {0}-{1} must sum to 100", first, last));

                expectedTurn = last + 1;
            }

            Assert.Equal(25, expectedTurn, "the bands must end exactly at turn 24");
        }

        [TestCase("FIX-06", "BUILD is offered through turn 20 and never after [V1 Build Economy Exception]")]
        public static void BuildWindowClosesAtTurn21()
        {
            JsonElement[] bands = Load("offer_weights.json").GetProperty("bands").EnumerateArray().ToArray();

            foreach (JsonElement band in bands)
            {
                int first = band.GetProperty("firstTurn").GetInt32();
                int build = band.GetProperty("build").GetInt32();

                if (first >= 21)
                {
                    Assert.Equal(0, build, "BUILD has 0% offer weight on turns 21-24 (Core Spec sec.6.7 X4)");
                }
                else
                {
                    Assert.True(build > 0, "BUILD may be offered on turns 1-20 (Core Spec sec.6.7 X3)");
                }
            }
        }

        [TestCase("FIX-07", "PRNG vectors are present and shaped for the M1 implementation")]
        public static void PrngVectorsArePresent()
        {
            JsonElement root = Load("prng_vectors.json");

            Assert.Equal(
                "pcg32-xsh-rr-64-32+splitmix64-v13",
                root.GetProperty("algorithm").GetString(),
                "the algorithm string is pinned by rulesVersion (Core Spec sec.5.15)");

            Assert.Equal(3, root.GetProperty("splitMix64").GetArrayLength(), "SM-00, SM-01, SM-02");
            Assert.Equal(2, root.GetProperty("streamDerivation").GetArrayLength(), "STR-00, STR-01");
            Assert.Equal(
                6,
                root.GetProperty("pcg32")[0].GetProperty("firstOutputs").GetArrayLength(),
                "PCG-00 publishes the first six outputs");
            Assert.Equal(
                3,
                root.GetProperty("offerAddresses")[0].GetProperty("addressSeedHexBySlot").GetArrayLength(),
                "OFF-19 covers all three offer slots");
        }

        private static JsonElement Load(string fileName)
        {
            string path = FixturePaths.File(fileName);
            Assert.True(File.Exists(path), "fixture " + fileName + " must exist at " + path);
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.Clone();
        }
    }
}
