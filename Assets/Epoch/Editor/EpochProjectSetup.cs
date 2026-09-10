using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Epoch.Application.Progression;
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
