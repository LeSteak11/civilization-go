using System.Collections.Generic;
using System.Text.Json;
using Epoch.Core.Combat;
using Epoch.Core.Domain;
using Epoch.Core.Effects;
using Epoch.Core.Numerics;
using Epoch.Core.Systems;
using Epoch.Testing;

namespace Epoch.Core.Tests
{
    /// <summary>
    /// Fixed-point arithmetic [Lock 7], the embedded damage table against its fixture,
    /// and the ActiveEffect total order [Final Lock 4].
    /// </summary>
    public static class NumericsAndEffectOrderTests
    {
        [TestCase("FIX-08", "The damage table embedded in the Core equals the shared fixture, entry for entry")]
        public static void EmbeddedDamageTableMatchesFixture()
        {
            // The Core cannot read the filesystem, so the 81 integers are compiled in.
            // That is only safe if something proves the two copies have not drifted.
            using JsonDocument document = JsonDocument.Parse(
                System.IO.File.ReadAllText(FixturePaths.File("damage_table.json")));

            JsonElement entries = document.RootElement.GetProperty("damageByDelta");
            Assert.Equal(DamageTable.EntryCount, entries.GetArrayLength(), "the fixture holds 81 entries");

            int index = 0;
            foreach (JsonElement entry in entries.EnumerateArray())
            {
                int delta = index + DamageTable.DeltaMin;
                Assert.Equal(entry.GetInt32(), DamageTable.ForDelta(delta), "damage at delta " + delta);
                index++;
            }
        }

        [TestCase("FIX-09", "The table is bounded by the spec's derived minimum and maximum")]
        public static void DamageBounds()
        {
            Assert.Equal(DamageTable.MinDamage, DamageTable.ForDelta(-40), "minimum damage is 9 at delta -40");
            Assert.Equal(DamageTable.MaxDamage, DamageTable.ForDelta(40), "maximum damage is 68 at delta +40");
            Assert.Equal(25, DamageTable.ForDelta(0), "delta 0 is 25");

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => DamageTable.ForDelta(41), "an unclamped delta is a programming error, not a game state");
        }

        [TestCase("NUM-01", "Delta rounds half away from zero, symmetrically")]
        public static void RoundHalfAwayFromZero()
        {
            Assert.Equal(6, new FixedValue(560).RoundHalfAwayFromZeroToInt(), "5.60 -> 6");
            Assert.Equal(-6, new FixedValue(-560).RoundHalfAwayFromZeroToInt(), "-5.60 -> -6");

            // The half case is the one that must not silently become banker's rounding.
            Assert.Equal(6, new FixedValue(550).RoundHalfAwayFromZeroToInt(), "5.50 -> 6, away from zero");
            Assert.Equal(-6, new FixedValue(-550).RoundHalfAwayFromZeroToInt(), "-5.50 -> -6, away from zero");
            Assert.Equal(17, new FixedValue(1650).RoundHalfAwayFromZeroToInt(), "16.50 -> 17 (TV-11)");
            Assert.Equal(12, new FixedValue(1240).RoundHalfAwayFromZeroToInt(), "12.40 -> 12 (TV-14)");
            Assert.Equal(0, FixedValue.Zero.RoundHalfAwayFromZeroToInt(), "zero stays zero");
        }

        [TestCase("NUM-02", "Stack and counter multipliers are exact in hundredths")]
        public static void MultipliersAreExact()
        {
            Assert.Equal("12.75", FixedValue.FromInt(17).ScaleByHundredths(75).ToString(), "17 x 0.75");
            Assert.Equal("6.00", FixedValue.FromInt(12).ScaleByHundredths(50).ToString(), "12 x 0.50");
            Assert.Equal("19.60", FixedValue.FromInt(14).ScaleByHundredths(140).ToString(), "14 x 1.40 (TV-09)");
            Assert.Equal("29.40", new FixedValue(2100).ScaleByHundredths(140).ToString(), "21.00 x 1.40 (TV-14)");
        }

        [TestCase("NUM-03", "Culture-free formatting: negatives below one keep their sign")]
        public static void Formatting()
        {
            Assert.Equal("0.00", FixedValue.Zero.ToString(), "zero");
            Assert.Equal("-0.50", new FixedValue(-50).ToString(), "a negative fraction keeps its sign");
            Assert.Equal("-3.25", new FixedValue(-325).ToString(), "negative whole and fraction");
            Assert.Equal("33.50", new FixedValue(3350).ToString(), "GT-17");
        }

        [TestCase("GT-27", "Every ADD resolves before every MULTIPLY, whatever the numeric priorities")]
        public static void AddBeforeMultiply()
        {
            // A MULTIPLY with a LOWER numeric priority than the ADDs must still resolve
            // after them: the partition wins over the number.
            List<ActiveEffect> effects = new List<ActiveEffect>
            {
                Effect("E_MUL", EffectOperation.MULTIPLY, magnitude: 2, priority: 50, EffectSourceType.PERK, "S_C"),
                Effect("E_ADD_100", EffectOperation.ADD, magnitude: 1, priority: 100, EffectSourceType.PERK, "S_B"),
                Effect("E_ADD_90", EffectOperation.ADD, magnitude: 2, priority: 90, EffectSourceType.PERK, "S_A"),
            };

            FixedValue result = EffectResolution.Resolve(FixedValue.FromInt(3), effects);

            // (3 + 2 + 1) x 2 = 12. Had the MULTIPLY gone first it would be 3x2+2+1 = 9.
            Assert.Equal("12.00", result.ToString(), "ADD partition resolves entirely before MULTIPLY");
        }

        [TestCase("GT-27b", "Within a partition the order is priority, then source type, then source id")]
        public static void TotalOrderWithinPartition()
        {
            ActiveEffect lowPriority = Effect("E1", EffectOperation.ADD, 1, 50, EffectSourceType.PERK, "S_Z");
            ActiveEffect highPriority = Effect("E2", EffectOperation.ADD, 1, 100, EffectSourceType.LANE_MODIFIER, "S_A");
            Assert.True(
                EffectResolution.CompareEffects(lowPriority, highPriority) < 0,
                "ascending priority wins before source type");

            ActiveEffect laneMod = Effect("E3", EffectOperation.ADD, 1, 100, EffectSourceType.LANE_MODIFIER, "S_Z");
            ActiveEffect perk = Effect("E4", EffectOperation.ADD, 1, 100, EffectSourceType.PERK, "S_A");
            Assert.True(
                EffectResolution.CompareEffects(laneMod, perk) < 0,
                "at equal priority, LANE_MODIFIER precedes PERK");

            ActiveEffect sourceA = Effect("E5", EffectOperation.ADD, 1, 100, EffectSourceType.PERK, "S_A");
            ActiveEffect sourceB = Effect("E6", EffectOperation.ADD, 1, 100, EffectSourceType.PERK, "S_B");
            Assert.True(
                EffectResolution.CompareEffects(sourceA, sourceB) < 0,
                "at equal priority and type, ordinal source id decides");
        }

        [TestCase("EFF-01", "Effect resolution is order-independent of the input list")]
        public static void ResolutionIsInputOrderIndependent()
        {
            // Feeding the same effects in a different order must give the same answer:
            // the comparator is total, so List.Sort's instability cannot leak through.
            ActiveEffect a = Effect("E_A", EffectOperation.ADD, 2, 100, EffectSourceType.PERK, "S_A");
            ActiveEffect b = Effect("E_B", EffectOperation.ADD, 3, 100, EffectSourceType.PERK, "S_B");
            ActiveEffect c = Effect("E_C", EffectOperation.MULTIPLY, 2, 200, EffectSourceType.PERK, "S_C");

            FixedValue forward = EffectResolution.Resolve(
                FixedValue.FromInt(1), new List<ActiveEffect> { a, b, c });
            FixedValue reverse = EffectResolution.Resolve(
                FixedValue.FromInt(1), new List<ActiveEffect> { c, b, a });

            Assert.Equal(forward.ToString(), reverse.ToString(), "(1+2+3)x2 = 12 either way");
            Assert.Equal("12.00", forward.ToString(), "and the value is the expected one");
        }

        [TestCase("EFF-02", "An empty effect list returns the base value untouched")]
        public static void NoEffectsIsIdentity()
        {
            Assert.Equal(
                "7.00",
                EffectResolution.Resolve(FixedValue.FromInt(7), new List<ActiveEffect>()).ToString(),
                "no effects means no change");
        }

        private static ActiveEffect Effect(
            string effectId,
            EffectOperation operation,
            int magnitude,
            int priority,
            EffectSourceType sourceType,
            string sourceId) =>
            new ActiveEffect(
                new StableId(effectId),
                sourceType,
                new StableId(sourceId),
                EffectTrigger.ON_INCOME,
                EffectScope.SIDE,
                "OWNER",
                operation,
                "growthPerTurn",
                FixedValue.FromInt(magnitude),
                priority,
                0);
    }
}
