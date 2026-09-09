using System.Collections.Generic;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.StateMachine;
using Epoch.Testing;

namespace Epoch.Core.Tests
{
    /// <summary>
    /// The canonical turn order end to end (Core Spec sec.4.1), driven through
    /// <see cref="MatchResolver"/> rather than through the individual systems.
    ///
    /// These are the tests that would catch an ordering regression: each system can be
    /// individually correct while the pipeline runs them in the wrong sequence.
    /// </summary>
    public static class TurnPipelineTests
    {
        private static ValidatedContentSet Content => ContentFixtureLoader.Load();

        /// <summary>A hand of exactly the cards a scenario needs, bypassing the RNG.</summary>
        private static CardOfferSet Hand(int turn, params CardDefinition[] cards)
        {
            List<CardOffer> offers = new List<CardOffer>();
            for (int i = 0; i < cards.Length; i++)
            {
                offers.Add(new CardOffer(i, cards[i].CardId));
            }

            return new CardOfferSet(turn, offers);
        }

        private static CardDefinition Card(CardType type, int age) => Content.EligiblePool(type, age)[0];

        private static CardDefinition GrowthBuild()
        {
            foreach (CardDefinition card in Content.Cards)
            {
                if (card.CardType == CardType.BUILD && card.YieldType == ResourceType.GROWTH)
                {
                    return card;
                }
            }

            throw new AssertionException("The authored pool must contain a Growth BUILD.");
        }

        private static RunState Resolve(RunState state, CardOfferSet offers, Selection player, Selection snapshot)
        {
            ResolvedTurn resolved = MatchResolver.Resolve(state, offers, player, snapshot, Content, 0);
            Assert.True(resolved.IsAccepted, "the turn was rejected: " + resolved.Error);
            return resolved.NextState!;
        }

        [TestCase("GT-02c", "Income ticks AFTER the card resolves, so a new structure pays out at once")]
        public static void IncomeFollowsTheCard()
        {
            // Q1: MR sec.1.2 orders "the pick" then "yields tick".
            CardDefinition build = GrowthBuild();
            RunState state = Scenario.Board(LaneModifier.HIGHLAND, LaneModifier.COAST, LaneModifier.HIGHLAND)
                .WithResources(Side.PLAYER, 8, 2)
                .WithResources(Side.SNAPSHOT, 0, 0);

            CardOfferSet offers = Hand(1, build);
            RunState after = Resolve(state, offers, new Selection(0, LaneId.A), Selection.Pass);

            // Paid 8 -> 0, then income 3 base + 1 tier-1 structure = 4.
            Assert.Equal(4, after.Player.Growth, "GT-02: pay 8, then earn 3 + 1 in the same turn");
            Assert.Equal(4, after.Player.Insight, "2 opening + 2 base income");
            Assert.Equal(1, after.Player.Structures.Count, "the structure exists");
            Assert.Equal(1, after.Player.Structures[0].Tier, "tier equals the resolving Age");
        }

        [TestCase("GT-11", "RIVER raises a Growth structure's yield by 1 and lowers its recorded payback")]
        public static void RiverBonusAndPaybackTelemetry()
        {
            CardDefinition build = GrowthBuild();

            // Lane A is RIVER here; lane B is not.
            RunState state = Scenario.Board(LaneModifier.RIVER, LaneModifier.HIGHLAND, LaneModifier.COAST, turn: 14)
                .WithResources(Side.PLAYER, 100, 100)
                .WithResources(Side.SNAPSHOT, 0, 0);

            RunState plain = Resolve(state, Hand(14, build), new Selection(0, LaneId.B), Selection.Pass);
            RunState river = Resolve(state, Hand(14, build), new Selection(0, LaneId.A), Selection.Pass);

            // Age III: cost 16, tier yield +3.
            Assert.Equal(3, plain.Player.Structures[0].YieldAmount, "plain lane keeps the tier yield");
            Assert.Equal(4, river.Player.Structures[0].YieldAmount, "RIVER adds +1 Growth");

            // paybackTurns is telemetry only and never gates anything (X5).
            Assert.Equal(533, plain.Player.Structures[0].PaybackTurnsHundredths, "16/3 = 5.33");
            Assert.Equal(400, river.Player.Structures[0].PaybackTurnsHundredths, "16/4 = 4.00");
        }

        [TestCase("GT-02b", "Two structures may share a lane: there is no cap and they occupy no tile")]
        public static void StructuresStackInALane()
        {
            CardDefinition build = GrowthBuild();
            RunState state = Scenario.Board()
                .WithResources(Side.PLAYER, 100, 100)
                .WithResources(Side.SNAPSHOT, 0, 0);

            RunState first = Resolve(state, Hand(1, build), new Selection(0, LaneId.A), Selection.Pass);
            RunState second = Resolve(first, Hand(2, build), new Selection(0, LaneId.A), Selection.Pass);

            Assert.Equal(2, second.Player.Structures.Count, "EC-29: no cap per lane or per run");
            Assert.Equal(LaneId.A, second.Player.Structures[1].LaneId, "both are lane-scoped to A");
        }

        [TestCase("GT-10b", "ADVANCE grants a perk and never accelerates the Age")]
        public static void AdvanceDoesNotAccelerateTheAge()
        {
            RunState state = Scenario.Board(turn: 3)
                .WithResources(Side.PLAYER, 0, 100)
                .WithResources(Side.SNAPSHOT, 0, 100);

            // Take ADVANCE on turns 3, 4 and 5 with three distinct perks.
            List<CardDefinition> perks = Content.EligiblePool(CardType.ADVANCE, 1);
            for (int i = 0; i < 3; i++)
            {
                // Both sides take it: perks are unique per side, so the same card is
                // legal for each. A PASS would stop being forced once income accrues.
                state = Resolve(state, Hand(state.Turn, perks[i]), new Selection(0, null), new Selection(0, null));
            }

            Assert.Equal(6, state.Turn, "three turns have passed");
            Assert.Equal(1, state.Age, "A6 [Lock 2]: still Age I on turn 6");
            Assert.Equal(3, state.Player.Perks.Count, "three perks were granted");

            // The Age changes only at step 15 of turn 6, effective from turn 7.
            RunState afterSix = Resolve(state, Hand(6, perks[3]), new Selection(0, null), new Selection(0, null));
            Assert.Equal(7, afterSix.Turn, "turn 6 completed");
            Assert.Equal(2, afterSix.Age, "turn 7 is the first Age II turn");
        }

        [TestCase("GT-10c", "Unit Power and cost are those of the Age the unit was trained in")]
        public static void UnitPowerIsFixedAtTraining()
        {
            CardDefinition ageOneTrain = Card(CardType.TRAIN, 1);
            RunState state = Scenario.Board(turn: 6)
                .WithResources(Side.PLAYER, 100, 0)
                .WithResources(Side.SNAPSHOT, 0, 0);

            RunState after = Resolve(state, Hand(6, ageOneTrain), new Selection(0, LaneId.A), Selection.Pass);

            // 100 - 6 (Age I unit cost, not Age II's 8) + 3 base income = 97.
            Assert.Equal(97, after.Player.Growth, "turn 6 still charges the Age I cost of 6");
            UnitInstance unit = after.Player.Units[0];
            Assert.Equal(10, unit.BasePower, "trained in Age I, so Power 10 forever (Invariant UI-1)");
            Assert.Equal(UnitInstance.MaxHp, unit.Hp, "T10: enters at full HP");
            Assert.Equal(6, unit.TrainedOnTurn, "it records the turn it was trained");

            // T4 puts it on tile 1, but T9 makes it move the same turn, so by the end of
            // the turn it is on tile 2. PIPE-01 pins that step explicitly.
            Assert.Equal(2, unit.TileIndex, "entered at tile 1 and advanced once this turn");
        }

        [TestCase("GT-15b", "A unit trained on turn 24 moves once and the run still ends normally")]
        public static void TurnTwentyFourTraining()
        {
            CardDefinition train = Card(CardType.TRAIN, 4);
            RunState state = Scenario.Board(LaneModifier.HIGHLAND, LaneModifier.RIVER, LaneModifier.HIGHLAND, turn: 24)
                .WithResources(Side.PLAYER, 100, 0)
                .WithResources(Side.SNAPSHOT, 0, 0);

            RunState after = Resolve(state, Hand(24, train), new Selection(0, LaneId.A), Selection.Pass);

            Assert.Equal(2, after.Player.Units[0].TileIndex, "entered at tile 1 and moved once, so it never reaches tile 3");
            Assert.True(after.Result is not null, "the run completes at turn 24");
            Assert.Equal(24, after.Result!.FinalTurn, "MC1: the result is taken at turn 24");
        }

        [TestCase("PIPE-01", "T9 [Lock 8]: a unit trained this turn moves and fights the same turn")]
        public static void NewUnitActsImmediately()
        {
            CardDefinition train = Card(CardType.TRAIN, 1);
            RunState state = Scenario.Board(LaneModifier.HIGHLAND, LaneModifier.RIVER, LaneModifier.HIGHLAND)
                .WithResources(Side.PLAYER, 100, 0)
                .WithResources(Side.SNAPSHOT, 0, 0);

            RunState after = Resolve(state, Hand(1, train), new Selection(0, LaneId.A), Selection.Pass);
            Assert.Equal(2, after.Player.Units[0].TileIndex, "it moved on the turn it was trained");
        }

        [TestCase("PIPE-02", "A rejected selection mutates nothing (Invariant SM-2)")]
        public static void RejectionIsPure()
        {
            CardDefinition build = GrowthBuild();
            RunState state = Scenario.Board().WithResources(Side.PLAYER, 0, 0);

            // 0 Growth cannot afford an Age I BUILD, so G3 rejects it.
            ResolvedTurn resolved = MatchResolver.Resolve(
                state, Hand(1, build), new Selection(0, LaneId.A), Selection.Pass, Content, 0);

            Assert.False(resolved.IsAccepted, "an unaffordable card is rejected");
            Assert.Equal(ValidationError.ERR_UNAFFORDABLE, resolved.Error, "G3");
            Assert.Equal(Side.PLAYER, resolved.RejectedSide!.Value, "and names the offending side");
            Assert.True(resolved.NextState is null, "no next state, therefore no mutation");
            Assert.Equal(0, state.Player.Structures.Count, "the original state is untouched");
        }

        [TestCase("PIPE-03", "A forced PASS still runs income, movement, combat and scoring")]
        public static void PassStillResolvesTheBoard()
        {
            // Both sides broke, so both PASS; a unit already on the board must still move.
            RunState state = Scenario.Board(LaneModifier.HIGHLAND, LaneModifier.RIVER, LaneModifier.HIGHLAND, turn: 5)
                .WithResources(Side.PLAYER, 0, 0)
                .WithResources(Side.SNAPSHOT, 0, 0)
                .WithUnits(Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 2));

            CardDefinition expensive = Card(CardType.TRAIN, 1);
            RunState after = Resolve(state, Hand(5, expensive), Selection.Pass, Selection.Pass);

            Assert.Equal(3, after.FindUnit("P1")!.TileIndex, "sec.6.6 item 4: movement still happens");
            Assert.Equal(3, after.Player.Growth, "income still ticks");
            Assert.Equal(1, after.Player.Score, "and the now-exclusive contested tile still scores");
        }
    }
}
