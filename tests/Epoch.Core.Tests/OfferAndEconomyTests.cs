using System.Collections.Generic;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Rng;
using Epoch.Core.StateMachine;
using Epoch.Core.Systems;
using Epoch.Testing;

namespace Epoch.Core.Tests
{
    /// <summary>
    /// Shared offer generation (Core Spec sec.6.0, [Final Lock 2]), lane modifiers
    /// [Lock 10], the economy (sec.7.1) and the validation gates (sec.6.1-6.6).
    ///
    /// These run against the real authored pool, because the thing most worth proving is
    /// that the locked weights and the authored roster actually produce legal hands for
    /// all 24 turns.
    /// </summary>
    public static class OfferAndEconomyTests
    {
        private static ValidatedContentSet Content => ContentFixtureLoader.Load();

        private static SeedState Seed(ulong master) =>
            IndexedRng.DeriveStreams(new SeedCode(SeedCodec.Encode(master)), master, SeedState.PinnedAlgorithm);

        [TestCase("GT-16", "The same seed produces the same 72 offers, identical for both sides")]
        public static void SameSeedOfferIdentity()
        {
            // Offers are a pure function of (seed, turn, content). Nothing about a side --
            // its resources, perks or lane capacity -- reaches the generator, which is
            // exactly why one shared hand can serve both sides (Invariant CO-1).
            for (ulong master = 0; master < 5; master++)
            {
                SeedState seed = Seed(master);
                for (int turn = 1; turn <= RunState.TurnsPerRun; turn++)
                {
                    CardOfferSet first = OfferGeneration.Generate(seed, turn, Content);
                    CardOfferSet second = OfferGeneration.Generate(seed, turn, Content);

                    Assert.Equal(RunState.CardsOfferedPerTurn, first.Count, "three offers per turn");
                    for (int slot = 0; slot < first.Count; slot++)
                    {
                        Assert.Equal(
                            first[slot].CardId.Value,
                            second[slot].CardId.Value,
                            "seed " + master + " turn " + turn + " slot " + slot + " is reproducible");
                    }
                }
            }
        }

        [TestCase("GT-26", "A hand never contains a duplicate cardId, though duplicate types are legal")]
        public static void NoDuplicateCardIdsInAHand()
        {
            for (ulong master = 0; master < 60; master++)
            {
                SeedState seed = Seed(master);
                for (int turn = 1; turn <= RunState.TurnsPerRun; turn++)
                {
                    CardOfferSet hand = OfferGeneration.Generate(seed, turn, Content);
                    HashSet<string> seen = new HashSet<string>();
                    for (int slot = 0; slot < hand.Count; slot++)
                    {
                        Assert.True(
                            seen.Add(hand[slot].CardId.Value),
                            "seed " + master + " turn " + turn + " offered a duplicate cardId");
                    }
                }
            }
        }

        [TestCase("GT-11c", "BUILD is never offered on turns 21-24, across many seeds")]
        public static void BuildWindowClosesAtTurn21()
        {
            for (ulong master = 0; master < 250; master++)
            {
                SeedState seed = Seed(master);
                for (int turn = 21; turn <= RunState.TurnsPerRun; turn++)
                {
                    CardOfferSet hand = OfferGeneration.Generate(seed, turn, Content);
                    for (int slot = 0; slot < hand.Count; slot++)
                    {
                        Assert.NotEqual(
                            CardType.BUILD,
                            Content.ById(hand[slot].CardId).CardType,
                            "seed " + master + " turn " + turn + " offered a BUILD after the window closed");
                    }
                }
            }
        }

        [TestCase("OFFER-01", "BUILD is still reachable on turns 19-20 under the V1 exception")]
        public static void BuildStillOfferedAtNineteenAndTwenty()
        {
            bool sawBuild = false;
            for (ulong master = 0; master < 250 && !sawBuild; master++)
            {
                SeedState seed = Seed(master);
                for (int turn = 19; turn <= 20 && !sawBuild; turn++)
                {
                    CardOfferSet hand = OfferGeneration.Generate(seed, turn, Content);
                    for (int slot = 0; slot < hand.Count; slot++)
                    {
                        if (Content.ById(hand[slot].CardId).CardType == CardType.BUILD)
                        {
                            sawBuild = true;
                        }
                    }
                }
            }

            Assert.True(sawBuild, "the V1 Build Economy Exception restores BUILD to turns 19-20");
        }

        [TestCase("GT-24", "Every run assigns exactly one RIVER, HIGHLAND and COAST, and all 6 layouts occur")]
        public static void LaneModifierSet()
        {
            HashSet<string> layouts = new HashSet<string>();
            for (ulong master = 0; master < 500; master++)
            {
                LaneState[] lanes = LaneModifierAssignment.Assign(Seed(master));

                HashSet<LaneModifier> modifiers = new HashSet<LaneModifier>();
                string layout = string.Empty;
                foreach (LaneState lane in lanes)
                {
                    modifiers.Add(lane.Modifier);
                    layout += lane.Modifier.ToString()[0];
                }

                Assert.Equal(3, modifiers.Count, "seed " + master + " must hold one of each modifier");
                layouts.Add(layout);
            }

            Assert.Equal(6, layouts.Count, "all 3! = 6 permutations occur across seeds");
        }

        [TestCase("GT-01", "Turn 1 has no contested income and no HIGHLAND holder")]
        public static void TurnOneBaseline()
        {
            RunState state = Scenario.Board();

            Assert.Equal(PlayerState.StartingGrowth, state.Player.Growth, "opening Growth is 8");
            Assert.Equal(PlayerState.StartingInsight, state.Player.Insight, "opening Insight is 2");

            IncomeBreakdown growth = IncomeSystem.Compute(state, Side.PLAYER, ResourceType.GROWTH);
            IncomeBreakdown insight = IncomeSystem.Compute(state, Side.PLAYER, ResourceType.INSIGHT);

            Assert.Equal(3, growth.Total, "3 base Growth and nothing else on turn 1");
            Assert.Equal(2, insight.Total, "2 base Insight and nothing else on turn 1");
            Assert.Equal(0, growth.Contested, "EC-31: tile3HolderPrevTurn is null on turn 1");
        }

        [TestCase("GT-02", "A structure yields on the turn it is placed, and RIVER adds +1 Growth")]
        public static void StructureYieldsImmediatelyAndRiverAdds()
        {
            // Lane A is RIVER in the default scenario board.
            RunState plain = Scenario.Board()
                .WithStructures(Side.PLAYER, Scenario.Structure("S1", Side.PLAYER, ResourceType.GROWTH, 1, LaneId.B));
            Assert.Equal(4, IncomeSystem.Compute(plain, Side.PLAYER, ResourceType.GROWTH).Total,
                "3 base + 1 tier-1 structure (Q1: it yields the turn it is placed)");

            // The RIVER bonus is applied at placement and carried on the structure.
            RunState river = Scenario.Board()
                .WithStructures(Side.PLAYER, Scenario.Structure("S1", Side.PLAYER, ResourceType.GROWTH, 2, LaneId.A));
            Assert.Equal(5, IncomeSystem.Compute(river, Side.PLAYER, ResourceType.GROWTH).Total,
                "3 base + (1 tier + 1 RIVER)");
        }

        [TestCase("ECON-01", "Contested income reads the PREVIOUS turn's holder, +1 Growth and +1 Insight")]
        public static void ContestedIncomeUsesPreviousHolder()
        {
            RunState state = Scenario.Board(turn: 5)
                .WithPrevHolder(LaneId.A, Side.PLAYER)
                .WithPrevHolder(LaneId.B, Side.PLAYER)
                .WithPrevHolder(LaneId.C, Side.SNAPSHOT);

            Assert.Equal(5, IncomeSystem.Compute(state, Side.PLAYER, ResourceType.GROWTH).Total, "3 base + 2 held");
            Assert.Equal(4, IncomeSystem.Compute(state, Side.PLAYER, ResourceType.INSIGHT).Total, "2 base + 2 held");
            Assert.Equal(4, IncomeSystem.Compute(state, Side.SNAPSHOT, ResourceType.GROWTH).Total, "3 base + 1 held");
        }

        [TestCase("GT-28", "Resources are uncapped: 998 + 5 is 1003, with no clamp")]
        public static void ResourcesAreUncapped()
        {
            RunState state = Scenario.Board().WithResources(Side.PLAYER, 998, 0);
            RunState credited = IncomeSystem.ApplyBoth(state, out _, out _, out _, out _);

            Assert.Equal(1001, credited.Player.Growth, "998 + 3 base, uncapped [Final Lock 6]");
            Assert.True(
                credited.Player.Growth > PlayerState.ResourceWarningThreshold,
                "the diagnostic threshold is advisory and never clamps");
        }

        [TestCase("GT-10", "Age is a pure function of turn and advances only at the boundary")]
        public static void AgeBoundaries()
        {
            Assert.Equal(1, RunState.AgeForTurn(6), "turn 6 resolves entirely under Age I");
            Assert.Equal(2, RunState.AgeForTurn(7), "turn 7 is the first Age II turn");
            Assert.Equal(4, RunState.AgeForTurn(24), "turn 24 is Age IV");

            AgeRow dawn = AgeTable.ForTurn(6);
            Assert.Equal(6, dawn.UnitCost, "Age I TRAIN costs 6");
            Assert.Equal(10, dawn.UnitPower, "Age I unit Power is 10");

            AgeRow bronze = AgeTable.ForTurn(7);
            Assert.Equal(8, bronze.UnitCost, "Age II TRAIN costs 8");
            Assert.Equal(12, bronze.UnitPower, "Age II unit Power is 12");
        }

        [TestCase("GT-09", "TRAIN is unselectable in a lane already holding three of that side's units")]
        public static void LaneCapacity()
        {
            RunState state = Scenario.Board(turn: 3).WithResources(Side.PLAYER, 100, 100);

            CardDefinition train = FindCard(CardType.TRAIN, 1);
            state = state.WithUnits(
                Scenario.Unit("P1", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 1),
                Scenario.Unit("P2", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 1),
                Scenario.Unit("P3", Side.PLAYER, UnitClass.SWORD, 10, LaneId.A, 1));

            CardOfferSet offers = new CardOfferSet(3, new[] { new CardOffer(0, train.CardId) });

            Assert.Equal(
                ValidationError.ERR_LANE_FULL,
                LegalitySystem.Check(state, Side.PLAYER, offers, 0, LaneId.A, Content),
                "T5: three units in the lane blocks a fourth");
            Assert.Equal(
                ValidationError.NONE,
                LegalitySystem.Check(state, Side.PLAYER, offers, 0, LaneId.B, Content),
                "but another lane is still legal");
        }

        [TestCase("GT-23", "A perk already held is unselectable for that side and selectable by the other")]
        public static void PerkUniquenessIsPerSide()
        {
            CardDefinition perk = FindCard(CardType.ADVANCE, 1);
            RunState state = Scenario.Board(turn: 3)
                .WithResources(Side.PLAYER, 100, 100)
                .WithResources(Side.SNAPSHOT, 100, 100)
                .WithPerks(Side.PLAYER, perk);

            CardOfferSet offers = new CardOfferSet(3, new[] { new CardOffer(0, perk.CardId) });

            Assert.Equal(
                ValidationError.ERR_DUPLICATE_PERK,
                LegalitySystem.Check(state, Side.PLAYER, offers, 0, null, Content),
                "A5: unique per side per run");
            Assert.Equal(
                ValidationError.NONE,
                LegalitySystem.Check(state, Side.SNAPSHOT, offers, 0, null, Content),
                "and it stays selectable by the opposing side");
        }

        [TestCase("GT-12", "A second Keystone is unselectable for the side that already holds one")]
        public static void KeystoneMaxOne()
        {
            CardDefinition keystone = FindKeystone();
            CardDefinition other = FindOtherKeystone(keystone);

            RunState state = Scenario.Board(turn: 15)
                .WithResources(Side.PLAYER, 100, 100)
                .WithResources(Side.SNAPSHOT, 100, 100)
                .WithPerks(Side.PLAYER, keystone);

            CardOfferSet offers = new CardOfferSet(15, new[] { new CardOffer(0, other.CardId) });

            Assert.True(state.Player.KeystoneTaken, "K2: the first Keystone sets the flag");
            Assert.Equal(
                ValidationError.ERR_KEYSTONE_ALREADY_TAKEN,
                LegalitySystem.Check(state, Side.PLAYER, offers, 0, null, Content),
                "K3: a second Keystone is unselectable");
            Assert.Equal(
                ValidationError.NONE,
                LegalitySystem.Check(state, Side.SNAPSHOT, offers, 0, null, Content),
                "K4: it is still offered and selectable by the opponent");
        }

        [TestCase("GT-08", "PASS is forced when nothing is legal, and rejected when something is")]
        public static void ForcedPassOnly()
        {
            CardDefinition train = FindCard(CardType.TRAIN, 1);
            CardOfferSet offers = new CardOfferSet(2, new[] { new CardOffer(0, train.CardId) });

            // Broke: an Age I TRAIN costs 6 Growth.
            RunState broke = Scenario.Board(turn: 2).WithResources(Side.PLAYER, 0, 0);
            Assert.False(
                LegalitySystem.HasAnyLegalSelection(broke, Side.PLAYER, offers, Content),
                "nothing is affordable");
            Assert.Equal(
                ValidationError.NONE,
                LegalitySystem.Validate(broke, Side.PLAYER, offers, Selection.Pass, Content),
                "so a PASS is legal");

            // Solvent: a PASS is no longer permitted, because voluntary passing is not a rule.
            RunState solvent = Scenario.Board(turn: 2).WithResources(Side.PLAYER, 50, 50);
            Assert.Equal(
                ValidationError.ERR_PASS_NOT_FORCED,
                LegalitySystem.Validate(solvent, Side.PLAYER, offers, Selection.Pass, Content),
                "sec.6.6 item 1: a side with a legal option must take one");
        }

        [TestCase("GT-11b", "A dominated late BUILD is legal: payback is never a runtime gate")]
        public static void LateBuildIsLegal()
        {
            CardDefinition build = FindCard(CardType.BUILD, 4);

            // Turn 20, Age IV: cost 22, +4/turn over 4 remaining turns = 16 returned.
            RunState state = Scenario.Board(turn: 20).WithResources(Side.PLAYER, 22, 0);
            CardOfferSet offers = new CardOfferSet(20, new[] { new CardOffer(0, build.CardId) });

            Assert.Equal(
                ValidationError.NONE,
                LegalitySystem.Check(state, Side.PLAYER, offers, 0, LaneId.A, Content),
                "X2: a BUILD is never rejected for its payback ratio");
        }

        private static CardDefinition FindCard(CardType type, int age)
        {
            List<CardDefinition> pool = Content.EligiblePool(type, age);
            Assert.True(pool.Count > 0, "the authored pool must contain a " + type + " for Age " + age);
            return pool[0];
        }

        private static CardDefinition FindKeystone()
        {
            foreach (CardDefinition card in Content.Cards)
            {
                if (card.IsKeystone)
                {
                    return card;
                }
            }

            throw new AssertionException("The authored pool must contain a Keystone.");
        }

        private static CardDefinition FindOtherKeystone(CardDefinition first)
        {
            foreach (CardDefinition card in Content.Cards)
            {
                if (card.IsKeystone && card.CardId != first.CardId)
                {
                    return card;
                }
            }

            throw new AssertionException("The authored pool must contain a second Keystone.");
        }
    }
}
