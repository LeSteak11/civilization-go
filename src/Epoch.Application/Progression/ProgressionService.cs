using System;
using System.Collections.Generic;
using Epoch.Core.Domain;

namespace Epoch.Application.Progression
{
    public sealed class ProgressionService
    {
        private readonly ProgressionStore _store;

        public ProgressionService(ProgressionStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            string? saved = store.Load();
            State = saved is null ? ProgressionCatalog.NewState() : ProgressionSaveCodec.Decode(saved);
            Validate(State);
            Persist();
        }

        public ProgressionState State { get; private set; }

        public BattleRunProgress StartBattle(string battleRunId, BattleRewardEligibility eligibility)
        {
            if (string.IsNullOrWhiteSpace(battleRunId))
            {
                throw new ArgumentException("A battle-run id is required.", nameof(battleRunId));
            }

            if (FindBattleRunIndex(battleRunId) >= 0)
            {
                throw new InvalidOperationException("Battle run '" + battleRunId + "' already exists.");
            }

            BattleRunProgress run = new BattleRunProgress(battleRunId, eligibility, false, false, 0);
            List<BattleRunProgress> runs = Copy(State.BattleRuns);
            runs.Add(run);
            State = State with { BattleRuns = runs };
            Persist();
            return run;
        }

        public BattleRunProgress BattleRun(string battleRunId)
        {
            int index = FindBattleRunIndex(battleRunId);
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(battleRunId), "Unknown battle run '" + battleRunId + "'.");
            }

            return State.BattleRuns[index];
        }

        public BattleRewardReceipt CompleteBattle(string battleRunId, MatchResult result)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            int index = FindBattleRunIndex(battleRunId);
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(battleRunId), "Unknown battle run '" + battleRunId + "'.");
            }

            BattleRunProgress run = State.BattleRuns[index];
            if (run.IsCompleted)
            {
                return new BattleRewardReceipt(
                    run.BattleRunId,
                    run.RewardGranted ? BattleRewardStatus.ALREADY_CREDITED : BattleRewardStatus.PRACTICE_NO_GOLD,
                    run.GoldAwarded,
                    0,
                    State.Gold);
            }

            int award = run.Eligibility == BattleRewardEligibility.ELIGIBLE
                ? GoldFor(result.Outcome)
                : 0;
            BattleRunProgress completed = run with
            {
                IsCompleted = true,
                RewardGranted = run.Eligibility == BattleRewardEligibility.ELIGIBLE,
                GoldAwarded = award,
            };
            List<BattleRunProgress> runs = Copy(State.BattleRuns);
            runs[index] = completed;
            State = State with { Gold = checked(State.Gold + award), BattleRuns = runs };
            Persist();

            return new BattleRewardReceipt(
                battleRunId,
                run.Eligibility == BattleRewardEligibility.ELIGIBLE
                    ? BattleRewardStatus.CREDITED
                    : BattleRewardStatus.PRACTICE_NO_GOLD,
                award,
                award,
                State.Gold);
        }

        public LandmarkUpgradeReceipt UpgradeLandmark(string capitalId, string landmarkId)
        {
            if (!string.Equals(State.CurrentCapitalId, capitalId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Only the currently focused capital can be upgraded.");
            }

            int capitalIndex = FindCapitalIndex(capitalId);
            if (capitalIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capitalId), "Unknown capital '" + capitalId + "'.");
            }

            CapitalProgress capital = State.Capitals[capitalIndex];
            if (!capital.IsUnlocked || capital.IsCompleted)
            {
                throw new InvalidOperationException("The selected capital is not upgradeable.");
            }

            int landmarkIndex = ProgressionCatalog.LandmarkIndex(landmarkId);
            if (landmarkIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(landmarkId), "Unknown landmark '" + landmarkId + "'.");
            }

            LandmarkProgress landmark = capital.Landmarks[landmarkIndex];
            if (landmark.Stage >= 5)
            {
                throw new InvalidOperationException("The selected landmark is already complete.");
            }

            CapitalDefinition definition = ProgressionCatalog.Capital(capitalId);
            int cost = definition.StageCosts[landmark.Stage];
            if (State.Gold < cost)
            {
                throw new InvalidOperationException("Not enough Gold for this landmark upgrade.");
            }

            List<LandmarkProgress> landmarks = Copy(capital.Landmarks);
            landmarks[landmarkIndex] = landmark with { Stage = landmark.Stage + 1 };
            bool completedCapital = AllLandmarksComplete(landmarks);
            int completionAward = completedCapital ? definition.CompletionReward : 0;
            string? unlockedCapitalId = null;

            List<CapitalProgress> capitals = Copy(State.Capitals);
            capitals[capitalIndex] = capital with
            {
                IsCompleted = completedCapital,
                CompletionRewardClaimed = completedCapital,
                Landmarks = landmarks,
            };

            string currentCapitalId = State.CurrentCapitalId;
            if (completedCapital && capitalIndex + 1 < capitals.Count)
            {
                CapitalProgress next = capitals[capitalIndex + 1];
                capitals[capitalIndex + 1] = next with { IsUnlocked = true };
                unlockedCapitalId = next.CapitalId;
                currentCapitalId = next.CapitalId;
            }

            State = State with
            {
                Gold = checked(State.Gold - cost + completionAward),
                CurrentCapitalId = currentCapitalId,
                Capitals = capitals,
            };
            Persist();

            return new LandmarkUpgradeReceipt(
                capitalId,
                landmarkId,
                cost,
                landmark.Stage + 1,
                completedCapital,
                completionAward,
                unlockedCapitalId,
                State.Gold);
        }

        public void SelectCapital(string capitalId)
        {
            int index = FindCapitalIndex(capitalId);
            if (index < 0 || !State.Capitals[index].IsUnlocked)
            {
                throw new InvalidOperationException("Only an unlocked capital can be selected.");
            }

            State = State with { CurrentCapitalId = capitalId };
            Persist();
        }

        public void ResetDevelopmentProgression()
        {
            State = ProgressionCatalog.NewState();
            Persist();
        }

        public CapitalHomePresentation CapitalHome() => ProgressionViews.CapitalHome(State);

        public LandmarkUpgradePresentation LandmarkUpgrade(string landmarkId) =>
            ProgressionViews.LandmarkUpgrade(State, landmarkId);

        public WorldProgressionPresentation WorldProgression() =>
            ProgressionViews.WorldProgression(State);

        public static void Validate(ProgressionState state)
        {
            if (state.SchemaVersion != ProgressionState.CurrentSchemaVersion || state.Gold < 0)
            {
                throw new InvalidOperationException("Unsupported or invalid progression state.");
            }

            if (state.Capitals.Count != ProgressionCatalog.Capitals.Count)
            {
                throw new InvalidOperationException("Progression state must contain the three prototype capitals.");
            }

            int incompleteUnlocked = 0;
            bool currentFound = false;
            for (int i = 0; i < state.Capitals.Count; i++)
            {
                CapitalProgress capital = state.Capitals[i];
                if (!string.Equals(capital.CapitalId, ProgressionCatalog.Capitals[i].CapitalId, StringComparison.Ordinal) ||
                    capital.Landmarks.Count != ProgressionCatalog.LandmarkIds.Count)
                {
                    throw new InvalidOperationException("Progression capital structure does not match the approved catalog.");
                }

                if (capital.IsUnlocked && !capital.IsCompleted)
                {
                    incompleteUnlocked++;
                }

                if (string.Equals(capital.CapitalId, state.CurrentCapitalId, StringComparison.Ordinal) && capital.IsUnlocked)
                {
                    currentFound = true;
                }

                bool allComplete = true;
                for (int j = 0; j < capital.Landmarks.Count; j++)
                {
                    LandmarkProgress landmark = capital.Landmarks[j];
                    if (!string.Equals(landmark.LandmarkId, ProgressionCatalog.LandmarkIds[j], StringComparison.Ordinal) ||
                        landmark.Stage < 0 || landmark.Stage > 5)
                    {
                        throw new InvalidOperationException("Progression landmark state is invalid.");
                    }

                    allComplete &= landmark.Stage == 5;
                }

                if (capital.IsCompleted != allComplete || capital.CompletionRewardClaimed != capital.IsCompleted)
                {
                    throw new InvalidOperationException("Capital completion state does not match its landmarks.");
                }
            }

            if (!currentFound || incompleteUnlocked > 1)
            {
                throw new InvalidOperationException("Progression world unlock state is invalid.");
            }

            for (int i = 0; i < state.BattleRuns.Count; i++)
            {
                BattleRunProgress run = state.BattleRuns[i];
                if (string.IsNullOrWhiteSpace(run.BattleRunId) || run.GoldAwarded < 0 ||
                    (run.RewardGranted && (!run.IsCompleted || run.Eligibility != BattleRewardEligibility.ELIGIBLE)) ||
                    (run.Eligibility == BattleRewardEligibility.PRACTICE && run.GoldAwarded != 0))
                {
                    throw new InvalidOperationException("Battle reward record is invalid.");
                }

                for (int j = i + 1; j < state.BattleRuns.Count; j++)
                {
                    if (string.Equals(run.BattleRunId, state.BattleRuns[j].BattleRunId, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Battle-run ids must be unique.");
                    }
                }
            }
        }

        private static int GoldFor(MatchOutcome outcome) => outcome switch
        {
            MatchOutcome.VICTORY => 300,
            MatchOutcome.TIE => 250,
            MatchOutcome.DEFEAT => 200,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };

        private int FindCapitalIndex(string capitalId)
        {
            for (int i = 0; i < State.Capitals.Count; i++)
            {
                if (string.Equals(State.Capitals[i].CapitalId, capitalId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindBattleRunIndex(string battleRunId)
        {
            for (int i = 0; i < State.BattleRuns.Count; i++)
            {
                if (string.Equals(State.BattleRuns[i].BattleRunId, battleRunId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private void Persist()
        {
            Validate(State);
            _store.Save(ProgressionSaveCodec.Encode(State));
        }

        private static bool AllLandmarksComplete(IReadOnlyList<LandmarkProgress> landmarks)
        {
            for (int i = 0; i < landmarks.Count; i++)
            {
                if (landmarks[i].Stage != 5)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<T> Copy<T>(IReadOnlyList<T> source)
        {
            List<T> copy = new List<T>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                copy.Add(source[i]);
            }

            return copy;
        }
    }
}
