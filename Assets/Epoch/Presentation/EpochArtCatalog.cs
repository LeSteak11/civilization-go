#nullable enable
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Epoch.Presentation
{
    /// <summary>
    /// Group 6a capital/modal art + Group 6b Result & Rewards hooks.
    /// Editor smoke loads via AssetDatabase; play prefers Resources.
    /// Missing RR piece art returns null so the shell soft-falls back to wireframe text.
    /// </summary>
    public static class EpochArtCatalog
    {
        public const string VisualsRoot = "Assets/EPOCH_Visuals/";
        public const string ResourcesRoot = "Assets/Epoch/Resources/EpochArt/";
        public const string ResultRewardsVisualFolder = "UI/ResultRewards/";

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

        // --- Group 6b Result & Rewards (RR-*) ---
        // Locked now: RR-SHELL-01. Remaining IDs soft-load when files land.

        public static Sprite? ResultShell =>
            LoadResult("rr_shell_01.png", "rr_shell_01");

        public static Sprite? ResultOutcomeVictory =>
            LoadResult("rr_outcome_victory.png", "rr_outcome_victory");

        public static Sprite? ResultOutcomeTie =>
            LoadResult("rr_outcome_tie.png", "rr_outcome_tie");

        public static Sprite? ResultOutcomeDefeat =>
            LoadResult("rr_outcome_defeat.png", "rr_outcome_defeat");

        public static Sprite? ResultScore =>
            LoadResult("rr_score.png", "rr_score");

        public static Sprite? ResultGoldCredit =>
            LoadResult("rr_gold_credit.png", "rr_gold_credit");

        public static Sprite? ResultGoldAlready =>
            LoadResult("rr_gold_already.png", "rr_gold_already");

        public static Sprite? ResultPractice =>
            LoadResult("rr_practice.png", "rr_practice");

        public static Sprite? ResultContinue =>
            LoadResult("rr_continue.png", "rr_continue");

        public static Sprite? ResultReplay =>
            LoadResult("rr_replay.png", "rr_replay");

        public static Sprite? ResultCopySeed =>
            LoadResult("rr_copy_seed.png", "rr_copy_seed");

        public static Sprite? ResultPracticeRestart =>
            LoadResult("rr_practice_restart.png", "rr_practice_restart");

        public static Sprite? ResultSeedRow =>
            LoadResult("rr_seed_row.png", "rr_seed_row");

        public static Sprite? ResultOutcome(Epoch.Core.Domain.MatchOutcome outcome)
        {
            return outcome switch
            {
                Epoch.Core.Domain.MatchOutcome.VICTORY => ResultOutcomeVictory,
                Epoch.Core.Domain.MatchOutcome.TIE => ResultOutcomeTie,
                Epoch.Core.Domain.MatchOutcome.DEFEAT => ResultOutcomeDefeat,
                _ => null,
            };
        }

        public static Sprite? ResultRewardStatus(Epoch.Application.Progression.BattleRewardStatus status)
        {
            return status switch
            {
                Epoch.Application.Progression.BattleRewardStatus.CREDITED => ResultGoldCredit,
                Epoch.Application.Progression.BattleRewardStatus.ALREADY_CREDITED => ResultGoldAlready,
                Epoch.Application.Progression.BattleRewardStatus.PRACTICE_NO_GOLD => ResultPractice,
                _ => null,
            };
        }

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

        public static bool TryApplySprite(Image image, Sprite? sprite, bool sliced = false)
        {
            if (image is null || sprite is null)
            {
                return false;
            }

            ApplySprite(image, sprite, sliced);
            return true;
        }

        private static Sprite? LoadResult(string fileName, string resourcesName) =>
            Load(ResultRewardsVisualFolder + fileName, resourcesName);

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
