using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Epoch.Testing;

namespace Epoch.Oracle.Tests
{
    /// <summary>
    /// At M0 there are no comparison fixtures yet, so what is tested is the thing that
    /// makes future fixtures trustworthy: that the oracle they will be generated from is
    /// pinned by content hash and has not drifted. The comparison suite itself arrives
    /// with M1.
    /// </summary>
    public static class OracleFixtureContractTests
    {
        [TestCase("ORC-01", "The oracle pin declares the rules and content versions it speaks for")]
        public static void PinDeclaresVersions()
        {
            JsonElement pin = Pin();
            Assert.Equal("1.2.0-v1-final", pin.GetProperty("rulesVersion").GetString(), "pinned rules version");
            Assert.Equal("1.0.1-v1", pin.GetProperty("contentVersion").GetString(), "pinned content version");
            Assert.Equal(64, (pin.GetProperty("oracleSha256").GetString() ?? string.Empty).Length, "a full SHA-256 hex digest");
        }

        [TestCase("ORC-02", "The pinned oracle file on disk still hashes to the pinned digest")]
        public static void OracleHasNotDrifted()
        {
            JsonElement pin = Pin();
            string relative = pin.GetProperty("oracleFile").GetString() ?? string.Empty;
            string path = Path.Combine(FixturePaths.RepositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar));

            Assert.True(File.Exists(path), "the pinned oracle must exist at " + path);

            using FileStream stream = File.OpenRead(path);
            string actual = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();

            Assert.Equal(
                pin.GetProperty("oracleSha256").GetString(),
                actual,
                "the oracle changed without the pin being updated; regenerated fixtures would compare against a different simulator");
        }

        [TestCase("ORC-03", "No comparison fixture has been generated yet, and the pin says so")]
        public static void FixturesAreDeclaredEmptyAtM0()
        {
            string[] generated = Directory
                .GetFiles(Path.Combine(FixturePaths.Root, "oracle"), "*.json")
                .Where(f => !Path.GetFileName(f).Equals("oracle-pin.json", StringComparison.Ordinal))
                .ToArray();

            Assert.Equal(
                generated.Length == 0,
                Pin().GetProperty("fixturesGeneratedAt").ValueKind == JsonValueKind.Null,
                "fixturesGeneratedAt must be null exactly while no fixtures exist; this case is replaced at M1");
        }

        private static JsonElement Pin()
        {
            string path = Path.Combine(FixturePaths.Root, "oracle", "oracle-pin.json");
            Assert.True(File.Exists(path), "oracle-pin.json must exist at " + path);
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.Clone();
        }
    }
}
