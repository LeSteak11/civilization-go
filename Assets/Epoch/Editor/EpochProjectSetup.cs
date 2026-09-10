using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
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
