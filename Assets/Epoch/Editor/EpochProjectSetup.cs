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
    }
}
