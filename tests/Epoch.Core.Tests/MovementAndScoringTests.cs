using System.Collections.Generic;
using Epoch.Core.Domain;
using Epoch.Core.Systems;
using Epoch.Testing;

namespace Epoch.Core.Tests
{
    /// <summary>
    /// Movement (Core Spec sec.8.2, [Lock 11]) and ownership/scoring (sec.8.4, sec.11.2,
    /// [Lock 4], [Lock 15], [Lock 17 R5]).
    ///
    /// The subtle rule these tests exist to pin is that blocking reads **pre-step**
    /// occupancy. Two units advancing toward each other across an empty tile both enter
    /// it; a unit facing an occupied tile stops. Those two facts come from the same rule
    /// and are easy to break independently.
    /// </summary>
    public static class MovementAndScoringTests
    {
        private static RunState Move(RunState state)
        {
            List<UnitMove> moves = new List<UnitMove>();
            return MovementSystem.Resolve(state, moves);
        }

        private static RunState Score(RunState state)
        {
            List<OwnershipChange> changes = new List<OwnershipChange>();
            return ScoringSystem.Resolve(state, changes);
        }

        [TestCase("GT-03", "A trained unit advances one tile in a plain lane")]
        public static void PlainAdvance()
        {
            RunState state = Scenario.PlainBoard()
                .WithUnits(Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.C, 1));

            Assert.Equal(2, Move(state).FindUnit("P1")!.TileIndex, "tile 1 -> tile 2");
        }

        [TestCase("GT-04", "COAST advances two tiles, and a block on step 1 forfeits step 2")]
        public static void CoastDoubleStepAndBlocking()
        {
            // Empty COAST lane: tile 1 -> tile 3.
            RunState open = Scenario.Board(LaneModifier.RIVER, LaneModifier.COAST, LaneModifier.RIVER)
                .WithUnits(Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.B, 1));
            Assert.Equal(3, Move(open).FindUnit("P1")!.TileIndex, "COAST takes two steps");

            // An enemy on tile 2 blocks step 1, and step 2 is forfeited (EC-12).
            RunState blocked = Scenario.Board(LaneModifier.RIVER, LaneModifier.COAST, LaneModifier.RIVER)
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.B, 1),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 10, LaneId.B, 2));
            Assert.Equal(1, Move(blocked).FindUnit("P1")!.TileIndex, "blocked on step 1, forfeits step 2");
        }

        [TestCase("GT-05", "Both sides enter the same empty tile because blocking reads pre-step occupancy")]
        public static void SimultaneousEntry()
        {
            RunState state = Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 2),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 10, LaneId.A, 4));

            RunState moved = Move(state);
            Assert.Equal(3, moved.FindUnit("P1")!.TileIndex, "player enters tile 3");
            Assert.Equal(3, moved.FindUnit("S1")!.TileIndex, "snapshot enters the same tile 3");

            // That tile is now a combat site; both take 25 and the tile is disputed.
            List<CombatOutcome> outcomes = new List<CombatOutcome>();
            List<StableId> destroyed = new List<StableId>();
            RunState fought = CombatSystem.Resolve(moved, outcomes, destroyed);
            Assert.Equal(75, fought.FindUnit("P1")!.Hp, "both deal 25");
            Assert.Equal(75, fought.FindUnit("S1")!.Hp, "both deal 25");

            RunState scored = Score(fought);
            Assert.Equal(0, scored.Player.Score, "a disputed tile scores for nobody");
            Assert.Equal(0, scored.Snapshot.Score, "a disputed tile scores for nobody");
            Assert.True(scored.Lane(LaneId.A).OwnedBy is null, "disputed means unowned");
        }

        [TestCase("MOVE-01", "Units never swap past each other")]
        public static void NoSwapping()
        {
            RunState state = Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 3),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 10, LaneId.A, 4));

            RunState moved = Move(state);
            Assert.Equal(3, moved.FindUnit("P1")!.TileIndex, "player is blocked by the adjacent enemy");
            Assert.Equal(4, moved.FindUnit("S1")!.TileIndex, "snapshot is blocked symmetrically");
        }

        [TestCase("MOVE-02", "The engaged-unit guard: a unit sharing a tile with an enemy does not move")]
        public static void EngagedUnitsDoNotMove()
        {
            RunState state = Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 3),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 10, LaneId.A, 3));

            RunState moved = Move(state);
            Assert.Equal(3, moved.FindUnit("P1")!.TileIndex, "already engaged, so it holds");
            Assert.Equal(3, moved.FindUnit("S1")!.TileIndex, "already engaged, so it holds");
        }

        [TestCase("GT-13", "Reaching the far end tile is inert and does not end the run")]
        public static void CapitalContactIsInert()
        {
            RunState state = Scenario.PlainBoard()
                .WithUnits(Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 5));

            RunState moved = Move(state);
            Assert.Equal(5, moved.FindUnit("P1")!.TileIndex, "it stops at tile 5 and stays a normal unit");
            Assert.Equal(0, moved.Player.Score, "capital contact grants no score");
            Assert.True(moved.Result is null, "and never terminates the run");
        }

        [TestCase("GT-19", "Score accrues at 1 point per exclusively held contested tile")]
        public static void ScorePerExclusiveTile()
        {
            // Exclusive in A and C; disputed in B.
            RunState state = Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 3),
                    Scenario.Unit("P2", Side.PLAYER, UnitClass.SWORD, 10, LaneId.C, 3),
                    Scenario.Unit("P3", Side.PLAYER, UnitClass.SWORD, 10, LaneId.B, 3),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 10, LaneId.B, 3));

            RunState scored = Score(state);
            Assert.Equal(2, scored.Player.Score, "two exclusive tiles, one point each");
            Assert.Equal(0, scored.Snapshot.Score, "lane B awards nothing to either side");
            Assert.Equal(Side.PLAYER, scored.Lane(LaneId.A).Tile3HolderPrevTurn!.Value, "ownership persists for next turn");
            Assert.True(scored.Lane(LaneId.B).Tile3HolderPrevTurn is null, "a disputed tile persists as null");
        }

        [TestCase("SCORE-01", "An unoccupied contested tile scores nothing and clears ownership")]
        public static void UnoccupiedTileClearsOwnership()
        {
            RunState state = Scenario.PlainBoard().WithPrevHolder(LaneId.A, Side.PLAYER);
            state = state.WithLane(state.Lane(LaneId.A) with { OwnedBy = Side.PLAYER });

            RunState scored = Score(state);
            Assert.Equal(0, scored.Player.Score, "an empty tile awards nothing");
            Assert.True(scored.Lane(LaneId.A).OwnedBy is null, "M32: ownership does not persist from an empty tile");
            Assert.True(scored.Lane(LaneId.A).Tile3HolderPrevTurn is null, "and the persisted value clears too");
        }

        [TestCase("GT-21", "The engagement ends when tile 3 is no longer contested, resetting REACH guards")]
        public static void EngagementResets()
        {
            // Both sides on tile 3: the engagement continues and the id is unchanged.
            RunState contested = Scenario.PlainBoard()
                .WithUnits(
                    Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 3),
                    Scenario.Unit("S1", Side.SNAPSHOT, UnitClass.SWORD, 10, LaneId.A, 3));
            Assert.Equal(0, Score(contested).Lane(LaneId.A).EngagementId, "an ongoing engagement keeps its id");

            // Only one side remains: the engagement ends and the id increments, which
            // clears every REACH guard in that lane at once.
            RunState resolvedLane = Scenario.PlainBoard()
                .WithUnits(Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 3));
            Assert.Equal(1, Score(resolvedLane).Lane(LaneId.A).EngagementId, "the engagement ended");
        }

        [TestCase("GT-19b", "Equal scores at turn 24 are a TIE with no tiebreaker")]
        public static void TieOutcome()
        {
            MatchResult tie = new MatchResult("1.2.0-v1-final", "1.0.1-v1", new SeedCode("T"), 31, 31, 24);
            Assert.Equal(MatchOutcome.TIE, tie.Outcome, "equal scores tie");
            Assert.Equal(0, tie.ScoreDifferential, "differential is zero");

            MatchResult win = new MatchResult("1.2.0-v1-final", "1.0.1-v1", new SeedCode("T"), 41, 38, 24);
            Assert.Equal(MatchOutcome.VICTORY, win.Outcome, "higher score wins");

            MatchResult loss = new MatchResult("1.2.0-v1-final", "1.0.1-v1", new SeedCode("T"), 38, 41, 24);
            Assert.Equal(MatchOutcome.DEFEAT, loss.Outcome, "lower score loses");
        }
    }
}
