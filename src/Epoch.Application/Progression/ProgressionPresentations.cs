using System;
using System.Collections.Generic;
using Epoch.Core.Domain;

namespace Epoch.Application.Progression
{
    public sealed record LandmarkPresentation(
        string LandmarkId,
        int Stage,
        int? NextCost,
        bool CanAffordNextStage);

    public sealed record CapitalHomePresentation(
        int Gold,
        string CurrentCapitalId,
        bool IsCapitalCompleted,
        int CompletedUpgradeCount,
        IReadOnlyList<LandmarkPresentation> Landmarks,
        bool CanStartNewBattle);

    public sealed record LandmarkUpgradePresentation(
        int Gold,
        string CapitalId,
        string LandmarkId,
        int Stage,
        int? NextCost,
        int? GoldAfterUpgrade,
        bool CanUpgrade,
        bool IsComplete);

    public sealed record CapitalWorldPresentation(
        string CapitalId,
        bool IsUnlocked,
        bool IsCurrent,
        bool IsCompleted,
        int CompletedUpgradeCount);

    public sealed record WorldProgressionPresentation(
        string CurrentCapitalId,
        bool IsWorldComplete,
        IReadOnlyList<CapitalWorldPresentation> Capitals);

    public sealed record ResultRewardsPresentation(
        MatchOutcome Outcome,
        int PlayerScore,
        int SnapshotScore,
        string Seed,
        BattleRewardStatus RewardStatus,
        int RewardGold,
        int GoldCreditedNow,
        int GoldBalance,
        bool CanContinueToCapital,
        bool CanReplay,
        bool CanCopySeed,
        bool CanRestartSameSeedPractice);

    public static class ProgressionViews
    {
        public static CapitalHomePresentation CapitalHome(ProgressionState state)
        {
            CapitalProgress capital = Capital(state, state.CurrentCapitalId);
            CapitalDefinition definition = ProgressionCatalog.Capital(capital.CapitalId);
            List<LandmarkPresentation> landmarks = new List<LandmarkPresentation>(capital.Landmarks.Count);
            int completed = 0;
            for (int i = 0; i < capital.Landmarks.Count; i++)
            {
                LandmarkProgress landmark = capital.Landmarks[i];
                completed += landmark.Stage;
                int? nextCost = landmark.Stage < 5 ? definition.StageCosts[landmark.Stage] : null;
                landmarks.Add(new LandmarkPresentation(
                    landmark.LandmarkId,
                    landmark.Stage,
                    nextCost,
                    nextCost.HasValue && state.Gold >= nextCost.Value));
            }

            return new CapitalHomePresentation(
                state.Gold,
                capital.CapitalId,
                capital.IsCompleted,
                completed,
                landmarks,
                true);
        }

        public static LandmarkUpgradePresentation LandmarkUpgrade(ProgressionState state, string landmarkId)
        {
            CapitalProgress capital = Capital(state, state.CurrentCapitalId);
            int index = ProgressionCatalog.LandmarkIndex(landmarkId);
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(landmarkId));
            }

            LandmarkProgress landmark = capital.Landmarks[index];
            int? nextCost = landmark.Stage < 5
                ? ProgressionCatalog.Capital(capital.CapitalId).StageCosts[landmark.Stage]
                : null;
            bool canUpgrade = !capital.IsCompleted && nextCost.HasValue && state.Gold >= nextCost.Value;
            return new LandmarkUpgradePresentation(
                state.Gold,
                capital.CapitalId,
                landmark.LandmarkId,
                landmark.Stage,
                nextCost,
                canUpgrade ? state.Gold - nextCost!.Value : null,
                canUpgrade,
                landmark.Stage == 5);
        }

        public static WorldProgressionPresentation WorldProgression(ProgressionState state)
        {
            List<CapitalWorldPresentation> capitals = new List<CapitalWorldPresentation>(state.Capitals.Count);
            for (int i = 0; i < state.Capitals.Count; i++)
            {
                CapitalProgress capital = state.Capitals[i];
                int completed = 0;
                for (int j = 0; j < capital.Landmarks.Count; j++)
                {
                    completed += capital.Landmarks[j].Stage;
                }

                capitals.Add(new CapitalWorldPresentation(
                    capital.CapitalId,
                    capital.IsUnlocked,
                    string.Equals(capital.CapitalId, state.CurrentCapitalId, StringComparison.Ordinal),
                    capital.IsCompleted,
                    completed));
            }

            return new WorldProgressionPresentation(state.CurrentCapitalId, state.IsWorldComplete, capitals);
        }

        private static CapitalProgress Capital(ProgressionState state, string capitalId)
        {
            for (int i = 0; i < state.Capitals.Count; i++)
            {
                if (string.Equals(state.Capitals[i].CapitalId, capitalId, StringComparison.Ordinal))
                {
                    return state.Capitals[i];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(capitalId));
        }
    }
}
