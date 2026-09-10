#nullable enable
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Epoch.Presentation
{
    /// <summary>
    /// Group 6a locked G2 art. Editor smoke loads via AssetDatabase; play prefers Resources.
    /// </summary>
    public static class EpochArtCatalog
    {
        public const string VisualsRoot = "Assets/EPOCH_Visuals/";
        public const string ResourcesRoot = "Assets/Epoch/Resources/EpochArt/";

        public static Sprite? LandmarkStage(int stage)
        {
            int clamped = Mathf.Clamp(stage, 0, 5);
            return Load(
                "Capital/stage_language/landmark_stage_" + clamped + ".png",
                "landmark_stage_" + clamped);
        }

        public static Sprite? CapitalScene =>
            Load("Capital/capital_scene_placeholder.png", "capital_scene_placeholder");

        public static Sprite? GoldIcon =>
            Load("02_UI/Currency/icon_gold.png", "icon_gold");

        public static Sprite? UpgradeModalChrome =>
            Load("02_UI/Modal/upgrade_modal_chrome.png", "upgrade_modal_chrome");

        public static Sprite? CapitalCompleteChrome =>
            Load("02_UI/Modal/capital_complete_modal.png", "capital_complete_modal");

        public static Sprite? WorldCompleteChrome =>
            Load("02_UI/Modal/world_complete_modal.png", "world_complete_modal");

        public static void ApplySprite(Image image, Sprite? sprite, bool sliced = false)
        {
            if (image is null || sprite is null)
            {
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced;
        }

        private static Sprite? Load(string visualsRelative, string resourcesName)
        {
#if UNITY_EDITOR
            Sprite? editorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(VisualsRoot + visualsRelative);
            if (editorSprite is not null)
            {
                return editorSprite;
            }
#endif
            return Resources.Load<Sprite>("EpochArt/" + resourcesName);
        }
    }
}
