#nullable enable
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

        public static void SmokeGroup5()
        {
            GameObject host = new GameObject("EPOCH Group 5 Smoke");
            EpochAppShell shell = host.AddComponent<EpochAppShell>();
            shell.InitializeForEditorSmoke();
            if (!shell.IsReady)
            {
                throw new System.InvalidOperationException(
                    "The Group 5 shell failed to initialize: " + shell.StartupError);
            }

            string root = Path.GetFullPath("Logs/Group5Approval");
            Directory.CreateDirectory(root);

            EpochMatchController battle = shell.SmokeHostedBattleMount();
            if (!shell.ShellCanvasEnabled || !shell.BattleUsesShellDestination || shell.StickyVisible ||
                shell.Destination != EpochShellDestination.BATTLE || !battle.IsHostedInShell ||
                battle.HasStickyPrimaryAction)
            {
                throw new System.InvalidOperationException(
                    "Group 5 requires a single shell canvas host with no mid-battle sticky primary.");
            }

            if (!battle.HelpOverlayActive)
            {
                throw new System.InvalidOperationException("In-battle Help overlay must still open on mount.");
            }

            string player = battle.PlayerChromeText;
            string snapshot = battle.SnapshotChromeText;
            string turn = battle.HeaderTurnText;
            string seed = battle.SeedChromeText;
            if (!player.Contains("Growth") || !player.Contains("Insight") ||
                !snapshot.Contains("Growth") || !snapshot.Contains("Insight") ||
                player.IndexOf("Gold", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                snapshot.IndexOf("Gold", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                turn.IndexOf("Gold", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new System.InvalidOperationException(
                    "Battle chrome must expose Growth/Insight only and never persistent Gold labels.");
            }

            if (!turn.Contains("TURN") || !turn.Contains("DAWN") || turn.Contains(seed.Replace("SEED  ", string.Empty)) ||
                !seed.StartsWith("SEED  ") || string.IsNullOrWhiteSpace(seed.Substring(6)))
            {
                throw new System.InvalidOperationException(
                    "Seed must remain secondary chrome, not part of the primary TURN/Age header.");
            }

            int initialTurn = shell.Game.Battle!.State.Turn;
            int initialPlayerScore = shell.Game.Battle.State.Player.Score;
            int initialSnapshotScore = shell.Game.Battle.State.Snapshot.Score;
            string initialSeed = shell.Game.Battle.State.Seed.MasterSeed.Text;
            CaptureBattle(shell, root, "reference-battle", 390, 844, new Rect(0, 20, 390, 804));
            if (shell.ResponsiveProfile != EpochResponsiveProfile.REFERENCE ||
                battle.ResponsiveProfile != EpochResponsiveProfile.REFERENCE)
            {
                throw new System.InvalidOperationException("Reference battle capture did not apply REFERENCE profile.");
            }

            CaptureBattle(shell, root, "short-9x16-battle", 360, 640, new Rect(0, 24, 360, 596));
            if (shell.ResponsiveProfile != EpochResponsiveProfile.SHORT ||
                battle.ResponsiveProfile != EpochResponsiveProfile.SHORT)
            {
                throw new System.InvalidOperationException("Short battle capture did not apply SHORT profile.");
            }

            CaptureBattle(shell, root, "tall-battle", 430, 1000, new Rect(0, 44, 430, 922));
            if (shell.ResponsiveProfile != EpochResponsiveProfile.TALL ||
                battle.ResponsiveProfile != EpochResponsiveProfile.TALL)
            {
                throw new System.InvalidOperationException("Tall battle capture did not apply TALL profile.");
            }

            if (shell.Game.Battle.State.Turn != initialTurn ||
                shell.Game.Battle.State.Player.Score != initialPlayerScore ||
                shell.Game.Battle.State.Snapshot.Score != initialSnapshotScore ||
                shell.Game.Battle.State.Seed.MasterSeed.Text != initialSeed ||
                shell.Game.Battle.Turns.Count != 0)
            {
                throw new System.InvalidOperationException(
                    "Responsive battle adaptation mutated the deterministic battle state.");
            }

            Color river = EpochBattleBoardPresentation.LaneSurface(LaneModifier.RIVER);
            Color highland = EpochBattleBoardPresentation.LaneSurface(LaneModifier.HIGHLAND);
            Color coast = EpochBattleBoardPresentation.LaneSurface(LaneModifier.COAST);
            if (river == highland || highland == coast || river == coast)
            {
                throw new System.InvalidOperationException("Modular lane surfaces must remain distinct by modifier.");
            }

            shell.StopHostedPresentationForEditorSmoke();
            CompleteActiveBattle(shell.Game, false);
            shell.ShowResultRewards();
            if (shell.Destination != EpochShellDestination.RESULT || shell.CurrentResultPresentation is null ||
                shell.CurrentResultPresentation.RewardStatus != BattleRewardStatus.CREDITED)
            {
                throw new System.InvalidOperationException(
                    "Battle completion must transition to Result & Rewards through EpochGameSession.");
            }

            CaptureResult(shell, root, "reference-result-after-battle", 390, 844, new Rect(0, 20, 390, 804));

            int beforeReplayGold = shell.Game.Progression.State.Gold;
            shell.BeginReadOnlyReplay();
            if (!shell.IsReplayHosted || !shell.ShellCanvasEnabled || !shell.BattleUsesShellDestination ||
                shell.StickyVisible || shell.Game.Progression.State.Gold != beforeReplayGold)
            {
                throw new System.InvalidOperationException(
                    "Read-only replay must remount under the shared shell canvas without sticky or Gold mutation.");
            }

            CaptureBattle(shell, root, "reference-replay", 390, 844, new Rect(0, 20, 390, 804));
            shell.StopHostedPresentationForEditorSmoke();
            shell.ShowResultRewards();

            shell.RestartSameSeedPractice();
            if (shell.Game.Battle is null || shell.Game.Battle.IsComplete || !shell.ShellCanvasEnabled ||
                !shell.BattleUsesShellDestination || shell.StickyVisible ||
                shell.Game.Progression.State.BattleRuns[shell.Game.Progression.State.BattleRuns.Count - 1].Eligibility !=
                BattleRewardEligibility.PRACTICE)
            {
                throw new System.InvalidOperationException(
                    "Practice restart must host under the shared shell canvas with no sticky primary.");
            }

            CaptureBattle(shell, root, "reference-practice-battle", 390, 844, new Rect(0, 20, 390, 804));
            shell.StopHostedPresentationForEditorSmoke();
            CompleteActiveBattle(shell.Game, false);
            shell.ShowResultRewards();
            if (shell.CurrentResultPresentation is null ||
                shell.CurrentResultPresentation.RewardStatus != BattleRewardStatus.PRACTICE_NO_GOLD)
            {
                throw new System.InvalidOperationException("Practice completion must reach Practice — no Gold result.");
            }

            Object.DestroyImmediate(host);
            Debug.Log("EPOCH Group 5 battle shell adaptation passed. Approval captures: " + root);
        }


        public static void SmokeGroup6a()
        {
            EnsureGroup6aArtImports();

            GameObject host = new GameObject("EPOCH Group 6a Smoke");
            EpochAppShell shell = host.AddComponent<EpochAppShell>();
            shell.InitializeForEditorSmoke();
            if (!shell.IsReady)
            {
                throw new System.InvalidOperationException(
                    "The Group 6a shell failed to initialize: " + shell.StartupError);
            }

            string root = Path.GetFullPath("Logs/Group6aApproval");
            Directory.CreateDirectory(root);

            shell.ShowCapital();
            AssertNamedSprite(shell.transform, "Gold Icon", "icon_gold");
            AssertNamedSprite(shell.transform, "Capital Scene Art", "capital_scene_placeholder");
            AssertNamedSprite(shell.transform, "Landmark Stage Art", "landmark_stage_0");
            shell.CaptureEditorPreview(
                Path.Combine(root, "reference-capital.png"),
                390, 844, new Rect(0, 20, 390, 804), EpochShellPreviewSurface.CAPITAL);

            shell.OpenUpgrade(ProgressionCatalog.LandmarkIds[0]);
            AssertNamedSprite(shell.transform, "Upgrade Modal", "upgrade_modal_chrome", requireSliced: true);
            shell.CaptureEditorPreview(
                Path.Combine(root, "reference-upgrade.png"),
                390, 844, new Rect(0, 20, 390, 804), EpochShellPreviewSurface.UPGRADE);
            shell.DismissOverlay();

            AwardVictoryGold(shell.Game.Progression, 16);
            shell.ShowCapital();
            CompleteAllLandmarksOnCurrentCapital(shell);
            if (!shell.IsCapitalCompletionOpen || shell.LastUpgradeReceipt is null ||
                !shell.LastUpgradeReceipt.CapitalCompleted)
            {
                throw new System.InvalidOperationException("Capital completion modal did not open for 6a art check.");
            }

            AssertNamedSprite(shell.transform, "Capital Completion", "capital_complete_modal", requireSliced: true);
            shell.CaptureCapitalCompletionForEditorSmoke(
                Path.Combine(root, "reference-capital-complete.png"),
                390, 844, new Rect(0, 20, 390, 804));
            shell.DismissOverlay();

            CompleteAllLandmarksOnCurrentCapital(shell);
            shell.DismissOverlay();
            CompleteAllLandmarksOnCurrentCapital(shell);
            if (!shell.IsCapitalCompletionOpen || shell.LastUpgradeReceipt is null ||
                shell.LastUpgradeReceipt.UnlockedCapitalId is not null ||
                !shell.Game.WorldProgression().IsWorldComplete)
            {
                throw new System.InvalidOperationException("World Complete modal path did not open for 6a art check.");
            }

            AssertNamedSprite(shell.transform, "Capital Completion", "world_complete_modal", requireSliced: true);
            shell.CaptureCapitalCompletionForEditorSmoke(
                Path.Combine(root, "reference-world-complete.png"),
                390, 844, new Rect(0, 20, 390, 804));

            Object.DestroyImmediate(host);
            Debug.Log("EPOCH Group 6a art integration smoke passed. Approval captures: " + root);
        }

        public static void SmokeGroup6b()
        {
            EnsureGroup6bArtImports();

            GameObject host = new GameObject("EPOCH Group 6b Smoke");
            EpochAppShell shell = host.AddComponent<EpochAppShell>();
            shell.InitializeForEditorSmoke();
            if (!shell.IsReady)
            {
                throw new System.InvalidOperationException(
                    "The Group 6b shell failed to initialize: " + shell.StartupError);
            }

            string root = Path.GetFullPath("Logs/Group6bApproval");
            Directory.CreateDirectory(root);

            shell.ShowResultPreview();
            AssertNamedSprite(shell.transform, "Result Shell Art", "rr_shell_01");
            shell.CaptureEditorPreview(
                Path.Combine(root, "reference-result-preview.png"),
                390, 844, new Rect(0, 20, 390, 804), EpochShellPreviewSurface.RESULT);

            _ = CompleteNewBattle(shell.Game, "group6b-victory", "0", false);
            AssertResult(shell, MatchOutcome.VICTORY, BattleRewardStatus.CREDITED, 300, 300);
            AssertNamedSprite(shell.transform, "Result Shell Art", "rr_shell_01");
            CaptureResult(shell, root, "reference-result-victory", 390, 844, new Rect(0, 20, 390, 804));

            Object.DestroyImmediate(host);
            Debug.Log("EPOCH Group 6b Result & Rewards art smoke passed. Approval captures: " + root);
        }

        private static void EnsureGroup6bArtImports()
        {
            const string shellPath = "Assets/EPOCH_Visuals/UI/ResultRewards/rr_shell_01.png";
            ConfigureSpriteImport(shellPath, Vector4.zero);
            SyncArtToResources(shellPath);

            string[] soft =
            {
                "Assets/EPOCH_Visuals/UI/ResultRewards/rr_outcome_victory.png",
                "Assets/EPOCH_Visuals/UI/ResultRewards/rr_outcome_tie.png",
                "Assets/EPOCH_Visuals/UI/ResultRewards/rr_outcome_defeat.png",
                "Assets/EPOCH_Visuals/UI/ResultRewards/rr_score.png",
                "Assets/EPOCH_Visuals/UI/ResultRewards/rr_gold_credit.png",
                "Assets/EPOCH_Visuals/UI/ResultRewards/rr_gold_already.png",
                "Assets/EPOCH_Visuals/UI/ResultRewards/rr_practice.png",
                "Assets/EPOCH_Visuals/UI/ResultRewards/rr_continue.png",
                "Assets/EPOCH_Visuals/UI/ResultRewards/rr_replay.png",
                "Assets/EPOCH_Visuals/UI/ResultRewards/rr_copy_seed.png",
                "Assets/EPOCH_Visuals/UI/ResultRewards/rr_practice_restart.png",
                "Assets/EPOCH_Visuals/UI/ResultRewards/rr_seed_row.png",
            };
            foreach (string path in soft)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                ConfigureSpriteImport(path, Vector4.zero);
                SyncArtToResources(path);
            }

            AssetDatabase.Refresh();
        }

        private static void EnsureGroup6aArtImports()
        {
            string[] plain =
            {
                "Assets/EPOCH_Visuals/02_UI/Currency/icon_gold.png",
                "Assets/EPOCH_Visuals/Capital/capital_scene_placeholder.png",
                "Assets/EPOCH_Visuals/Capital/stage_language/landmark_stage_0.png",
                "Assets/EPOCH_Visuals/Capital/stage_language/landmark_stage_1.png",
                "Assets/EPOCH_Visuals/Capital/stage_language/landmark_stage_2.png",
                "Assets/EPOCH_Visuals/Capital/stage_language/landmark_stage_3.png",
                "Assets/EPOCH_Visuals/Capital/stage_language/landmark_stage_4.png",
                "Assets/EPOCH_Visuals/Capital/stage_language/landmark_stage_5.png",
            };
            foreach (string path in plain)
            {
                ConfigureSpriteImport(path, Vector4.zero);
                SyncArtToResources(path);
            }

            Vector4 modalBorder = new Vector4(72f, 72f, 72f, 72f);
            ConfigureSpriteImport("Assets/EPOCH_Visuals/02_UI/Modal/upgrade_modal_chrome.png", modalBorder);
            ConfigureSpriteImport("Assets/EPOCH_Visuals/02_UI/Modal/capital_complete_modal.png", modalBorder);
            ConfigureSpriteImport("Assets/EPOCH_Visuals/02_UI/Modal/world_complete_modal.png", modalBorder);
            SyncArtToResources("Assets/EPOCH_Visuals/02_UI/Modal/upgrade_modal_chrome.png");
            SyncArtToResources("Assets/EPOCH_Visuals/02_UI/Modal/capital_complete_modal.png");
            SyncArtToResources("Assets/EPOCH_Visuals/02_UI/Modal/world_complete_modal.png");
            AssetDatabase.Refresh();
        }

        private static void ConfigureSpriteImport(string assetPath, Vector4 border)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer is null)
            {
                throw new System.InvalidOperationException("Missing Group 6a texture: " + assetPath);
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (importer.npotScale != TextureImporterNPOTScale.None)
            {
                importer.npotScale = TextureImporterNPOTScale.None;
                changed = true;
            }

            if (importer.spriteBorder != border)
            {
                importer.spriteBorder = border;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void SyncArtToResources(string assetPath)
        {
            Directory.CreateDirectory("Assets/Epoch/Resources/EpochArt");
            string fileName = Path.GetFileName(assetPath);
            string dest = Path.Combine("Assets/Epoch/Resources/EpochArt", fileName).Replace('\\', '/');
            if (!File.Exists(dest) || new FileInfo(assetPath).Length != new FileInfo(dest).Length)
            {
                File.Copy(assetPath, dest, true);
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            TextureImporter destImporter = AssetImporter.GetAtPath(dest) as TextureImporter;
            if (importer is null)
            {
                return;
            }

            if (destImporter is null)
            {
                AssetDatabase.ImportAsset(dest);
                destImporter = AssetImporter.GetAtPath(dest) as TextureImporter;
            }

            if (destImporter is null)
            {
                return;
            }

            destImporter.textureType = TextureImporterType.Sprite;
            destImporter.spriteImportMode = SpriteImportMode.Single;
            destImporter.mipmapEnabled = false;
            destImporter.alphaIsTransparency = true;
            destImporter.npotScale = TextureImporterNPOTScale.None;
            destImporter.spriteBorder = importer.spriteBorder;
            destImporter.SaveAndReimport();
        }

        private static void AssertNamedSprite(
            Transform root,
            string objectName,
            string spriteNameContains,
            bool requireSliced = false)
        {
            Transform target = FindDeep(root, objectName);
            if (target is null)
            {
                throw new System.InvalidOperationException("Missing UI node for Group 6a art: " + objectName);
            }

            UnityEngine.UI.Image image = target.GetComponent<UnityEngine.UI.Image>();
            if (image is null || image.sprite is null ||
                image.sprite.name.IndexOf(spriteNameContains, System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new System.InvalidOperationException(
                    "Group 6a expected sprite '" + spriteNameContains + "' on " + objectName +
                    " but found " + (image?.sprite != null ? image.sprite.name : "null") + ".");
            }

            if (requireSliced && image.type != UnityEngine.UI.Image.Type.Sliced)
            {
                throw new System.InvalidOperationException(objectName + " chrome must use sliced Image type.");
            }
        }

        private static Transform FindDeep(Transform root, string objectName)
        {
            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), objectName);
                if (found is not null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void CaptureBattle(
            EpochAppShell shell,
            string root,
            string name,
            int width,
            int height,
            Rect safeArea)
        {
            shell.CaptureHostedBattleForEditorSmoke(
                Path.Combine(root, name + ".png"), width, height, safeArea);
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
