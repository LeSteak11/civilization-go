using System.Collections.Generic;

namespace Epoch.Application.Progression
{
    public enum BattleRewardEligibility
    {
        ELIGIBLE,
        PRACTICE,
    }

    public enum BattleRewardStatus
    {
        CREDITED,
        ALREADY_CREDITED,
        PRACTICE_NO_GOLD,
    }

    public sealed record LandmarkProgress(string LandmarkId, int Stage);

    public sealed record CapitalProgress(
        string CapitalId,
        bool IsUnlocked,
        bool IsCompleted,
        bool CompletionRewardClaimed,
        IReadOnlyList<LandmarkProgress> Landmarks);

    public sealed record BattleRunProgress(
        string BattleRunId,
        BattleRewardEligibility Eligibility,
        bool IsCompleted,
        bool RewardGranted,
        int GoldAwarded);

    public sealed record ProgressionState(
        int SchemaVersion,
        int Gold,
        string CurrentCapitalId,
        IReadOnlyList<CapitalProgress> Capitals,
        IReadOnlyList<BattleRunProgress> BattleRuns)
    {
        public const int CurrentSchemaVersion = 1;

        public bool IsWorldComplete
        {
            get
            {
                for (int i = 0; i < Capitals.Count; i++)
                {
                    if (!Capitals[i].IsCompleted)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }

    public sealed record BattleRewardReceipt(
        string BattleRunId,
        BattleRewardStatus Status,
        int RewardGold,
        int GoldCreditedNow,
        int GoldBalance);

    public sealed record LandmarkUpgradeReceipt(
        string CapitalId,
        string LandmarkId,
        int Cost,
        int NewStage,
        bool CapitalCompleted,
        int CompletionGoldAwarded,
        string? UnlockedCapitalId,
        int GoldBalance);
}
