using System.Linq;
using Epoch.Core.Domain;
using Epoch.Testing;

namespace Epoch.Core.Tests
{
    /// <summary>
    /// The values in Core Spec sec.2 are LOCKED and carry an explicit instruction not to
    /// rebalance them. These cases exist so that a well-meaning later edit trips a test
    /// instead of silently changing the game.
    /// </summary>
    public static class LockedConstantsTests
    {
        [TestCase("CONST-01", "Run structure constants match Core Spec sec.2.1")]
        public static void RunStructureConstants()
        {
            Assert.Equal(24, RunState.TurnsPerRun, "TURNS_PER_RUN");
            Assert.Equal(4, RunState.AgeCount, "AGE_COUNT");
            Assert.Equal(6, RunState.TurnsPerAge, "TURNS_PER_AGE");
            Assert.Equal(3, RunState.CardsOfferedPerTurn, "CARDS_OFFERED_PER_TURN");
            Assert.Equal(3, RunState.LaneCount, "LANE_COUNT");
            Assert.Equal(5, RunState.TilesPerLane, "TILES_PER_LANE");
            Assert.Equal(3, RunState.MaxUnitsPerLane, "MAX_UNITS_PER_LANE");
            Assert.Equal(3, RunState.ContestedTileIndex, "tile 3 is the only contested tile");
        }

        [TestCase("RS-1", "Age is a pure function of turn for all 24 turns [Lock 1]")]
        public static void AgeIsFixedToTurnRanges()
        {
            int[] expected = Enumerable.Range(1, 24)
                .Select(t => t <= 6 ? 1 : t <= 12 ? 2 : t <= 18 ? 3 : 4)
                .ToArray();

            int[] actual = Enumerable.Range(1, 24).Select(RunState.AgeForTurn).ToArray();

            Assert.SequenceEqual(expected, actual, "Ages are fixed to turns 1-6 / 7-12 / 13-18 / 19-24; ADVANCE never accelerates them");
        }

        [TestCase("RS-1b", "Age boundaries land on the turn the spec names")]
        public static void AgeBoundaries()
        {
            Assert.Equal(1, RunState.AgeForTurn(6), "turn 6 resolves entirely under Age I");
            Assert.Equal(2, RunState.AgeForTurn(7), "turn 7 is the first Age II turn");
            Assert.Equal(3, RunState.AgeForTurn(13), "turn 13 is the first Age III turn");
            Assert.Equal(4, RunState.AgeForTurn(19), "turn 19 is the first Age IV turn");
            Assert.Equal(4, RunState.AgeForTurn(24), "the run ends inside Age IV");
        }
    }
}
