using System.Collections.Generic;
using Epoch.Core.Domain;
using Epoch.Core.Systems;
using Epoch.Testing;

namespace Epoch.Core.Tests
{
    /// <summary>
    /// The fourteen combat test vectors of Core Spec sec.9.4, plus the golden scenarios
    /// that depend on them.
    ///
    /// Every expected damage number below is a table read, never a computation: Delta is
    /// clamped to [-40,+40] and rounded half away from zero **before** the lookup, which
    /// is what removes float from the authoritative path entirely [Lock 7].
    /// </summary>
    public static class CombatVectorTests
    {
        /// <summary>Resolve a single clash and return the outcome for the one combat site.</summary>
        private static CombatOutcome Clash(RunState state)
        {
            List<CombatOutcome> outcomes = new List<CombatOutcome>();
            List<StableId> destroyed = new List<StableId>();
            CombatSystem.Resolve(state, outcomes, destroyed);
            Assert.Equal(1, outcomes.Count, "exactly one combat site was expected");
            return outcomes[0];
        }

        private static RunState Duel(int playerPower, int snapshotPower, UnitClass playerClass = UnitClass.SWORD, UnitClass snapshotClass = UnitClass.SWORD) =>
            Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, playerClass, playerPower, LaneId.A, 3),
                    Scenario.Unit("S1", Side.SNAPSHOT, snapshotClass, snapshotPower, LaneId.A, 3));

        private static void AssertDamage(CombatOutcome outcome, int expectedDelta, int playerDeals, int snapshotDeals, string vector)
        {
            Assert.Equal(expectedDelta, outcome.PlayerDelta, vector + " delta");
            Assert.Equal(playerDeals, outcome.PlayerDamageDealt, vector + " player damage");
            Assert.Equal(snapshotDeals, outcome.SnapshotDamageDealt, vector + " snapshot damage");
        }

        [TestCase("TV-01", "Equal power: both deal 25, four clashes to kill")]
        public static void EqualPower() =>
            AssertDamage(Clash(Duel(10, 10)), 0, 25, 25, "TV-01");

        [TestCase("TV-02", "Delta +7 (one Age): exchange 1.42:1")]
        public static void DeltaSeven() =>
            AssertDamage(Clash(Duel(17, 10)), 7, 30, 21, "TV-02");

        [TestCase("TV-03", "Delta +8 (commander budget): exchange 1.49:1")]
        public static void DeltaEight() =>
            AssertDamage(Clash(Duel(18, 10)), 8, 31, 20, "TV-03");

        [TestCase("TV-04", "Delta +15: exchange 2.12:1")]
        public static void DeltaFifteen() =>
            AssertDamage(Clash(Duel(25, 10)), 15, 36, 17, "TV-04");

        [TestCase("TV-05", "Delta +25: exchange 3.49:1")]
        public static void DeltaTwentyFive() =>
            AssertDamage(Clash(Duel(35, 10)), 25, 47, 13, "TV-05");

        [TestCase("TV-06", "Delta +40 at the clamp: exchange 7.39:1")]
        public static void DeltaForty() =>
            AssertDamage(Clash(Duel(50, 10)), 40, 68, 9, "TV-06");

        [TestCase("TV-07", "Delta +60 beyond the clamp is byte-identical to +40")]
        public static void ClampProof() =>
            AssertDamage(Clash(Duel(70, 10)), 40, 68, 9, "TV-07");

        [TestCase("TV-08", "Delta -40 reversed proves symmetry")]
        public static void ClampSymmetry() =>
            AssertDamage(Clash(Duel(10, 50)), -40, 9, 68, "TV-08");

        [TestCase("TV-09", "Counter is applied to the dominant class before the curve")]
        public static void CounterBeforeCurve()
        {
            // SWORD 14 x1.40 = 19.60 against SPEAR 14.00 -> delta +5.6 -> +6.
            CombatOutcome outcome = Clash(Duel(14, 14, UnitClass.SWORD, UnitClass.SPEAR));
            Assert.True(outcome.PlayerCountered, "SWORD beats SPEAR");
            Assert.False(outcome.SnapshotCountered, "the counter is one-directional");
            AssertDamage(outcome, 6, 29, 22, "TV-09");
        }

        [TestCase("TV-10", "HIGHLAND +3 goes to the previous turn's exclusive holder")]
        public static void HighlandUsesPreviousHolder()
        {
            RunState state = Scenario.Board(LaneModifier.HIGHLAND, LaneModifier.RIVER, LaneModifier.RIVER)
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 14, LaneId.A, 3),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 14, LaneId.A, 3))
                .WithPrevHolder(LaneId.A, Side.SNAPSHOT);

            AssertDamage(Clash(state), -3, 23, 27, "TV-10");
        }

        [TestCase("TV-10b", "HIGHLAND after a disputed turn gives neither side the bonus")]
        public static void HighlandNullHolder()
        {
            RunState state = Scenario.Board(LaneModifier.HIGHLAND, LaneModifier.RIVER, LaneModifier.RIVER)
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 14, LaneId.A, 3),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 14, LaneId.A, 3))
                .WithPrevHolder(LaneId.A, null);

            AssertDamage(Clash(state), 0, 25, 25, "TV-10b");
        }

        [TestCase("GT-20b", "HIGHLAND is tile-3 only and never applies elsewhere")]
        public static void HighlandIsTileThreeOnly()
        {
            RunState state = Scenario.Board(LaneModifier.HIGHLAND, LaneModifier.RIVER, LaneModifier.RIVER)
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 14, LaneId.A, 2),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 14, LaneId.A, 2))
                .WithPrevHolder(LaneId.A, Side.SNAPSHOT);

            AssertDamage(Clash(state), 0, 25, 25, "GT-20b: no +3 outside tile 3");
        }

        [TestCase("TV-11", "Multi-unit stacking: [17,14,12] resolves to 33.50")]
        public static void StackMultipliers()
        {
            RunState state = Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 17, LaneId.A, 3),
                    Scenario.Unit("P2", Side.PLAYER, UnitClass.SWORD, 14, LaneId.A, 3),
                    Scenario.Unit("P3", Side.PLAYER, UnitClass.SWORD, 12, LaneId.A, 3),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 17, LaneId.A, 3));

            CombatOutcome outcome = Clash(state);
            Assert.Equal("33.50", outcome.PlayerPower.ToString(), "GT-17: 17 + 10.5 + 6");
            AssertDamage(outcome, 17, 38, 16, "TV-11");
        }

        [TestCase("TV-12", "A full 3-stack mirror is symmetric")]
        public static void StackingIsSymmetric()
        {
            RunState state = Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 3),
                    Scenario.Unit("P2", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 3),
                    Scenario.Unit("P3", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 3),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 10, LaneId.A, 3),
                    Scenario.Unit("S2", Side.SNAPSHOT, UnitClass.SWORD, 10, LaneId.A, 3),
                    Scenario.Unit("S3", Side.SNAPSHOT, UnitClass.SWORD, 10, LaneId.A, 3));

            CombatOutcome outcome = Clash(state);
            Assert.Equal("22.50", outcome.PlayerPower.ToString(), "22.50 each side");
            AssertDamage(outcome, 0, 25, 25, "TV-12");
        }

        [TestCase("TV-13", "REACH: the tile-2 supporter contributes and takes no return damage on the first clash")]
        public static void ReachFirstClash()
        {
            RunState state = Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 14, LaneId.A, 3),
                    Scenario.Unit("P2", Side.PLAYER, UnitClass.SPEAR, 12, LaneId.A, 2, reach: true),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 14, LaneId.A, 3));

            CombatOutcome outcome = Clash(state);
            Assert.Equal("23.00", outcome.PlayerPower.ToString(), "14 + 9 with stack multipliers");
            AssertDamage(outcome, 9, 31, 20, "TV-13");

            List<CombatOutcome> outcomes = new List<CombatOutcome>();
            List<StableId> destroyed = new List<StableId>();
            RunState after = CombatSystem.Resolve(state, outcomes, destroyed);

            // All 20 damage lands on the frontline SWORD; the REACH SPEAR is exempt.
            Assert.Equal(80, after.FindUnit("P1")!.Hp, "frontline takes the full 20");
            Assert.Equal(100, after.FindUnit("P2")!.Hp, "the protected REACH unit takes none");
            Assert.Equal(0, after.FindUnit("P2")!.ReachGuardUsedInEngagement!.Value, "guard is consumed for this engagement");
        }

        [TestCase("TV-13b", "REACH second clash in the same engagement: the guard is already spent")]
        public static void ReachSecondClash()
        {
            // Engagement 0, guard already consumed in engagement 0.
            RunState state = Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 14, LaneId.A, 3, hp: 10),
                    Scenario.Unit("P2", Side.PLAYER, UnitClass.SPEAR, 12, LaneId.A, 2, reach: true, guardUsedInEngagement: 0),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 14, LaneId.A, 3));

            CombatOutcome outcome = Clash(state);
            AssertDamage(outcome, 9, 31, 20, "TV-13b");

            List<CombatOutcome> outcomes = new List<CombatOutcome>();
            List<StableId> destroyed = new List<StableId>();
            RunState after = CombatSystem.Resolve(state, outcomes, destroyed);

            // 20 damage: 10 destroys the frontline, and the overflow now reaches the
            // REACH unit because its guard was already spent in this engagement.
            Assert.True(after.FindUnit("P1") is null, "the 10-HP frontline is destroyed");
            Assert.Equal(90, after.FindUnit("P2")!.Hp, "overflow of 10 reaches the now-eligible REACH unit");
        }

        [TestCase("TV-13c", "REACH with no friendly frontline contributes nothing and is not protected")]
        public static void ReachWithoutFrontline()
        {
            // The REACH unit is alone on tile 2; the enemy is on tile 3. There is no
            // tile-3 clash for it to join, so there is no combat site at all.
            RunState state = Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P2", Side.PLAYER, UnitClass.SPEAR, 12, LaneId.A, 2, reach: true),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 14, LaneId.A, 3));

            List<CombatOutcome> outcomes = new List<CombatOutcome>();
            List<StableId> destroyed = new List<StableId>();
            RunState after = CombatSystem.Resolve(state, outcomes, destroyed);

            Assert.Empty(outcomes, "TV-13c: no clash occurs, so REACH contributes nothing");
            Assert.Equal(100, after.FindUnit("P2")!.Hp, "and it is not protected because it never fought");
        }

        [TestCase("TV-14", "Counter and HIGHLAND stack in the locked resolution order")]
        public static void CounterAndHighland()
        {
            // SPEAR [12,12] -> 21.00 x1.40 = 29.40 against HORSE 14 +3 HIGHLAND = 17.00.
            RunState state = Scenario.Board(LaneModifier.HIGHLAND, LaneModifier.RIVER, LaneModifier.RIVER)
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SPEAR, 12, LaneId.A, 3),
                    Scenario.Unit("P2", Side.PLAYER, UnitClass.SPEAR, 12, LaneId.A, 3),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.HORSE, 14, LaneId.A, 3))
                .WithPrevHolder(LaneId.A, Side.SNAPSHOT);

            CombatOutcome outcome = Clash(state);
            Assert.Equal("29.40", outcome.PlayerPower.ToString(), "stack then counter");
            Assert.Equal("17.00", outcome.SnapshotPower.ToString(), "14 + 3 HIGHLAND");
            AssertDamage(outcome, 12, 34, 19, "TV-14");
        }

        [TestCase("GT-18b", "Dominant class in a mixed stack is the largest post-stacking contribution")]
        public static void DominantClassInMixedStack()
        {
            // A: SWORD 17 + SPEAR 14 + SPEAR 12 -> SWORD 17.0 vs SPEAR 10.5 + 6.0 = 16.5.
            RunState state = Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 17, LaneId.A, 3),
                    Scenario.Unit("P2", Side.PLAYER, UnitClass.SPEAR, 14, LaneId.A, 3),
                    Scenario.Unit("P3", Side.PLAYER, UnitClass.SPEAR, 12, LaneId.A, 3),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SPEAR, 17, LaneId.A, 3));

            CombatOutcome outcome = Clash(state);
            Assert.Equal(UnitClass.SWORD, outcome.PlayerDominantClass!.Value, "SWORD contributes 17.0 against SPEAR's 16.5");
            Assert.True(outcome.PlayerCountered, "SWORD beats SPEAR, so the counter applies once");
        }

        [TestCase("GT-25", "Damage overflows front-to-back and stops when it is spent")]
        public static void DamageOverflow()
        {
            // HP 10/15/100 in M22 stack order, and powers chosen so that the incoming
            // damage is exactly the 40 the scenario names: the player stack is
            // 30 + 20x0.75 + 10x0.50 = 50.00, so a lone 69.00 gives delta +19 -> 40.
            RunState state = Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 30, LaneId.A, 3, hp: 10),
                    Scenario.Unit("P2", Side.PLAYER, UnitClass.SWORD, 20, LaneId.A, 3, hp: 15),
                    Scenario.Unit("P3", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 3, hp: 100),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 69, LaneId.A, 3));

            List<CombatOutcome> outcomes = new List<CombatOutcome>();
            List<StableId> destroyed = new List<StableId>();
            RunState after = CombatSystem.Resolve(state, outcomes, destroyed);

            Assert.Equal(40, outcomes[0].SnapshotDamageDealt, "incoming damage is 40");
            Assert.True(after.FindUnit("P1") is null, "first unit destroyed");
            Assert.True(after.FindUnit("P2") is null, "second unit destroyed");
            Assert.Equal(85, after.FindUnit("P3")!.Hp, "the third absorbs the remaining 15");
        }

        [TestCase("GT-06", "Mutual annihilation: damage is simultaneous, so both stacks die")]
        public static void MutualAnnihilation()
        {
            RunState state = Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 3, hp: 20),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 10, LaneId.A, 3, hp: 20));

            List<CombatOutcome> outcomes = new List<CombatOutcome>();
            List<StableId> destroyed = new List<StableId>();
            RunState after = CombatSystem.Resolve(state, outcomes, destroyed);

            Assert.Equal(2, destroyed.Count, "both are destroyed from the same pre-damage state");
            Assert.Empty(after.Player.Units, "player stack is gone");
            Assert.Empty(after.Snapshot.Units, "snapshot stack is gone");
        }

        [TestCase("COMBAT-01", "Combat resolves at every co-occupied tile, not only tile 3")]
        public static void AllTileCombat()
        {
            RunState state = Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 2),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 10, LaneId.A, 2),
                    Scenario.Unit("P2", Side.PLAYER, UnitClass.SWORD, 10, LaneId.B, 4),
                    Scenario.Unit("S2", Side.SNAPSHOT, UnitClass.SWORD, 10, LaneId.B, 4),
                    Scenario.Unit("P3", Side.PLAYER, UnitClass.SWORD, 10, LaneId.C, 3),
                    Scenario.Unit("S3", Side.SNAPSHOT, UnitClass.SWORD, 10, LaneId.C, 3));

            List<CombatOutcome> outcomes = new List<CombatOutcome>();
            List<StableId> destroyed = new List<StableId>();
            CombatSystem.Resolve(state, outcomes, destroyed);

            Assert.Equal(3, outcomes.Count, "one site per co-occupied tile across all lanes");
        }

        [TestCase("COMBAT-02", "A tile holding only one side is not a combat site")]
        public static void NoCombatWithoutBothSides()
        {
            RunState state = Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 3),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 10, LaneId.A, 4));

            List<CombatOutcome> outcomes = new List<CombatOutcome>();
            List<StableId> destroyed = new List<StableId>();
            CombatSystem.Resolve(state, outcomes, destroyed);

            Assert.Empty(outcomes, "adjacent is not co-occupied");
        }
    }
}
