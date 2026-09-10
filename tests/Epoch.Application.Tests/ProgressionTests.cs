using System;
using System.Collections.Generic;
using Epoch.Application.Progression;
using Epoch.Application.Runs;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Rng;
using Epoch.Testing;

namespace Epoch.Application.Tests
{
    public static class ProgressionTests
    {
        [TestCase("D2P04-01", "A new progression save has Gold zero and the approved 5x5 capital structure")]
        public static void NewSaveAndRoundTrip()
        {
            MemoryProgressionStore store = new MemoryProgressionStore();
            ProgressionService progression = new ProgressionService(store);

            Assert.Equal(0, progression.State.Gold, "a new save starts with no Gold");
            Assert.Equal("capital_01", progression.State.CurrentCapitalId, "the first capital is focused");
            Assert.Equal(3, progression.State.Capitals.Count, "the prototype has three capitals");
            for (int i = 0; i < progression.State.Capitals.Count; i++)
            {
                CapitalProgress capital = progression.State.Capitals[i];
                Assert.Equal(5, capital.Landmarks.Count, "every capital has five landmarks");
                Assert.Equal(i == 0, capital.IsUnlocked, "only the first capital starts unlocked");
                for (int j = 0; j < capital.Landmarks.Count; j++)
                {
                    Assert.Equal(0, capital.Landmarks[j].Stage, "every landmark starts at stage zero");
                }
            }

            Award(progression, "round-trip", BattleRewardEligibility.ELIGIBLE, Result(4, 1));
            _ = progression.UpgradeLandmark("capital_01", "landmark_01");
            ProgressionService reloaded = new ProgressionService(store);
            Assert.Equal(280, reloaded.State.Gold, "earned and spent Gold round-trips");
            Assert.Equal(progression.State.CurrentCapitalId, reloaded.State.CurrentCapitalId, "focus round-trips");
            Assert.Equal(3, reloaded.State.Capitals.Count, "capital state round-trips");
            Assert.Equal(1, reloaded.State.Capitals[0].Landmarks[0].Stage, "landmark stages round-trip");
            Assert.True(reloaded.State.BattleRuns[0].RewardGranted, "reward claims round-trip");

            reloaded.ResetDevelopmentProgression();
            Assert.Equal(0, reloaded.State.Gold, "development reset restores new-save Gold");
            Assert.Empty(reloaded.State.BattleRuns, "development reset clears reward records");
        }

        [TestCase("D2P04-02", "Eligible results award 300/250/200 Gold once and practice awards none")]
        public static void BattleRewardsAreIdempotent()
        {
            ProgressionService progression = NewProgression();
            Award(progression, "victory", BattleRewardEligibility.ELIGIBLE, Result(4, 1));
            Award(progression, "tie", BattleRewardEligibility.ELIGIBLE, Result(2, 2));
            Award(progression, "defeat", BattleRewardEligibility.ELIGIBLE, Result(1, 4));
            Assert.Equal(750, progression.State.Gold, "Victory, Tie, and Defeat pay 300/250/200");

            BattleRewardReceipt duplicate = progression.CompleteBattle("victory", Result(4, 1));
            Assert.Equal(BattleRewardStatus.ALREADY_CREDITED, duplicate.Status, "reopening cannot award twice");
            Assert.Equal(300, duplicate.RewardGold, "a reopened result retains its original reward amount");
            Assert.Equal(0, duplicate.GoldCreditedNow, "a repeated completion credits zero additional Gold");
            Assert.Equal(750, progression.State.Gold, "a repeated completion leaves Gold unchanged");

            progression.StartBattle("practice", BattleRewardEligibility.PRACTICE);
            BattleRewardReceipt practice = progression.CompleteBattle("practice", Result(4, 1));
            Assert.Equal(BattleRewardStatus.PRACTICE_NO_GOLD, practice.Status, "same-seed practice is no-Gold");
            Assert.Equal(0, practice.RewardGold, "practice awards nothing");
            Assert.Equal(0, practice.GoldCreditedNow, "practice credits nothing");
            Assert.Equal(750, progression.State.Gold, "practice cannot change persistent Gold");

            Award(progression, "genuinely-new", BattleRewardEligibility.ELIGIBLE, Result(4, 1));
            Assert.Equal(1050, progression.State.Gold, "a genuinely new battle remains eligible");
        }

        [TestCase("D2P04-03", "Landmarks use exact sequential costs and allow banked Gold")]
        public static void LandmarkStagesAndCosts()
        {
            ProgressionService progression = NewProgression();
            Award(progression, "funding", BattleRewardEligibility.ELIGIBLE, Result(4, 1));
            int[] costs = { 20, 30, 40, 50, 60 };
            for (int stage = 0; stage < costs.Length; stage++)
            {
                int before = progression.State.Gold;
                LandmarkUpgradeReceipt receipt = progression.UpgradeLandmark("capital_01", "landmark_01");
                Assert.Equal(costs[stage], receipt.Cost, "the approved stage cost is charged");
                Assert.Equal(stage + 1, receipt.NewStage, "stages advance sequentially");
                Assert.Equal(before - costs[stage], progression.State.Gold, "only the stage cost is deducted");
            }

            Assert.Equal(100, progression.State.Gold, "unspent Gold remains banked");
            Assert.True(progression.CapitalHome().CanStartNewBattle, "banked Gold never blocks the Battle CTA");

            ProgressionService empty = NewProgression();
            Assert.Throws<InvalidOperationException>(
                () => empty.UpgradeLandmark("capital_01", "landmark_01"),
                "an unaffordable upgrade is rejected without mutation");
            Assert.Equal(0, empty.State.Gold, "rejected upgrade preserves Gold");
            Assert.Equal(0, empty.State.Capitals[0].Landmarks[0].Stage, "rejected upgrade preserves stage");
        }

        [TestCase("D2P04-04", "Completing each 5x5 capital pays once, unlocks, auto-focuses, and completes the world")]
        public static void CapitalAndWorldCompletion()
        {
            ProgressionService progression = NewProgression();
            for (int i = 0; i < 20; i++)
            {
                Award(progression, "fund-" + i, BattleRewardEligibility.ELIGIBLE, Result(4, 1));
            }

            int beforeFirst = progression.State.Gold;
            CompleteCurrentCapital(progression, "capital_01");
            Assert.Equal(beforeFirst - 1000 + 250, progression.State.Gold,
                "capital 1 charges 1000 and pays 250");
            Assert.Equal("capital_02", progression.State.CurrentCapitalId, "capital 2 auto-focuses");
            Assert.True(progression.State.Capitals[0].IsCompleted, "capital 1 completes after 25 upgrades");
            Assert.True(progression.State.Capitals[1].IsUnlocked, "capital 2 unlocks");

            progression.SelectCapital("capital_01");
            Assert.Equal("capital_01", progression.State.CurrentCapitalId, "completed capitals remain revisitable");
            progression.SelectCapital("capital_02");

            int beforeSecond = progression.State.Gold;
            CompleteCurrentCapital(progression, "capital_02");
            Assert.Equal(beforeSecond - 1750 + 350, progression.State.Gold,
                "capital 2 charges 1750 and pays 350");
            Assert.Equal("capital_03", progression.State.CurrentCapitalId, "capital 3 auto-focuses");
            Assert.True(progression.State.Capitals[2].IsUnlocked, "capital 3 unlocks");

            int beforeFinal = progression.State.Gold;
            CompleteCurrentCapital(progression, "capital_03");
            Assert.True(progression.State.IsWorldComplete, "all three capitals complete the prototype world");
            Assert.Equal(beforeFinal - 2500 + 500, progression.State.Gold, "final capital charges 2500 and pays 500");
            Assert.Equal("capital_03", progression.State.CurrentCapitalId, "the final completed capital remains focused");
        }

        [TestCase("D2P04-05", "UI-facing projections expose Capital, upgrade, world, and result actions")]
        public static void PresentationNeutralViews()
        {
            ProgressionService progression = NewProgression();
            Award(progression, "view-funding", BattleRewardEligibility.ELIGIBLE, Result(4, 1));

            CapitalHomePresentation capital = progression.CapitalHome();
            Assert.Equal(300, capital.Gold, "Capital shows persistent Gold");
            Assert.Equal(5, capital.Landmarks.Count, "Capital exposes five landmark states");
            Assert.True(capital.CanStartNewBattle, "Capital exposes the Battle action");

            LandmarkUpgradePresentation upgrade = progression.LandmarkUpgrade("landmark_01");
            Assert.Equal(20, upgrade.NextCost, "the modal exposes the next exact cost");
            Assert.Equal(280, upgrade.GoldAfterUpgrade, "the modal exposes the post-upgrade balance");
            Assert.True(upgrade.CanUpgrade, "the modal exposes affordability");

            WorldProgressionPresentation world = progression.WorldProgression();
            Assert.Equal(3, world.Capitals.Count, "the World sheet exposes the ordered sequence");
            Assert.True(world.Capitals[0].IsCurrent, "the focused capital is explicit");
            Assert.False(world.Capitals[1].IsUnlocked, "locked capitals remain non-selectable");
        }

        [TestCase("D2P04-06", "Progression wraps completion without changing the authoritative battle or replay hash")]
        public static void BattleBoundaryAndResultFlow()
        {
            ValidatedContentSet content = ContentFixtureLoader.Load();
            SeedCode seed = new SeedCode(SeedCodec.Encode(4242));
            MemoryProgressionStore store = new MemoryProgressionStore();
            EpochGameSession game = new EpochGameSession(content, store);
            game.StartNewBattle(seed, "wrapped");
            PlayableMatchSession direct = new PlayableMatchSession(seed, content);

            Assert.Throws<InvalidOperationException>(
                () => game.UpgradeLandmark("landmark_01"),
                "Capital actions are unavailable while Battle is active");

            while (!direct.IsComplete)
            {
                Selection selection = FirstLegal(direct.Cards());
                if (selection.IsPass)
                {
                    _ = direct.SubmitForcedPass();
                    _ = game.SubmitForcedPass();
                }
                else
                {
                    _ = direct.Submit(selection);
                    _ = game.SubmitBattleSelection(selection);
                }
            }

            Assert.True(game.ResultRewards is not null, "completion exposes Result & Rewards");
            Assert.Equal(BattleRewardStatus.CREDITED, game.ResultRewards!.RewardStatus, "eligible Gold is already credited");
            Assert.Equal(game.ResultRewards.RewardGold, game.ResultRewards.GoldCreditedNow,
                "a first eligible result exposes the amount credited now");
            Assert.True(game.ResultRewards.CanContinueToCapital, "Continue is exposed");
            Assert.True(game.ResultRewards.CanReplay, "read-only Replay is exposed");
            Assert.True(game.ResultRewards.CanCopySeed, "Copy Seed is exposed");
            Assert.True(game.ResultRewards.CanRestartSameSeedPractice, "practice restart is exposed");
            Assert.Equal(
                direct.Turns[direct.Turns.Count - 1].StateHash,
                game.Battle!.Turns[game.Battle.Turns.Count - 1].StateHash,
                "progression does not enter the authoritative state hash");
            Assert.Equal(direct.State.Result, game.Battle.State.Result, "progression does not change MatchResult");
            Assert.True(game.Battle.VerifyReplay(), "the wrapped battle still verifies its replay");

            int gold = game.Progression.State.Gold;
            EpochGameSession restored = new EpochGameSession(content, store);
            restored.RestoreCompletedResult("wrapped", direct);
            Assert.Equal(BattleRewardStatus.ALREADY_CREDITED, restored.ResultRewards!.RewardStatus,
                "a restored rewarded result is marked already credited");
            Assert.Equal(0, restored.ResultRewards.GoldCreditedNow,
                "restoring a rewarded result credits no additional Gold");
            Assert.Equal(gold, restored.Progression.State.Gold,
                "restoring a rewarded result preserves persistent Gold");

            BattleRunProgress practice = game.RestartSameSeedPractice("practice-restart");
            Assert.Equal(BattleRewardEligibility.PRACTICE, practice.Eligibility, "Restart Same Seed is classified as practice");
            Assert.Equal(gold, game.Progression.State.Gold, "starting practice does not change Gold");
        }

        private static void CompleteCurrentCapital(ProgressionService progression, string capitalId)
        {
            for (int landmark = 0; landmark < ProgressionCatalog.LandmarkIds.Count; landmark++)
            {
                for (int stage = 0; stage < 5; stage++)
                {
                    _ = progression.UpgradeLandmark(capitalId, ProgressionCatalog.LandmarkIds[landmark]);
                }
            }
        }

        private static void Award(
            ProgressionService progression,
            string runId,
            BattleRewardEligibility eligibility,
            MatchResult result)
        {
            _ = progression.StartBattle(runId, eligibility);
            _ = progression.CompleteBattle(runId, result);
        }

        private static MatchResult Result(int playerScore, int snapshotScore) =>
            new MatchResult("rules", "content", new SeedCode("seed"), playerScore, snapshotScore, 24);

        private static ProgressionService NewProgression() =>
            new ProgressionService(new MemoryProgressionStore());

        private static Selection FirstLegal(IReadOnlyList<CardPresentation> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                CardPresentation card = cards[i];
                if (!card.IsLegal)
                {
                    continue;
                }

                return card.Definition.CardType == CardType.ADVANCE
                    ? new Selection(card.OfferIndex, null)
                    : new Selection(card.OfferIndex, card.LegalLanes[0]);
            }

            return Selection.Pass;
        }

        private sealed class MemoryProgressionStore : ProgressionStore
        {
            private string? _value;

            public string? Load() => _value;

            public void Save(string serializedState) => _value = serializedState;
        }
    }
}
