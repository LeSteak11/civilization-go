using System;
using System.Collections.Generic;
using Epoch.Application.Runs;
using Epoch.Core.Content;
using Epoch.Core.Domain;

namespace Epoch.Application.Progression
{
    public sealed class EpochGameSession
    {
        private readonly ValidatedContentSet _content;
        private string? _activeBattleRunId;
        private PlayableMatchSession? _battle;

        public EpochGameSession(ValidatedContentSet content, ProgressionStore store)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            Progression = new ProgressionService(store);
        }

        public ProgressionService Progression { get; }

        public PlayableMatchSession? Battle => _battle;

        public ResultRewardsPresentation? ResultRewards { get; private set; }

        public BattleRunProgress StartNewBattle(SeedCode seed, string? battleRunId = null)
        {
            EnsureAtCapital();
            string id = battleRunId ?? Guid.NewGuid().ToString("N");
            PlayableMatchSession battle = new PlayableMatchSession(seed, _content);
            BattleRunProgress run = Progression.StartBattle(id, BattleRewardEligibility.ELIGIBLE);
            _activeBattleRunId = id;
            _battle = battle;
            ResultRewards = null;
            return run;
        }

        public void ResumeBattle(string battleRunId, PlayableMatchSession battle)
        {
            EnsureAtCapital();
            BattleRunProgress run = Progression.BattleRun(battleRunId);
            if (run.IsCompleted || battle.IsComplete)
            {
                throw new InvalidOperationException("Only an incomplete battle can be resumed.");
            }

            _activeBattleRunId = battleRunId;
            _battle = battle;
            ResultRewards = null;
        }

        public void RestoreCompletedResult(string battleRunId, PlayableMatchSession battle)
        {
            EnsureAtCapital();
            if (!battle.IsComplete)
            {
                throw new InvalidOperationException("Only a completed battle can restore Result & Rewards.");
            }

            _activeBattleRunId = battleRunId;
            _battle = battle;
            PresentResult(Progression.CompleteBattle(battleRunId, battle.State.Result!), battle.State.Result!);
        }

        public PresentationTurn SubmitBattleSelection(Selection selection)
        {
            PlayableMatchSession battle = RequireIncompleteBattle();
            PresentationTurn turn = battle.Submit(selection);
            FinalizeResultIfComplete();
            return turn;
        }

        public PresentationTurn SubmitForcedPass()
        {
            PlayableMatchSession battle = RequireIncompleteBattle();
            PresentationTurn turn = battle.SubmitForcedPass();
            FinalizeResultIfComplete();
            return turn;
        }

        public BattleRunProgress RestartSameSeedPractice(string? battleRunId = null)
        {
            if (_battle is null || !_battle.IsComplete || ResultRewards is null)
            {
                throw new InvalidOperationException("Restart Same Seed is available only from a completed result.");
            }

            string id = battleRunId ?? Guid.NewGuid().ToString("N");
            PlayableMatchSession practice = _battle.RestartSameSeed();
            BattleRunProgress run = Progression.StartBattle(id, BattleRewardEligibility.PRACTICE);
            _activeBattleRunId = id;
            _battle = practice;
            ResultRewards = null;
            return run;
        }

        public IReadOnlyList<PresentationTurn> ReplayTurns()
        {
            if (_battle is null || !_battle.IsComplete || ResultRewards is null)
            {
                throw new InvalidOperationException("Replay is available only from a completed result.");
            }

            return _battle.Turns;
        }

        public CapitalHomePresentation ContinueToCapital()
        {
            if (ResultRewards is null)
            {
                throw new InvalidOperationException("Continue to Capital is available only from a completed result.");
            }

            _activeBattleRunId = null;
            _battle = null;
            ResultRewards = null;
            return Progression.CapitalHome();
        }

        public CapitalHomePresentation CapitalHome()
        {
            EnsureAtCapital();
            return Progression.CapitalHome();
        }

        public LandmarkUpgradePresentation LandmarkUpgrade(string landmarkId)
        {
            EnsureAtCapital();
            return Progression.LandmarkUpgrade(landmarkId);
        }

        public LandmarkUpgradeReceipt UpgradeLandmark(string landmarkId)
        {
            EnsureAtCapital();
            return Progression.UpgradeLandmark(Progression.State.CurrentCapitalId, landmarkId);
        }

        public WorldProgressionPresentation WorldProgression()
        {
            EnsureAtCapital();
            return Progression.WorldProgression();
        }

        public void SelectCapital(string capitalId)
        {
            EnsureAtCapital();
            Progression.SelectCapital(capitalId);
        }

        private void FinalizeResultIfComplete()
        {
            if (_battle is null || !_battle.IsComplete || _activeBattleRunId is null)
            {
                return;
            }

            MatchResult result = _battle.State.Result!;
            BattleRewardReceipt receipt = Progression.CompleteBattle(_activeBattleRunId, result);
            PresentResult(receipt, result);
        }

        private void PresentResult(BattleRewardReceipt receipt, MatchResult result)
        {
            ResultRewards = new ResultRewardsPresentation(
                result.Outcome,
                result.PlayerScore,
                result.SnapshotScore,
                result.Seed.Text,
                receipt.Status,
                receipt.RewardGold,
                receipt.GoldCreditedNow,
                receipt.GoldBalance,
                true,
                true,
                true,
                true);
        }

        private PlayableMatchSession RequireIncompleteBattle()
        {
            if (_battle is null || _battle.IsComplete || _activeBattleRunId is null)
            {
                throw new InvalidOperationException("There is no incomplete active battle.");
            }

            return _battle;
        }

        private void EnsureAtCapital()
        {
            if (_battle is not null || ResultRewards is not null)
            {
                throw new InvalidOperationException("A new battle can start only from Capital/Home.");
            }
        }
    }
}
