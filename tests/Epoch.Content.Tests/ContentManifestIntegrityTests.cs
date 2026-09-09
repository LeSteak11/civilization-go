using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Epoch.Testing;

namespace Epoch.Content.Tests
{
    /// <summary>
    /// M0 verifies the shipped content manifest independently of the Markdown Content
    /// Manifest's own validation appendix. The strict loader, canonical hash and effect
    /// coverage arrive at M2; what is checked here is everything provable from the bytes
    /// alone, so that a content edit cannot quietly invalidate the counts and integrity
    /// claims the design documents assert.
    /// </summary>
    public static class ContentManifestIntegrityTests
    {
        private static readonly string[] Sections = { "builds", "trains", "perks", "keystones" };

        [TestCase("CNT-01", "Manifest declares the expected schema, content and rules versions")]
        public static void Versions()
        {
            JsonElement root = Manifest();
            Assert.Equal("1.0.0", root.GetProperty("schemaVersion").GetString(), "schema version");
            Assert.Equal("1.0.1-v1", root.GetProperty("contentVersion").GetString(), "content version");
            Assert.Equal("1.2.0-v1-final", root.GetProperty("compatibleRulesVersion").GetString(), "compatible rules version");
        }

        [TestCase("CNT-02", "Roster is 4 BUILD / 16 TRAIN / 24 perks / 4 Keystones = 48 entries")]
        public static void RosterCounts()
        {
            JsonElement root = Manifest();
            Assert.Equal(4, root.GetProperty("builds").GetArrayLength(), "BUILD definitions");
            Assert.Equal(16, root.GetProperty("trains").GetArrayLength(), "TRAIN definitions");
            Assert.Equal(24, root.GetProperty("perks").GetArrayLength(), "regular perks");
            Assert.Equal(4, root.GetProperty("keystones").GetArrayLength(), "Keystones");
            Assert.Equal(48, AllEntries().Count, "total authored entries");
        }

        [TestCase("CNT-03", "Entry ids and effect ids are globally unique")]
        public static void IdsAreUnique()
        {
            List<JsonElement> entries = AllEntries();

            List<string> entryIds = entries.Select(Id).ToList();
            Assert.Equal(entryIds.Count, entryIds.Distinct(StringComparer.Ordinal).Count(), "duplicate entry id");

            List<string> effectIds = entries
                .SelectMany(e => e.GetProperty("effects").EnumerateArray())
                .Select(fx => fx.GetProperty("effectId").GetString() ?? string.Empty)
                .ToList();
            Assert.Equal(effectIds.Count, effectIds.Distinct(StringComparer.Ordinal).Count(), "duplicate effect id");
        }

        [TestCase("CNT-04", "Every effect names its containing entry as sourceId")]
        public static void EffectSourceIntegrity()
        {
            List<string> violations = new List<string>();

            foreach (JsonElement entry in AllEntries())
            {
                foreach (JsonElement fx in entry.GetProperty("effects").EnumerateArray())
                {
                    string sourceId = fx.GetProperty("sourceId").GetString() ?? string.Empty;
                    if (!string.Equals(sourceId, Id(entry), StringComparison.Ordinal))
                    {
                        violations.Add(Id(entry) + " -> " + sourceId);
                    }
                }
            }

            Assert.Empty(violations, "an effect's sourceId must equal its entry id (Content Manifest sec.4.2)");
        }

        [TestCase("CNT-05", "Every effect is ADD or MULTIPLY with an explicit priority")]
        public static void EffectOperationsAndPriorities()
        {
            List<string> violations = new List<string>();

            foreach (JsonElement entry in AllEntries())
            {
                foreach (JsonElement fx in entry.GetProperty("effects").EnumerateArray())
                {
                    string op = fx.GetProperty("operation").GetString() ?? string.Empty;
                    if (op != "ADD" && op != "MULTIPLY")
                    {
                        violations.Add(Id(entry) + " operation=" + op);
                    }

                    if (!fx.TryGetProperty("priority", out JsonElement priority) || priority.ValueKind != JsonValueKind.Number)
                    {
                        violations.Add(Id(entry) + " missing explicit priority");
                    }
                }
            }

            Assert.Empty(violations, "only ADD and MULTIPLY exist, and every effect carries an explicit priority [Final Lock 4]");
        }

        [TestCase("CNT-06", "No entry authors a numeric purchase cost")]
        public static void NoAuthoredCosts()
        {
            string[] forbidden = { "cost", "costGrowth", "costInsight", "price", "resolvedCost" };

            List<string> violations = AllEntries()
                .Where(e => forbidden.Any(f => e.TryGetProperty(f, out _)))
                .Select(Id)
                .ToList();

            Assert.Empty(violations, "cost is read from the resolving Age, never authored on a card (Core Spec sec.5.10)");
        }

        [TestCase("CNT-07", "Age eligibility is well formed; Keystones start at Age III; each TRAIN is one Age")]
        public static void AgeEligibility()
        {
            List<string> violations = new List<string>();

            foreach (JsonElement entry in AllEntries())
            {
                int min = entry.GetProperty("minAge").GetInt32();
                int max = entry.GetProperty("maxAge").GetInt32();

                if (min < 1 || min > max || max > 4)
                {
                    violations.Add(Id(entry) + " ages " + min + ".." + max);
                }
            }

            foreach (JsonElement keystone in Manifest().GetProperty("keystones").EnumerateArray())
            {
                if (keystone.GetProperty("minAge").GetInt32() != 3)
                {
                    violations.Add(Id(keystone) + " Keystone minAge must be 3");
                }
            }

            foreach (JsonElement train in Manifest().GetProperty("trains").EnumerateArray())
            {
                if (train.GetProperty("minAge").GetInt32() != train.GetProperty("maxAge").GetInt32())
                {
                    violations.Add(Id(train) + " TRAIN must be restricted to exactly one Age");
                }
            }

            Assert.Empty(violations, "Age eligibility (Content Manifest sec.4.2)");
        }

        [TestCase("CNT-08", "Offer sufficiency: every Age can fill a three-card hand without a duplicate id")]
        public static void OfferSufficiency()
        {
            JsonElement root = Manifest();
            List<string> problems = new List<string>();

            for (int age = 1; age <= 4; age++)
            {
                int trains = Eligible(root.GetProperty("trains"), age);
                int builds = Eligible(root.GetProperty("builds"), age);
                int advances = Eligible(root.GetProperty("perks"), age) + Eligible(root.GetProperty("keystones"), age);

                if (trains < 3 || builds < 3 || advances < 3)
                {
                    problems.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "Age {0}: build={1} train={2} advance={3}",
                        age,
                        builds,
                        trains,
                        advances));
                }
            }

            Assert.Empty(problems, "content validation must guarantee at least three eligible ids per non-zero-weight type (Core Spec sec.6.0)");
        }

        [TestCase("CNT-09", "Every entry is prototype-tunable and cites inherited rules")]
        public static void Provenance()
        {
            List<string> violations = AllEntries()
                .Where(e =>
                    (e.GetProperty("sourceClassification").GetString() ?? string.Empty) != "PROTOTYPE-TUNABLE"
                    || e.GetProperty("ruleSources").GetArrayLength() == 0)
                .Select(Id)
                .ToList();

            Assert.Empty(violations, "authority attaches to inherited rules, never to an invented entry (Content Manifest sec.1)");
        }

        [TestCase("CNT-10", "Every entry carries both localization keys")]
        public static void LocalizationKeys()
        {
            List<string> violations = AllEntries()
                .Where(e =>
                {
                    JsonElement keys = e.GetProperty("localizationKeys");
                    return string.IsNullOrEmpty(keys.GetProperty("name").GetString())
                        || string.IsNullOrEmpty(keys.GetProperty("effect").GetString());
                })
                .Select(Id)
                .ToList();

            Assert.Empty(violations, "every entry needs a name key and an effect key");
        }

        [TestCase("CNT-11", "Manifest Age table and offer weights agree with the rules fixtures")]
        public static void ContentAgreesWithRulesFixtures()
        {
            JsonElement content = Manifest();
            JsonElement ages = LoadFixture("age_table.json").GetProperty("ages");
            JsonElement bands = LoadFixture("offer_weights.json").GetProperty("bands");

            JsonElement[] contentAges = content.GetProperty("ageTable").EnumerateArray().ToArray();
            Assert.Equal(4, contentAges.Length, "four Ages");

            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(ages[i].GetProperty("perkCost").GetInt32(), contentAges[i].GetProperty("perkCost").GetInt32(), "perk cost, Age " + (i + 1));
                Assert.Equal(ages[i].GetProperty("unitPower").GetInt32(), contentAges[i].GetProperty("unitPower").GetInt32(), "unit Power, Age " + (i + 1));
                Assert.Equal(ages[i].GetProperty("unitCost").GetInt32(), contentAges[i].GetProperty("unitCost").GetInt32(), "unit cost, Age " + (i + 1));
                Assert.Equal(ages[i].GetProperty("buildCost").GetInt32(), contentAges[i].GetProperty("buildCost").GetInt32(), "build cost, Age " + (i + 1));
            }

            JsonElement[] contentBands = content.GetProperty("offerWeights").EnumerateArray().ToArray();
            Assert.Equal(bands.GetArrayLength(), contentBands.Length, "same number of weight bands");

            for (int i = 0; i < contentBands.Length; i++)
            {
                Assert.Equal(bands[i].GetProperty("build").GetInt32(), contentBands[i].GetProperty("BUILD").GetInt32(), "BUILD weight, band " + i);
                Assert.Equal(bands[i].GetProperty("train").GetInt32(), contentBands[i].GetProperty("TRAIN").GetInt32(), "TRAIN weight, band " + i);
                Assert.Equal(bands[i].GetProperty("advance").GetInt32(), contentBands[i].GetProperty("ADVANCE").GetInt32(), "ADVANCE weight, band " + i);
            }
        }

        [TestCase("CNT-12", "Effect defaults are ADD 100 and MULTIPLY 200")]
        public static void EffectDefaults()
        {
            JsonElement defaults = Manifest().GetProperty("effectDefaults");
            Assert.Equal(100, defaults.GetProperty("ADD").GetInt32(), "default ADD priority [Final Lock 4]");
            Assert.Equal(200, defaults.GetProperty("MULTIPLY").GetInt32(), "default MULTIPLY priority [Final Lock 4]");
        }

        private static int Eligible(JsonElement section, int age) =>
            section.EnumerateArray().Count(e => e.GetProperty("minAge").GetInt32() <= age && age <= e.GetProperty("maxAge").GetInt32());

        private static string Id(JsonElement entry) => entry.GetProperty("id").GetString() ?? string.Empty;

        private static List<JsonElement> AllEntries()
        {
            JsonElement root = Manifest();
            return Sections.SelectMany(s => root.GetProperty(s).EnumerateArray()).ToList();
        }

        private static JsonElement Manifest()
        {
            string path = FixturePaths.ContentManifest;
            Assert.True(File.Exists(path), "the authored content manifest must exist at " + path);
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.Clone();
        }

        private static JsonElement LoadFixture(string name)
        {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(FixturePaths.File(name)));
            return doc.RootElement.Clone();
        }
    }
}
