using System.Collections.Generic;
using System.Linq;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Systems;
using Epoch.Testing;

namespace Epoch.Core.Tests
{
    /// <summary>
    /// Regression cover for the five effects M2 found dead, plus owner-confirmed
    /// decision (L).
    ///
    /// M1 gathered effects from perks only with a side-wide scope context, so every
    /// lane-scoped income perk and every structure-borne effect matched nothing: they
    /// loaded, read as if they did something, and did nothing. These tests pin the
    /// repaired behaviour so it cannot regress silently again.
    /// </summary>
    public static class RepairedEffectTests
    {
        private static ValidatedContentSet Content => ContentFixtureLoader.Load();

        private static CardDefinition Card(string id) => Content.ById(new StableId(id));

        /// <summary>Lane A = RIVER, B = HIGHLAND, C = COAST in the default scenario board.</summary>
        private static RunState BoardWithStructureIn(LaneId lane, ResourceType yieldType = ResourceType.GROWTH) =>
            Scenario.Board()
                .WithStructures(Side.PLAYER, Scenario.Structure("S1", Side.PLAYER, yieldType, 1, lane));

        private static int Growth(RunState state) =>
            IncomeSystem.Compute(state, Side.PLAYER, ResourceType.GROWTH).Total;

        private static int Insight(RunState state) =>
            IncomeSystem.Compute(state, Side.PLAYER, ResourceType.INSIGHT).Total;

        [TestCase("M2-R1", "Lane-scoped income perks apply, and only when a structure is in that lane")]
        public static void LaneScopedIncomePerks()
        {
            // River Markets: +1 Growth if the owner has a structure in the RIVER lane.
            RunState riverNoStructure = Scenario.Board().WithPerks(Side.PLAYER, Card("PERK_ECO_RIVER_GROWTH"));
            Assert.Equal(3, Growth(riverNoStructure), "no structure anywhere, so no RIVER bonus");

            RunState riverWrongLane = BoardWithStructureIn(LaneId.B).WithPerks(Side.PLAYER, Card("PERK_ECO_RIVER_GROWTH"));
            Assert.Equal(4, Growth(riverWrongLane), "structure is in HIGHLAND, so 3 base + 1 structure only");

            RunState river = BoardWithStructureIn(LaneId.A).WithPerks(Side.PLAYER, Card("PERK_ECO_RIVER_GROWTH"));
            Assert.Equal(5, Growth(river), "3 base + 1 structure + 1 River Markets");

            // Coastal Trade: +1 Growth from a structure in the COAST lane.
            RunState coast = BoardWithStructureIn(LaneId.C).WithPerks(Side.PLAYER, Card("PERK_UTIL_COAST_GROWTH"));
            Assert.Equal(5, Growth(coast), "3 base + 1 structure + 1 Coastal Trade");

            // Hill Observatories: +1 Insight from a structure in the HIGHLAND lane.
            RunState highland = BoardWithStructureIn(LaneId.B).WithPerks(Side.PLAYER, Card("PERK_UTIL_HIGHLAND_INSIGHT"));
            Assert.Equal(3, Insight(highland), "2 base + 1 Hill Observatories");
            Assert.Equal(4, Growth(highland), "and Growth is untouched: 3 base + 1 structure");
        }

        [TestCase("M2-R2", "River Academies converts 1 Growth income into 1 Insight")]
        public static void RiverAcademiesConversion()
        {
            RunState state = BoardWithStructureIn(LaneId.A).WithPerks(Side.PLAYER, Card("PERK_CONV_RIVER_INSIGHT"));

            // Both halves of the conversion are separate effects on one card; M1 ran neither.
            Assert.Equal(3, Growth(state), "3 base + 1 structure - 1 converted");
            Assert.Equal(3, Insight(state), "2 base + 1 converted");
        }

        [TestCase("M2-R3", "Garrison Post grants Power in its own lane, and only there")]
        public static void GarrisonPostStructurePower()
        {
            CardDefinition garrison = Card("BUILD_GARRISON_POST");

            RunState state = Scenario.PlainBoard()
                .WithStructures(
                    Side.PLAYER,
                    Scenario.Structure(
                        "S1", Side.PLAYER, ResourceType.GROWTH, 1, LaneId.A, effects: garrison.Effects.ToArray()))
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 3),
                    Scenario.Unit("S1U", Side.SNAPSHOT, UnitClass.SWORD, 10, LaneId.A, 3),
                    Scenario.Unit("P2", Side.PLAYER, UnitClass.SWORD, 10, LaneId.B, 3),
                    Scenario.Unit("S2U", Side.SNAPSHOT, UnitClass.SWORD, 10, LaneId.B, 3));

            List<CombatOutcome> outcomes = new List<CombatOutcome>();
            List<StableId> destroyed = new List<StableId>();
            CombatSystem.Resolve(state, outcomes, destroyed);

            CombatOutcome laneA = outcomes.First(o => o.Lane == LaneId.A);
            CombatOutcome laneB = outcomes.First(o => o.Lane == LaneId.B);

            Assert.Equal("11.00", laneA.PlayerPower.ToString(), "the structure's lane gets +1 Power");
            Assert.Equal("10.00", laneB.PlayerPower.ToString(), "another lane does not");
            Assert.Equal(1, laneA.PlayerDelta, "and that reaches the damage lookup");
        }

        [TestCase("M2-R4", "Decision L: a UNIT_CLASS Power effect changes stack rank and the dominant class")]
        public static void UnitClassEffectChangesStackRankAndDominantClass()
        {
            // SWORD 14 outranks SPEAR 13, so without the perk SWORD leads the stack and is
            // dominant: 14 x 1.00 + 13 x 0.75 = 23.75.
            RunState without = Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 14, LaneId.A, 3),
                    Scenario.Unit("P2", Side.PLAYER, UnitClass.SPEAR, 13, LaneId.A, 3),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.HORSE, 10, LaneId.A, 3));

            List<CombatOutcome> before = new List<CombatOutcome>();
            CombatSystem.Resolve(without, before, new List<StableId>());
            Assert.Equal("23.75", before[0].PlayerPower.ToString(), "14 + 9.75");
            Assert.Equal(UnitClass.SWORD, before[0].PlayerDominantClass!.Value, "SWORD contributes the most");

            // Combat Drill gives SPEAR +2 *before* stacking, so SPEAR becomes 15 and takes
            // rank 0: 15 x 1.00 + 14 x 0.75 = 25.50, and the dominant class flips.
            RunState with = without.WithPerks(Side.PLAYER, Card("PERK_MIL_SPEAR"));

            List<CombatOutcome> after = new List<CombatOutcome>();
            CombatSystem.Resolve(with, after, new List<StableId>());

            Assert.Equal(
                UnitClass.SPEAR,
                after[0].PlayerDominantClass!.Value,
                "decision L: applying before stacking changes which class is dominant at C3");
            Assert.False(before[0].PlayerCountered, "SWORD does not counter HORSE");
            Assert.True(after[0].PlayerCountered, "SPEAR does counter HORSE, so the flip also flips the matchup");

            // 15 x 1.00 + 14 x 0.75 = 25.50 post-stacking, then x1.40 for the counter.
            Assert.Equal("35.70", after[0].PlayerPower.ToString(), "the bonus changed the stack ranking, then the counter applied");
        }
    }
}
