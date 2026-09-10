using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Epoch.Application.Progression;
using Epoch.Application.Runs;
using Epoch.Core.Domain;
using Epoch.Core.Rng;
using Epoch.Presentation;

namespace Epoch.Editor
{
    public static class EpochProjectSetup
    {
        public const string ScenePath = "Assets/Epoch/Scenes/PrototypeMatch.unity";

        [MenuItem("EPOCH/Configure M4 Project")]
        public static void Configure()
        {
            Directory.CreateDirectory("Assets/Epoch/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.productName = "EPOCH";
            PlayerSettings.companyName = "EPOCH Prototype";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);

            AssetDatabase.SaveAssets();
            Debug.Log("EPOCH M4 configured for Unity 6000.3.23f1 at 390x844 portrait reference.");
        }

        public static void SmokePresentation()
        {
            GameObject host = new GameObject("EPOCH M4 Smoke");
            EpochMatchController controller = host.AddComponent<EpochMatchController>();
            controller.InitializeForEditorSmoke();
            if (!controller.IsReady)
            {
                throw new System.InvalidOperationException(
                    "The M4 presentation failed to initialize: " + controller.StartupError);
            }

            Directory.CreateDirectory("Logs");
            controller.CaptureEditorPreview(Path.GetFullPath("Logs/m4-preview.png"));
            Object.DestroyImmediate(host);
            Debug.Log("EPOCH M4 presentation smoke passed: content loaded and the portrait UI initialized.");
        }

        public static void SmokeGroup1()
        {
            GameObject host = new GameObject("EPOCH Group 1 Smoke");
            EpochAppShell shell = host.AddComponent<EpochAppShell>();
            shell.InitializeForEditorSmoke();
            if (!shell.IsReady)
            {
                throw new System.InvalidOperationException(
                    "The Group 1 shell failed to initialize: " + shell.StartupError);
            }

            if (EpochUiTokens.ProfileFor(390, 844) != EpochResponsiveProfile.REFERENCE ||
                EpochUiTokens.ProfileFor(360, 640) != EpochResponsiveProfile.SHORT ||
                EpochUiTokens.ProfileFor(430, 1000) != EpochResponsiveProfile.TALL)
            {
                throw new System.InvalidOperationException("Responsive profile classification failed.");
            }

            if (shell.StickyActionHeight < EpochUiTokens.PrimaryActionHeight)
            {
                throw new System.InvalidOperationException("Sticky primary action is below the 56pt contract.");
            }

            string root = Path.GetFullPath("Logs/Group1Approval");
            CaptureProfile(shell, root, "reference", 390, 844, new Rect(0, 20, 390, 804));
            CaptureProfile(shell, root, "short-9x16", 360, 640, new Rect(0, 24, 360, 596));
            CaptureProfile(shell, root, "tall", 430, 1000, new Rect(0, 44, 430, 922));

            shell.ApplyViewport(new Rect(0, 24, 360, 596), 360, 640);
            if (shell.AppliedSafeArea != new Rect(0, 24, 360, 596))
            {
                throw new System.InvalidOperationException("Safe-area application failed.");
            }

            shell.Preview(EpochShellPreviewSurface.CAPITAL);
            if (shell.Destination != EpochShellDestination.CAPITAL)
            {
                throw new System.InvalidOperationException("Capital destination walkthrough failed.");
            }

            shell.Preview(EpochShellPreviewSurface.BATTLE);
            if (shell.Destination != EpochShellDestination.BATTLE)
            {
                throw new System.InvalidOperationException("Battle host destination walkthrough failed.");
            }

            shell.Preview(EpochShellPreviewSurface.UPGRADE);
            if (!shell.IsModalOpen || shell.IsSheetOpen)
            {
                throw new System.InvalidOperationException("Upgrade modal shell walkthrough failed.");
            }

            shell.DismissOverlay();
            shell.Preview(EpochShellPreviewSurface.WORLD);
            if (!shell.IsSheetOpen || shell.IsModalOpen)
            {
                throw new System.InvalidOperationException("World sheet shell walkthrough failed.");
            }

            shell.DismissOverlay();
            shell.Preview(EpochShellPreviewSurface.RESULT);
            if (shell.Destination != EpochShellDestination.RESULT)
            {
                throw new System.InvalidOperationException("Result destination walkthrough failed.");
            }

            shell.SmokeHostedBattleStart();

            Object.DestroyImmediate(host);
            Debug.Log("EPOCH Group 1 shell smoke passed. Approval captures: " + root);
        }

        public static void SmokeGroup2()
        {
            GameObject host = new GameObject("EPOCH Group 2 Smoke");
            EpochAppShell shell = host.AddComponent<EpochAppShell>();
            shell.InitializeForEditorSmoke();
            if (!shell.IsReady)
            {
                throw new System.InvalidOperationException(
                    "The Group 2 shell failed to initialize: " + shell.StartupError);
            }

            CapitalHomePresentation initial = shell.CurrentCapitalPresentation!;
            if (initial.Gold != 0 || initial.Landmarks.Count != 5 || !initial.CanStartNewBattle)
            {
                throw new System.InvalidOperationException(
                    "The new-save Capital projection or banked-Gold Battle action is incorrect.");
            }

            shell.OpenUpgrade(ProgressionCatalog.LandmarkIds[0]);
            LandmarkUpgradePresentation unaffordable = shell.CurrentUpgradePresentation!;
            if (unaffordable.CanUpgrade || unaffordable.GoldAfterUpgrade.HasValue)
            {
                throw new System.InvalidOperationException("The unaffordable modal state is incorrect.");
            }

            shell.ConfirmUpgrade();
            if (shell.Game.Progression.State.Gold != 0 ||
                shell.Game.Progression.State.Capitals[0].Landmarks[0].Stage != 0)
            {
                throw new System.InvalidOperationException("An unaffordable confirmation mutated progression.");
            }

            string root = Path.GetFullPath("Logs/Group2Approval");
            shell.CaptureEditorPreview(
                Path.Combine(root, "reference-upgrade-unaffordable.png"),
                390, 844, new Rect(0, 20, 390, 804), EpochShellPreviewSurface.UPGRADE);

            AwardVictoryGold(shell.Game.Progression, 4);
            shell.ShowCapital();
            CapitalHomePresentation funded = shell.CurrentCapitalPresentation!;
            if (funded.Gold != 1200 || !funded.CanStartNewBattle || !funded.Landmarks[0].CanAffordNextStage)
            {
                throw new System.InvalidOperationException("Funded Capital projection is incorrect.");
            }

            int[] firstLandmarkCosts = { 20, 30, 40, 50, 60 };
            shell.OpenUpgrade(ProgressionCatalog.LandmarkIds[0]);
            for (int stage = 0; stage < firstLandmarkCosts.Length; stage++)
            {
                LandmarkUpgradePresentation before = shell.CurrentUpgradePresentation!;
                if (before.Stage != stage || before.NextCost != firstLandmarkCosts[stage] || !before.CanUpgrade)
                {
                    throw new System.InvalidOperationException("The modal did not expose the next sequential upgrade.");
                }

                shell.ConfirmUpgrade();
                LandmarkUpgradeReceipt receipt = shell.LastUpgradeReceipt!;
                if (receipt.Cost != firstLandmarkCosts[stage] || receipt.NewStage != stage + 1)
                {
                    throw new System.InvalidOperationException("The confirmed upgrade receipt is incorrect.");
                }
            }

            shell.DismissOverlay();
            shell.ShowCapital();
            CapitalHomePresentation banked = shell.CurrentCapitalPresentation!;
            if (banked.Gold != 1000 || !banked.CanStartNewBattle || banked.Landmarks[0].Stage != 5)
            {
                throw new System.InvalidOperationException("Sequential upgrades did not preserve banked Gold or Battle access.");
            }

            for (int landmark = 1; landmark < ProgressionCatalog.LandmarkIds.Count; landmark++)
            {
                shell.OpenUpgrade(ProgressionCatalog.LandmarkIds[landmark]);
                for (int stage = 0; stage < 5; stage++)
                {
                    shell.ConfirmUpgrade();
                }

                if (landmark < ProgressionCatalog.LandmarkIds.Count - 1)
                {
                    shell.DismissOverlay();
                }
            }

            LandmarkUpgradeReceipt completion = shell.LastUpgradeReceipt!;
            CapitalHomePresentation nextCapital = shell.CurrentCapitalPresentation!;
            if (!completion.CapitalCompleted || completion.CompletionGoldAwarded != 250 ||
                completion.UnlockedCapitalId != "capital_02" || completion.GoldBalance != 450 ||
                !shell.IsCapitalCompletionOpen || nextCapital.CurrentCapitalId != "capital_02")
            {
                throw new System.InvalidOperationException(
                    "Capital completion reward, unlock, or next-capital auto-focus is incorrect.");
            }

            shell.DismissOverlay();
            shell.Game.SelectCapital("capital_01");
            shell.ShowCapital();
            shell.OpenUpgrade(ProgressionCatalog.LandmarkIds[0]);
            int completedBalance = shell.Game.Progression.State.Gold;
            shell.ConfirmUpgrade();
            if (shell.Game.Progression.State.Gold != completedBalance ||
                shell.CurrentUpgradePresentation is null || !shell.CurrentUpgradePresentation.IsComplete)
            {
                throw new System.InvalidOperationException("A completed capital could be rewarded or upgraded twice.");
            }

            shell.DismissOverlay();
            shell.Game.SelectCapital("capital_02");
            shell.ShowCapital();
            CaptureGroup2Upgrade(shell, root, "reference", 390, 844, new Rect(0, 20, 390, 804));
            CaptureGroup2Upgrade(shell, root, "short-9x16", 360, 640, new Rect(0, 24, 360, 596));
            CaptureGroup2Upgrade(shell, root, "tall", 430, 1000, new Rect(0, 44, 430, 922));

            Object.DestroyImmediate(host);
            Debug.Log("EPOCH Group 2 progression walkthrough passed. Approval captures: " + root);
        }

        public static void SmokeGroup3()
        {
            GameObject host = new GameObject("EPOCH Group 3 Smoke");
            EpochAppShell shell = host.AddComponent<EpochAppShell>();
            shell.InitializeForEditorSmoke();
            if (!shell.IsReady)
            {
                throw new System.InvalidOperationException(
                    "The Group 3 shell failed to initialize: " + shell.StartupError);
            }

            string root = Path.GetFullPath("Logs/Group3Approval");
            PlayableMatchSession victoryBattle = CompleteNewBattle(shell.Game, "group3-victory", "0", false);
            string victoryHash = victoryBattle.Turns[victoryBattle.Turns.Count - 1].StateHash;
            AssertResult(shell, MatchOutcome.VICTORY, BattleRewardStatus.CREDITED, 300, 300);

            shell.CopyResultSeed();
            if (shell.LastCopiedSeed != "0" || GUIUtility.systemCopyBuffer != "0")
            {
                throw new System.InvalidOperationException("Copy Seed did not write the result seed to the clipboard.");
            }

            int beforeReplayGold = shell.Game.Progression.State.Gold;
            shell.BeginReadOnlyReplay();
            if (!shell.IsReplayHosted || shell.Game.ResultRewards is null ||
                shell.Game.Progression.State.Gold != beforeReplayGold || !victoryBattle.VerifyReplay() ||
                victoryBattle.Turns[victoryBattle.Turns.Count - 1].StateHash != victoryHash)
            {
                throw new System.InvalidOperationException("Read-only Replay mutated or failed to mount the recorded battle.");
            }

            shell.StopHostedPresentationForEditorSmoke();
            shell.ShowResultRewards();
            CaptureResult(shell, root, "reference-victory", 390, 844, new Rect(0, 20, 390, 804));
            shell.ContinueToCapital();
            AssertContinuedToCapital(shell, 300);

            _ = CompleteNewBattle(shell.Game, "group3-defeat", "2", false);
            AssertResult(shell, MatchOutcome.DEFEAT, BattleRewardStatus.CREDITED, 200, 500);
            CaptureResult(shell, root, "reference-defeat", 390, 844, new Rect(0, 20, 390, 804));
            shell.ContinueToCapital();
            AssertContinuedToCapital(shell, 500);

            _ = CompleteNewBattle(shell.Game, "group3-tie", "0", true);
            AssertResult(shell, MatchOutcome.TIE, BattleRewardStatus.CREDITED, 250, 750);
            CaptureResult(shell, root, "reference-tie", 390, 844, new Rect(0, 20, 390, 804));
            shell.ContinueToCapital();
            AssertContinuedToCapital(shell, 750);

            shell.Game.RestoreCompletedResult("group3-victory", victoryBattle);
            AssertResult(shell, MatchOutcome.VICTORY, BattleRewardStatus.ALREADY_CREDITED, 300, 750);
            if (shell.Game.ResultRewards!.GoldCreditedNow != 0)
            {
                throw new System.InvalidOperationException("Reopening a result exposed a duplicate Gold credit.");
            }

            CaptureResult(shell, root, "reference-already-credited", 390, 844, new Rect(0, 20, 390, 804));
            int beforePracticeGold = shell.Game.Progression.State.Gold;
            shell.RestartSameSeedPractice();
            if (shell.Game.Battle is null || shell.Game.Battle.IsComplete ||
                shell.Game.Progression.State.BattleRuns[shell.Game.Progression.State.BattleRuns.Count - 1].Eligibility !=
                BattleRewardEligibility.PRACTICE)
            {
                throw new System.InvalidOperationException("Restart Same Seed did not create a hosted practice battle.");
            }

            shell.StopHostedPresentationForEditorSmoke();
            CompleteActiveBattle(shell.Game, false);
            AssertResult(shell, MatchOutcome.VICTORY, BattleRewardStatus.PRACTICE_NO_GOLD, 0, beforePracticeGold);
            if (shell.Game.ResultRewards!.Seed != "0" || shell.Game.Progression.State.Gold != beforePracticeGold ||
                shell.Game.ResultRewards.GoldCreditedNow != 0 || !shell.Game.Battle!.VerifyReplay())
            {
                throw new System.InvalidOperationException("Practice completion changed Gold, seed, or replay integrity.");
            }

            CaptureResult(shell, root, "reference-practice", 390, 844, new Rect(0, 20, 390, 804));
            CaptureResult(shell, root, "short-9x16-practice", 360, 640, new Rect(0, 24, 360, 596));
            CaptureResult(shell, root, "tall-practice", 430, 1000, new Rect(0, 44, 430, 922));
            shell.ContinueToCapital();
            AssertContinuedToCapital(shell, beforePracticeGold);

            Object.DestroyImmediate(host);
            Debug.Log("EPOCH Group 3 result/reward walkthrough passed. Approval captures: " + root);
        }

        public static void SmokeGroup4()
        {
            GameObject host = new GameObject("EPOCH Group 4 Smoke");
            EpochAppShell shell = host.AddComponent<EpochAppShell>();
            shell.InitializeForEditorSmoke();
            if (!shell.IsReady)
            {
                throw new System.InvalidOperationException(
                    "The Group 4 shell failed to initialize: " + shell.StartupError);
            }

            CapitalHomePresentation initial = shell.CurrentCapitalPresentation!;
            if (initial.CurrentCapitalId != "capital_01" || shell.Destination != EpochShellDestination.CAPITAL ||
                shell.StickyActionHeight < 56f)
            {
                throw new System.InvalidOperationException(
                    "Fresh shell must start on capital_01 with sticky CAPITAL actions.");
            }

            shell.OpenWorldForEditor();
            WorldProgressionPresentation world = shell.CurrentWorldPresentation!;
            if (world.Capitals.Count != 3 || world.IsWorldComplete ||
                !world.Capitals[0].IsUnlocked || !world.Capitals[0].IsCurrent || world.Capitals[0].IsCompleted ||
                world.Capitals[1].IsUnlocked || world.Capitals[2].IsUnlocked || !shell.IsSheetOpen)
            {
                throw new System.InvalidOperationException(
                    "Fresh World sheet must show capital_01 current and capitals 02/03 locked.");
            }

            bool lockedRejected = false;
            try
            {
                shell.Game.SelectCapital("capital_02");
            }
            catch (System.InvalidOperationException)
            {
                lockedRejected = true;
            }

            if (!lockedRejected)
            {
                throw new System.InvalidOperationException("Locked capital selection must be rejected.");
            }

            shell.DismissOverlay();
            string root = Path.GetFullPath("Logs/Group4Approval");
            shell.CaptureWorldForEditorSmoke(
                Path.Combine(root, "reference-world-locked.png"),
                390, 844, new Rect(0, 20, 390, 804));
            shell.DismissOverlay();

            // Cap1=1000, Cap2=1750, Cap3=2500; completion returns 250/350/500.
            // Need start gold >= 4650; 16 victories * 300 = 4800.
            AwardVictoryGold(shell.Game.Progression, 16);
            shell.ShowCapital();

            CompleteAllLandmarksOnCurrentCapital(shell);
            LandmarkUpgradeReceipt capital1 = shell.LastUpgradeReceipt!;
            CapitalHomePresentation after1 = shell.CurrentCapitalPresentation!;
            if (!capital1.CapitalCompleted || capital1.CompletionGoldAwarded != 250 ||
                capital1.UnlockedCapitalId != "capital_02" || after1.CurrentCapitalId != "capital_02" ||
                !shell.IsCapitalCompletionOpen)
            {
                throw new System.InvalidOperationException(
                    "Capital 01 completion must award 250, unlock capital_02, and auto-focus.");
            }

            shell.CaptureCapitalCompletionForEditorSmoke(
                Path.Combine(root, "reference-capital01-complete.png"),
                390, 844, new Rect(0, 20, 390, 804));
            shell.DismissOverlay();
            if (shell.IsCapitalCompletionOpen || shell.CurrentCapitalPresentation!.CurrentCapitalId != "capital_02")
            {
                throw new System.InvalidOperationException(
                    "CONTINUE must leave the player on the auto-focused capital.");
            }

            shell.OpenWorld();
            WorldProgressionPresentation mid = shell.CurrentWorldPresentation!;
            if (!mid.Capitals[0].IsCompleted || !mid.Capitals[0].IsUnlocked ||
                !mid.Capitals[1].IsUnlocked || !mid.Capitals[1].IsCurrent || mid.Capitals[2].IsUnlocked)
            {
                throw new System.InvalidOperationException(
                    "After capital_01, World must show completed revisitable 01 and current unlocked 02.");
            }

            shell.SelectWorldCapital("capital_01");
            if (shell.Destination != EpochShellDestination.CAPITAL ||
                shell.CurrentCapitalPresentation!.CurrentCapitalId != "capital_01" ||
                !shell.CurrentCapitalPresentation.IsCapitalCompleted || shell.IsSheetOpen)
            {
                throw new System.InvalidOperationException(
                    "Completed capital_01 must be revisitable as a read-only capital placeholder.");
            }

            shell.SelectWorldCapital("capital_02");
            if (shell.CurrentCapitalPresentation!.CurrentCapitalId != "capital_02" ||
                shell.CurrentCapitalPresentation.IsCapitalCompleted)
            {
                throw new System.InvalidOperationException("Selecting capital_02 must restore the in-progress focus.");
            }

            CompleteAllLandmarksOnCurrentCapital(shell);
            LandmarkUpgradeReceipt capital2 = shell.LastUpgradeReceipt!;
            if (!capital2.CapitalCompleted || capital2.CompletionGoldAwarded != 350 ||
                capital2.UnlockedCapitalId != "capital_03" ||
                shell.CurrentCapitalPresentation!.CurrentCapitalId != "capital_03" ||
                !shell.IsCapitalCompletionOpen)
            {
                throw new System.InvalidOperationException(
                    "Capital 02 completion must award 350, unlock capital_03, and auto-focus.");
            }

            shell.DismissOverlay();
            CompleteAllLandmarksOnCurrentCapital(shell);
            LandmarkUpgradeReceipt capital3 = shell.LastUpgradeReceipt!;
            WorldProgressionPresentation finished = shell.Game.WorldProgression();
            if (!capital3.CapitalCompleted || capital3.CompletionGoldAwarded != 500 ||
                capital3.UnlockedCapitalId is not null || !finished.IsWorldComplete ||
                !shell.IsCapitalCompletionOpen)
            {
                throw new System.InvalidOperationException(
                    "Capital 03 completion must award 500, leave UnlockedCapitalId null, and mark World Complete.");
            }

            shell.CaptureCapitalCompletionForEditorSmoke(
                Path.Combine(root, "reference-world-complete.png"),
                390, 844, new Rect(0, 20, 390, 804));
            shell.DismissOverlay();
            if (shell.CurrentCapitalPresentation!.CurrentCapitalId != "capital_03")
            {
                throw new System.InvalidOperationException(
                    "World Complete CONTINUE should remain on the final capital focus.");
            }

            shell.OpenWorld();
            WorldProgressionPresentation revisit = shell.CurrentWorldPresentation!;
            if (!revisit.IsWorldComplete || revisit.Capitals.Count != 3 ||
                !revisit.Capitals[0].IsCompleted || !revisit.Capitals[1].IsCompleted ||
                !revisit.Capitals[2].IsCompleted ||
                !revisit.Capitals[0].IsUnlocked || !revisit.Capitals[1].IsUnlocked ||
                !revisit.Capitals[2].IsUnlocked)
            {
                throw new System.InvalidOperationException(
                    "After World Complete, all three capitals must remain unlocked and revisitable.");
            }

            shell.CaptureWorldForEditorSmoke(
                Path.Combine(root, "reference-world-revisitable.png"),
                390, 844, new Rect(0, 20, 390, 804));
            shell.CaptureWorldForEditorSmoke(
                Path.Combine(root, "short-9x16-world-revisitable.png"),
                360, 640, new Rect(0, 24, 360, 596));
            shell.CaptureWorldForEditorSmoke(
                Path.Combine(root, "tall-world-revisitable.png"),
                430, 1000, new Rect(0, 44, 430, 922));

            shell.SelectWorldCapital("capital_01");
            shell.SelectWorldCapital("capital_02");
            shell.SelectWorldCapital("capital_03");
            if (shell.Destination != EpochShellDestination.CAPITAL ||
                shell.CurrentCapitalPresentation!.CurrentCapitalId != "capital_03" ||
                shell.StickyActionHeight < 56f)
            {
                throw new System.InvalidOperationException(
                    "Post-world-complete revisits must keep CAPITAL destination and sticky actions.");
            }

            Object.DestroyImmediate(host);
            Debug.Log("EPOCH Group 4 world progression walkthrough passed. Approval captures: " + root);
        }

        private static void CompleteAllLandmarksOnCurrentCapital(EpochAppShell shell)
        {
            for (int landmark = 0; landmark < ProgressionCatalog.LandmarkIds.Count; landmark++)
            {
                shell.OpenUpgrade(ProgressionCatalog.LandmarkIds[landmark]);
                for (int stage = 0; stage < 5; stage++)
                {
                    LandmarkUpgradePresentation before = shell.CurrentUpgradePresentation!;
                    if (!before.CanUpgrade)
                    {
                        throw new System.InvalidOperationException(
                            "Expected an affordable upgrade on " + before.LandmarkId + " stage " + before.Stage + ".");
                    }

                    shell.ConfirmUpgrade();
                }

                if (landmark < ProgressionCatalog.LandmarkIds.Count - 1)
                {
                    if (shell.IsCapitalCompletionOpen)
                    {
                        throw new System.InvalidOperationException(
                            "Capital completion opened before the final landmark finished.");
                    }

                    shell.DismissOverlay();
                }
            }
        }

        private static PlayableMatchSession CompleteNewBattle(
            EpochGameSession game,
            string battleRunId,
            string seed,
            bool chooseLastLegal)
        {
            _ = game.StartNewBattle(new SeedCode(seed), battleRunId);
            CompleteActiveBattle(game, chooseLastLegal);
            return game.Battle!;
        }

        private static void CompleteActiveBattle(EpochGameSession game, bool chooseLastLegal)
        {
            while (!game.Battle!.IsComplete)
            {
                if (game.Battle.IsForcedPass)
                {
                    _ = game.SubmitForcedPass();
                    continue;
                }

                System.Collections.Generic.IReadOnlyList<CardPresentation> cards = game.Battle.Cards();
                int start = chooseLastLegal ? cards.Count - 1 : 0;
                int end = chooseLastLegal ? -1 : cards.Count;
                int step = chooseLastLegal ? -1 : 1;
                bool submitted = false;
                for (int i = start; i != end; i += step)
                {
                    CardPresentation card = cards[i];
                    if (!card.IsLegal)
                    {
                        continue;
                    }

                    Epoch.Core.Domain.Selection selection = card.Definition.CardType == CardType.ADVANCE
                        ? new Epoch.Core.Domain.Selection(card.OfferIndex, null)
                        : new Epoch.Core.Domain.Selection(card.OfferIndex, card.LegalLanes[0]);
                    _ = game.SubmitBattleSelection(selection);
                    submitted = true;
                    break;
                }

                if (!submitted)
                {
                    throw new System.InvalidOperationException("The result walkthrough found no legal player action.");
                }
            }
        }

        private static void AssertResult(
            EpochAppShell shell,
            MatchOutcome outcome,
            BattleRewardStatus status,
            int reward,
            int goldBalance)
        {
            shell.ShowResultRewards();
            ResultRewardsPresentation result = shell.CurrentResultPresentation!;
            if (result.Outcome != outcome || result.RewardStatus != status ||
                result.RewardGold != reward || result.GoldBalance != goldBalance ||
                !result.CanContinueToCapital || !result.CanReplay || !result.CanCopySeed ||
                !result.CanRestartSameSeedPractice || shell.StickyActionHeight < 56f)
            {
                throw new System.InvalidOperationException("Result & Rewards projection or action hierarchy is incorrect.");
            }
        }

        private static void AssertContinuedToCapital(EpochAppShell shell, int expectedGold)
        {
            if (shell.Destination != EpochShellDestination.CAPITAL || shell.Game.ResultRewards is not null ||
                shell.CurrentCapitalPresentation is null || shell.CurrentCapitalPresentation.Gold != expectedGold)
            {
                throw new System.InvalidOperationException("Continue to Capital did not restore the Capital projection.");
            }
        }

        private static void CaptureResult(
            EpochAppShell shell,
            string root,
            string name,
            int width,
            int height,
            Rect safeArea)
        {
            shell.CaptureCurrentResultForEditorSmoke(
                Path.Combine(root, name + ".png"), width, height, safeArea);
        }

        private static void AwardVictoryGold(ProgressionService progression, int battleCount)
        {
            for (int i = 0; i < battleCount; i++)
            {
                string runId = "group2-funding-" + i;
                _ = progression.StartBattle(runId, BattleRewardEligibility.ELIGIBLE);
                _ = progression.CompleteBattle(
                    runId,
                    new MatchResult("group2-smoke", "group2-smoke", new SeedCode("group2"), 1, 0, 24));
            }
        }

        private static void CaptureGroup2Upgrade(
            EpochAppShell shell,
            string root,
            string profile,
            int width,
            int height,
            Rect safeArea)
        {
            shell.CaptureEditorPreview(
                Path.Combine(root, profile + "-upgrade-affordable.png"),
                width, height, safeArea, EpochShellPreviewSurface.UPGRADE);
        }

        private static void CaptureProfile(
            EpochAppShell shell,
            string root,
            string profile,
            int width,
            int height,
            Rect safeArea)
        {
            foreach (EpochShellPreviewSurface surface in System.Enum.GetValues(typeof(EpochShellPreviewSurface)))
            {
                string path = Path.Combine(root, profile + "-" + surface.ToString().ToLowerInvariant() + ".png");
                shell.CaptureEditorPreview(path, width, height, safeArea, surface);
            }
        }
    }
}
