#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Epoch.Application.Progression;
using Epoch.Content.Loading;
using Epoch.Core.Domain;
using Epoch.Core.Rng;
using Epoch.Core.StateMachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Epoch.Presentation
{
    public enum EpochShellDestination
    {
        CAPITAL,
        BATTLE,
        RESULT,
    }

    public enum EpochShellPreviewSurface
    {
        CAPITAL,
        BATTLE,
        RESULT,
        UPGRADE,
        WORLD,
    }

    public sealed class EpochAppShell : MonoBehaviour
    {
        private Font _font = null!;
        private Canvas _canvas = null!;
        private RectTransform _destinationLayer = null!;
        private RectTransform _overlayLayer = null!;
        private RectTransform _stickyLayer = null!;
        private EpochSafeAreaRoot _safeArea = null!;
        private EpochGameSession _game = null!;
        private GameObject? _matchHost;
        private bool _previewResult;
        private bool _battleLayoutActive;
        private string? _selectedLandmarkId;
        private string _resultNotice = string.Empty;

        public bool IsReady { get; private set; }

        public string StartupError { get; private set; } = string.Empty;

        public EpochShellDestination Destination { get; private set; } = EpochShellDestination.CAPITAL;

        public EpochResponsiveProfile ResponsiveProfile { get; private set; }

        public bool IsModalOpen { get; private set; }

        public bool IsSheetOpen { get; private set; }

        public bool IsCapitalCompletionOpen { get; private set; }

        public float StickyActionHeight => EpochUiTokens.PrimaryActionHeight;

        public Rect AppliedSafeArea => _safeArea.LastSafeArea;

        public EpochGameSession Game => _game;

        public CapitalHomePresentation? CurrentCapitalPresentation { get; private set; }

        public LandmarkUpgradePresentation? CurrentUpgradePresentation { get; private set; }

        public LandmarkUpgradeReceipt? LastUpgradeReceipt { get; private set; }

        public ResultRewardsPresentation? CurrentResultPresentation { get; private set; }

        public WorldProgressionPresentation? CurrentWorldPresentation { get; private set; }

        public string LastCopiedSeed { get; private set; } = string.Empty;

        public bool IsReplayHosted { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<EpochAppShell>() is null)
            {
                new GameObject("EPOCH Â· UI Shell").AddComponent<EpochAppShell>();
            }
        }

        private void Awake()
        {
            if (IsReady)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            UnityEngine.Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureCamera();
            EnsureEventSystem();
            BuildShell();

            try
            {
                string contentPath = Path.Combine(
                    UnityEngine.Application.dataPath, "..", "_aiinfodocs", "data", "epoch_v1_content.json");
                var content = ContentLoader.Load(
                    File.ReadAllText(Path.GetFullPath(contentPath)), RunFactory.RulesVersion);
                ProgressionStore store = UnityEngine.Application.isPlaying
                    ? new FileProgressionStore(Path.Combine(UnityEngine.Application.persistentDataPath, "epoch-progression.save"))
                    : new ShellMemoryProgressionStore();
                _game = new EpochGameSession(content, store);
                ApplyViewport(Screen.safeArea, Screen.width, Screen.height);
                ShowCapital();
                IsReady = true;
            }
            catch (Exception exception)
            {
                StartupError = exception.ToString();
                Debug.LogException(exception);
                ShowFatal(exception.Message);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                DismissOverlay();
            }
        }

        public void ApplyViewport(Rect safeArea, int screenWidth, int screenHeight)
        {
            _safeArea.Apply(safeArea, screenWidth, screenHeight);
            ResponsiveProfile = EpochUiTokens.ProfileFor(screenWidth, screenHeight);
            ApplyDestinationOffsets();
            _stickyLayer.anchorMin = Vector2.zero;
            _stickyLayer.anchorMax = new Vector2(1f, 0f);
            _stickyLayer.pivot = new Vector2(0.5f, 0f);
            _stickyLayer.offsetMin = new Vector2(EpochUiTokens.Gutter, EpochUiTokens.SpaceSmall);
            _stickyLayer.offsetMax = new Vector2(-EpochUiTokens.Gutter, EpochUiTokens.StickyRegionHeight);
            if (_matchHost is not null)
            {
                EpochMatchController? controller = _matchHost.GetComponent<EpochMatchController>();
                controller?.ApplyResponsiveProfile(ResponsiveProfile);
            }
        }

        public void ShowCapital()
        {
            Destination = EpochShellDestination.CAPITAL;
            _previewResult = false;
            ExitBattleLayoutMode();
            CloseOverlay();
            EpochUiFactory.Clear(_destinationLayer);
            EpochUiFactory.Clear(_stickyLayer);
            CapitalHomePresentation state = _game.CapitalHome();
            CurrentCapitalPresentation = state;

            float compact = ResponsiveProfile == EpochResponsiveProfile.SHORT ? 0.05f : 0f;
            RectTransform header = EpochUiFactory.Panel(
                "Capital Header", _destinationLayer, EpochUiTokens.Surface,
                new Vector2(0f, 0.82f + compact), Vector2.one);
            EpochUiFactory.Text(
                "Capital Title", "CAPITAL / " + state.CurrentCapitalId,
                header, _font, EpochUiTokens.TextHeading, EpochUiTokens.Text,
                TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(0.05f, 0f), new Vector2(0.66f, 1f));
            EpochUiFactory.SpriteImage(
                "Gold Icon", header, EpochArtCatalog.GoldIcon,
                new Vector2(0.66f, 0.22f), new Vector2(0.74f, 0.78f));
            EpochUiFactory.Text(
                "Gold", "GOLD  " + state.Gold,
                header, _font, EpochUiTokens.TextBody, EpochUiTokens.Accent,
                TextAnchor.MiddleRight, FontStyle.Bold, new Vector2(0.74f, 0f), new Vector2(0.95f, 1f));

            EpochUiFactory.Text(
                "Progress", state.CompletedUpgradeCount + " / 25 UPGRADES",
                _destinationLayer, _font, EpochUiTokens.TextBody, EpochUiTokens.TextMuted,
                TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(0.04f, 0.73f + compact), new Vector2(0.62f, 0.81f + compact));
            EpochUiFactory.Button(
                "World", "WORLD", _destinationLayer, _font, EpochButtonStyle.SECONDARY,
                OpenWorld, new Vector2(0.68f, 0.73f + compact), new Vector2(0.96f, 0.81f + compact));

            float top = ResponsiveProfile == EpochResponsiveProfile.SHORT ? 0.69f : 0.70f;
            float rowHeight = ResponsiveProfile == EpochResponsiveProfile.SHORT ? 0.105f : 0.115f;
            for (int i = 0; i < state.Landmarks.Count; i++)
            {
                LandmarkPresentation landmark = state.Landmarks[i];
                float yMax = top - i * rowHeight;
                float yMin = yMax - rowHeight + 0.012f;
                string status = landmark.Stage == 5
                    ? "COMPLETE"
                    : landmark.CanAffordNextStage
                        ? "READY · " + landmark.NextCost + " GOLD"
                        : "NEED " + landmark.NextCost + " GOLD";
                string id = landmark.LandmarkId;
                RectTransform row = EpochUiFactory.Panel(
                    "Landmark " + (i + 1), _destinationLayer, EpochUiTokens.SurfaceRaised,
                    new Vector2(0.04f, yMin), new Vector2(0.96f, yMax));
                Button rowButton = row.gameObject.AddComponent<Button>();
                ColorBlock colors = rowButton.colors;
                colors.normalColor = EpochUiTokens.SurfaceRaised;
                colors.highlightedColor = new Color(0.22f, 0.28f, 0.34f, 1f);
                colors.pressedColor = new Color(0.12f, 0.16f, 0.20f, 1f);
                colors.selectedColor = EpochUiTokens.SurfaceRaised;
                colors.disabledColor = EpochUiTokens.Disabled;
                rowButton.colors = colors;
                rowButton.onClick.AddListener(() => OpenUpgrade(id));
                EpochUiFactory.SpriteImage(
                    "Landmark Stage Art", row, EpochArtCatalog.LandmarkStage(landmark.Stage),
                    new Vector2(0.02f, 0.10f), new Vector2(0.16f, 0.90f));
                EpochUiFactory.Text(
                    "Label",
                    "LANDMARK " + (i + 1) + "     STAGE " + landmark.Stage + "/5     " + status,
                    row, _font, EpochUiTokens.TextSmall, EpochUiTokens.Text,
                    TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(0.18f, 0f), new Vector2(0.98f, 1f));
                if (landmark.Stage == 5)
                {
                    Outline outline = row.gameObject.AddComponent<Outline>();
                    outline.effectColor = EpochUiTokens.Player;
                    outline.effectDistance = new Vector2(3f, -3f);
                }
            }

            float sceneTop = ResponsiveProfile == EpochResponsiveProfile.SHORT ? 0.14f : 0.13f;
            EpochUiFactory.SpriteImage(
                "Capital Scene Art", _destinationLayer, EpochArtCatalog.CapitalScene,
                new Vector2(0.08f, 0.02f), new Vector2(0.92f, sceneTop));
            string capitalStatus = state.IsCapitalCompleted
                ? (_game.WorldProgression().IsWorldComplete
                    ? "WORLD COMPLETE"
                    : "COMPLETED CAPITAL")
                : "CAPITAL";
            EpochUiFactory.Text(
                "Capital Placeholder", capitalStatus,
                _destinationLayer, _font, EpochUiTokens.TextSmall, EpochUiTokens.TextMuted,
                TextAnchor.LowerCenter, FontStyle.Bold, new Vector2(0.10f, 0.02f), new Vector2(0.90f, sceneTop));
StickyPrimary("START NEW BATTLE", StartBattle, state.CanStartNewBattle);
        }

        public void OpenUpgrade(string landmarkId)
        {
            LandmarkUpgradePresentation state = _game.LandmarkUpgrade(landmarkId);
            CloseOverlay();
            _selectedLandmarkId = landmarkId;
            CurrentUpgradePresentation = state;
            IsModalOpen = true;
            _overlayLayer.gameObject.SetActive(true);
            RectTransform dimmer = EpochUiFactory.Panel("Overlay Dimmer", _overlayLayer, EpochUiTokens.Dimmer, Vector2.zero, Vector2.one);
            Button dismiss = dimmer.gameObject.AddComponent<Button>();
            dismiss.onClick.AddListener(DismissOverlay);
            RectTransform modal = EpochUiFactory.Panel(
                "Upgrade Modal", _overlayLayer, Color.white,
                new Vector2(0.06f, ResponsiveProfile == EpochResponsiveProfile.SHORT ? 0.08f : 0.17f),
                new Vector2(0.94f, ResponsiveProfile == EpochResponsiveProfile.SHORT ? 0.92f : 0.83f));
            EpochArtCatalog.ApplySprite(modal.GetComponent<Image>(), EpochArtCatalog.UpgradeModalChrome, sliced: true);
            EpochUiFactory.Text(
                "Modal Title", "LANDMARK UPGRADE",
                modal, _font, EpochUiTokens.TextHeading, EpochUiTokens.Text,
                TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.98f));
            _ = EpochUiFactory.ScrollArea(
                "Modal Body Scroll", modal, new Vector2(0.06f, 0.23f), new Vector2(0.94f, 0.80f), out RectTransform body);
            EpochUiFactory.Text(
                "Modal Body",
                state.LandmarkId + "\n\nSTAGE " + state.Stage + " / 5" +
                (state.IsComplete ? "\n\nLANDMARK COMPLETE" :
                    "\n\nNEXT STAGE  " + (state.Stage + 1) + " / 5" +
                    "\n\nCOST  " + state.NextCost + " GOLD" +
                    "\n\nBALANCE  " + state.Gold + " â†’ " +
                    (state.GoldAfterUpgrade.HasValue ? state.GoldAfterUpgrade.Value.ToString() : "INSUFFICIENT") + " GOLD") +
                "",
                body, _font, EpochUiTokens.TextBody, EpochUiTokens.Text,
                TextAnchor.UpperCenter, FontStyle.Normal, Vector2.zero, Vector2.one);
            EpochUiFactory.Button(
                "Close Modal", "CLOSE", modal, _font, EpochButtonStyle.SECONDARY,
                DismissOverlay, new Vector2(0.05f, 0.05f), new Vector2(0.39f, 0.19f));
            EpochUiFactory.Button(
                "Confirm Upgrade",
                state.IsComplete ? "COMPLETE" : "UPGRADE Â· " + state.NextCost + " GOLD",
                modal, _font, EpochButtonStyle.PRIMARY,
                ConfirmUpgrade, new Vector2(0.42f, 0.05f), new Vector2(0.95f, 0.19f), state.CanUpgrade);
        }

        public void ConfirmUpgrade()
        {
            if (_selectedLandmarkId is null)
            {
                return;
            }

            LandmarkUpgradePresentation state = _game.LandmarkUpgrade(_selectedLandmarkId);
            if (!state.CanUpgrade)
            {
                return;
            }

            string landmarkId = _selectedLandmarkId;
            LandmarkUpgradeReceipt receipt = _game.UpgradeLandmark(landmarkId);
            LastUpgradeReceipt = receipt;
            ShowCapital();
            if (receipt.CapitalCompleted)
            {
                ShowCapitalCompletion(receipt);
            }
            else
            {
                OpenUpgrade(landmarkId);
            }
        }

        public void OpenWorld()
        {
            WorldProgressionPresentation state = _game.WorldProgression();
            CurrentWorldPresentation = state;
            CloseOverlay();
            IsSheetOpen = true;
            _overlayLayer.gameObject.SetActive(true);
            RectTransform dimmer = EpochUiFactory.Panel("Overlay Dimmer", _overlayLayer, EpochUiTokens.Dimmer, Vector2.zero, Vector2.one);
            Button dismiss = dimmer.gameObject.AddComponent<Button>();
            dismiss.onClick.AddListener(DismissOverlay);
            float sheetTop = ResponsiveProfile == EpochResponsiveProfile.SHORT ? 0.95f : 0.78f;
            RectTransform sheet = EpochUiFactory.Panel(
                "World Bottom Sheet", _overlayLayer, EpochUiTokens.SurfaceRaised,
                Vector2.zero, new Vector2(1f, sheetTop));
            RectTransform handle = EpochUiFactory.Panel(
                "Sheet Handle", sheet, EpochUiTokens.TextMuted,
                new Vector2(0.42f, 0.955f), new Vector2(0.58f, 0.97f));
            handle.GetComponent<Image>().raycastTarget = false;
            string sheetTitle = state.IsWorldComplete ? "WORLD COMPLETE" : "WORLD PROGRESSION";
            EpochUiFactory.Text(
                "Sheet Title", sheetTitle,
                sheet, _font, EpochUiTokens.TextHeading, EpochUiTokens.Text,
                TextAnchor.MiddleLeft, FontStyle.Bold, new Vector2(0.05f, 0.82f), new Vector2(0.72f, 0.95f));
            EpochUiFactory.Button(
                "Dismiss Sheet", "DONE", sheet, _font, EpochButtonStyle.QUIET,
                DismissOverlay, new Vector2(0.74f, 0.83f), new Vector2(0.95f, 0.94f));
            _ = EpochUiFactory.ScrollArea(
                "World List Scroll", sheet, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.80f), out RectTransform content);
            content.sizeDelta = new Vector2(0f, 360f);
            for (int i = 0; i < state.Capitals.Count; i++)
            {
                CapitalWorldPresentation capital = state.Capitals[i];
                float yMax = 1f - i * 0.30f;
                float yMin = yMax - 0.25f;
                string status = !capital.IsUnlocked
                    ? "LOCKED"
                    : capital.IsCompleted
                        ? "COMPLETE"
                        : capital.IsCurrent
                            ? "CURRENT"
                            : "IN PROGRESS";
                string id = capital.CapitalId;
                bool unlocked = capital.IsUnlocked;
                EpochUiFactory.Button(
                    "World Capital " + (i + 1),
                    "CAPITAL " + (i + 1) + "     " + status + "     " + capital.CompletedUpgradeCount + "/25",
                    content, _font, EpochButtonStyle.SECONDARY,
                    () => SelectWorldCapital(id), new Vector2(0f, yMin), new Vector2(1f, yMax),
                    unlocked, capital.IsCurrent);
            }
        }

        public void SelectWorldCapital(string capitalId)
        {
            _game.SelectCapital(capitalId);
            CloseOverlay();
            ShowCapital();
        }

        public void OpenWorldForEditor() => OpenWorld();

        public void ShowBattlePlaceholder()
        {
            Destination = EpochShellDestination.BATTLE;
            EnterBattleLayoutMode();
            CloseOverlay();
            EpochUiFactory.Clear(_destinationLayer);
            EpochUiFactory.Clear(_stickyLayer);
            EpochUiFactory.Text(
                "Battle Host Title", "BATTLE HOST",
                _destinationLayer, _font, EpochUiTokens.TextTitle, EpochUiTokens.Text,
                TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.76f));
            EpochUiFactory.Text(
                "Battle Host Note",
                "Deterministic 3 Ã— 5 battle mounts on this shell destination.\nSafe-area + SHORT/REFERENCE/TALL reflow.\n\nGROWTH  Â·  INSIGHT\nNO PERSISTENT GOLD CHROME\nNO STICKY PRIMARY MID-BATTLE",
                _destinationLayer, _font, EpochUiTokens.TextBody, EpochUiTokens.TextMuted,
                TextAnchor.MiddleCenter, FontStyle.Normal, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.60f));
        }

        public void ShowResultPreview()
        {
            Destination = EpochShellDestination.RESULT;
            _previewResult = true;
            _resultNotice = string.Empty;
            RenderResult();
        }

        public void ShowResultRewards()
        {
            if (_game.ResultRewards is null)
            {
                throw new InvalidOperationException("Result & Rewards is available only after battle completion.");
            }

            Destination = EpochShellDestination.RESULT;
            _previewResult = false;
            RenderResult();
        }

        public void DismissOverlay()
        {
            if (!IsModalOpen && !IsSheetOpen && !IsCapitalCompletionOpen)
            {
                return;
            }

            bool wasCapitalCompletion = IsCapitalCompletionOpen;
            CloseOverlay();
            if (wasCapitalCompletion)
            {
                ShowCapital();
            }
        }

        private void BuildShell()
        {
            GameObject canvasObject = new GameObject(
                "EPOCH UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(EpochUiTokens.ReferenceWidth, EpochUiTokens.ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvas = canvasObject.GetComponent<RectTransform>();
            _ = EpochUiFactory.Panel("Full Bleed Background", canvas, EpochUiTokens.Background, Vector2.zero, Vector2.one);
            RectTransform safe = EpochUiFactory.Panel("Safe Area", canvas, new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.one);
            _safeArea = safe.gameObject.AddComponent<EpochSafeAreaRoot>();
            _destinationLayer = EpochUiFactory.Panel("Destination Layer", safe, new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.one);
            _stickyLayer = EpochUiFactory.Panel("Sticky Action Layer", safe, EpochUiTokens.Surface, Vector2.zero, Vector2.one);
            _overlayLayer = EpochUiFactory.Panel("Overlay Layer", safe, new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.one);
            _overlayLayer.gameObject.SetActive(false);
        }

        private void StartBattle()
        {
            SeedCode seed = new SeedCode(SeedCodec.Encode(20260908UL));
            _ = _game.StartNewBattle(seed);
            MountHostedBattle();
        }

        private void MountHostedBattle()
        {
            _resultNotice = string.Empty;
            CurrentResultPresentation = null;
            IsReplayHosted = false;
            PrepareShellBattleHost("EPOCH Hosted Battle");
            EpochMatchController controller = _matchHost!.AddComponent<EpochMatchController>();
            controller.BindShellHost(_matchHost.GetComponent<RectTransform>(), ResponsiveProfile);
            controller.InitializeHosted(_game, OnHostedBattleComplete);
            _matchHost.SetActive(true);
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                controller.InitializeForEditorSmoke();
            }
#endif
        }

        private void OnHostedBattleComplete()
        {
            DestroyMatchHost();
            _resultNotice = string.Empty;
            ShowResultRewards();
        }

        private void RenderResult()
        {
            ExitBattleLayoutMode();
            CloseOverlay();
            EpochUiFactory.Clear(_destinationLayer);
            EpochUiFactory.Clear(_stickyLayer);
            ResultRewardsPresentation? result = _previewResult ? null : _game.ResultRewards;
            CurrentResultPresentation = result;
            string outcome = result is null
                ? "RESULT & REWARDS"
                : result.RewardStatus == BattleRewardStatus.PRACTICE_NO_GOLD
                    ? "PRACTICE Â· " + result.Outcome
                    : result.Outcome.ToString();
            string score = result is null ? "SCORE  â€”  â€”" : result.PlayerScore + "  â€”  " + result.SnapshotScore;
            string reward = result is null
                ? "WIREFRAME PREVIEW Â· NO REWARD APPLIED"
                : result.RewardStatus == BattleRewardStatus.PRACTICE_NO_GOLD
                    ? "PRACTICE â€” NO GOLD\nGOLD BALANCE  " + result.GoldBalance
                    : result.RewardStatus == BattleRewardStatus.ALREADY_CREDITED
                        ? "ALREADY CREDITED Â· NO NEW GOLD\nORIGINAL REWARD  +" + result.RewardGold +
                          " Â· GOLD BALANCE  " + result.GoldBalance
                        : "+" + result.GoldCreditedNow + " GOLD Â· CREDITED AUTOMATICALLY\nGOLD BALANCE  " +
                          result.GoldBalance;
            string seed = result is null ? "SEED  â€”" : "SEED  " + result.Seed;

            // RR-SHELL-01 chrome (locked). Piece art soft-falls back to wireframe text when missing.
            RectTransform shellArt = EpochUiFactory.Panel(
                "Result Shell Art", _destinationLayer, new Color(0f, 0f, 0f, 0f),
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f));
            EpochArtCatalog.ApplySprite(shellArt.GetComponent<Image>(), EpochArtCatalog.ResultShell);

            Sprite? outcomeArt = result is null ? null : EpochArtCatalog.ResultOutcome(result.Outcome);
            if (outcomeArt is not null)
            {
                EpochUiFactory.SpriteImage(
                    "Result Outcome Art", _destinationLayer, outcomeArt,
                    new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.90f));
            }

            if (EpochArtCatalog.ResultScore is not null)
            {
                EpochUiFactory.SpriteImage(
                    "Result Score Art", _destinationLayer, EpochArtCatalog.ResultScore,
                    new Vector2(0.10f, 0.62f), new Vector2(0.90f, 0.73f));
            }

            Sprite? rewardArt = result is null ? null : EpochArtCatalog.ResultRewardStatus(result.RewardStatus);
            if (rewardArt is not null)
            {
                EpochUiFactory.SpriteImage(
                    "Result Reward Art", _destinationLayer, rewardArt,
                    new Vector2(0.10f, 0.48f), new Vector2(0.90f, 0.61f));
            }

            if (EpochArtCatalog.ResultSeedRow is not null)
            {
                EpochUiFactory.SpriteImage(
                    "Result Seed Row Art", _destinationLayer, EpochArtCatalog.ResultSeedRow,
                    new Vector2(0.10f, 0.42f), new Vector2(0.90f, 0.48f));
            }

            EpochUiFactory.Text(
                "Outcome", outcome,
                _destinationLayer, _font, EpochUiTokens.TextTitle, EpochUiTokens.Text,
                TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.88f));
            EpochUiFactory.Text(
                "Score", score,
                _destinationLayer, _font, EpochUiTokens.TextHeading, EpochUiTokens.Text,
                TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(0.08f, 0.61f), new Vector2(0.92f, 0.71f));
            EpochUiFactory.Text(
                "Reward", reward + "\n" + seed,
                _destinationLayer, _font, EpochUiTokens.TextBody, EpochUiTokens.Accent,
                TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.60f));

            if (!string.IsNullOrEmpty(_resultNotice))
            {
                EpochUiFactory.Text(
                    "Result Notice", _resultNotice,
                    _destinationLayer, _font, EpochUiTokens.TextSmall, EpochUiTokens.Player,
                    TextAnchor.MiddleCenter, FontStyle.Bold,
                    new Vector2(0.08f, 0.37f), new Vector2(0.92f, 0.42f));
            }

            float buttonTop = ResponsiveProfile == EpochResponsiveProfile.SHORT ? 0.36f : 0.35f;
            Button replay = EpochUiFactory.Button(
                "Replay", "REPLAY · READ ONLY", _destinationLayer, _font, EpochButtonStyle.SECONDARY,
                BeginReadOnlyReplay, new Vector2(0.06f, buttonTop - 0.09f), new Vector2(0.47f, buttonTop),
                result?.CanReplay == true);
            SoftDressResultButton(replay, EpochArtCatalog.ResultReplay);

            Button copySeed = EpochUiFactory.Button(
                "Copy Seed", "COPY SEED", _destinationLayer, _font, EpochButtonStyle.SECONDARY,
                CopyResultSeed, new Vector2(0.53f, buttonTop - 0.09f), new Vector2(0.94f, buttonTop),
                result?.CanCopySeed == true);
            SoftDressResultButton(copySeed, EpochArtCatalog.ResultCopySeed);

            Button practiceRestart = EpochUiFactory.Button(
                "Practice Restart", "RESTART SAME SEED · PRACTICE / NO GOLD",
                _destinationLayer, _font, EpochButtonStyle.QUIET,
                RestartSameSeedPractice, new Vector2(0.06f, buttonTop - 0.21f), new Vector2(0.94f, buttonTop - 0.12f),
                result?.CanRestartSameSeedPractice == true);
            SoftDressResultButton(practiceRestart, EpochArtCatalog.ResultPracticeRestart);

            StickyPrimary(
                "CONTINUE TO CAPITAL",
                result is null ? ShowCapital : ContinueToCapital,
                result is null || result.CanContinueToCapital);
            SoftDressResultButton(
                _stickyLayer.Find("Primary Sticky Action")?.GetComponent<Button>(),
                EpochArtCatalog.ResultContinue);
        }

        private static void SoftDressResultButton(Button? button, Sprite? sprite)
        {
            if (button is null || sprite is null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (!EpochArtCatalog.TryApplySprite(image, sprite, sliced: true))
            {
                return;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            button.colors = colors;
        }

        public void ContinueToCapital()
        {
            _ = _game.ContinueToCapital();
            CurrentResultPresentation = null;
            _resultNotice = string.Empty;
            ShowCapital();
        }

        public void CopyResultSeed()
        {
            ResultRewardsPresentation result = _game.ResultRewards ??
                throw new InvalidOperationException("Copy Seed is available only from Result & Rewards.");
            if (!result.CanCopySeed)
            {
                return;
            }

            GUIUtility.systemCopyBuffer = result.Seed;
            LastCopiedSeed = result.Seed;
            _resultNotice = "SEED COPIED";
            RenderResult();
        }

        public void BeginReadOnlyReplay()
        {
            ResultRewardsPresentation result = _game.ResultRewards ??
                throw new InvalidOperationException("Replay is available only from Result & Rewards.");
            if (!result.CanReplay)
            {
                return;
            }

            IReadOnlyList<Epoch.Application.Runs.PresentationTurn> frames = _game.ReplayTurns();
            Epoch.Application.Runs.PlayableMatchSession session = _game.Battle ??
                throw new InvalidOperationException("The completed battle is unavailable for replay.");
            IsReplayHosted = true;
            PrepareShellBattleHost("EPOCH Read-Only Replay");
            EpochMatchController controller = _matchHost!.AddComponent<EpochMatchController>();
            controller.BindShellHost(_matchHost.GetComponent<RectTransform>(), ResponsiveProfile);
            controller.InitializeHostedReplay(session, frames, OnHostedReplayComplete);
            _matchHost.SetActive(true);
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                controller.InitializeForEditorSmoke();
            }
#endif
        }

        public void RestartSameSeedPractice()
        {
            ResultRewardsPresentation result = _game.ResultRewards ??
                throw new InvalidOperationException("Practice restart is available only from Result & Rewards.");
            if (!result.CanRestartSameSeedPractice)
            {
                return;
            }

            _ = _game.RestartSameSeedPractice();
            MountHostedBattle();
        }

        private void OnHostedReplayComplete()
        {
            DestroyMatchHost();
            IsReplayHosted = false;
            _resultNotice = "READ-ONLY REPLAY COMPLETE";
            ShowResultRewards();
        }

        private void PrepareShellBattleHost(string hostName)
        {
            DestroyMatchHost();
            Destination = EpochShellDestination.BATTLE;
            EnterBattleLayoutMode();
            CloseOverlay();
            EpochUiFactory.Clear(_destinationLayer);
            EpochUiFactory.Clear(_stickyLayer);
            _canvas.enabled = true;
            _matchHost = new GameObject(hostName);
            _matchHost.transform.SetParent(_destinationLayer, false);
            RectTransform hostRect = _matchHost.AddComponent<RectTransform>();
            hostRect.anchorMin = Vector2.zero;
            hostRect.anchorMax = Vector2.one;
            hostRect.offsetMin = Vector2.zero;
            hostRect.offsetMax = Vector2.zero;
            _matchHost.SetActive(false);
        }

        private void EnterBattleLayoutMode()
        {
            _battleLayoutActive = true;
            _stickyLayer.gameObject.SetActive(false);
            ApplyDestinationOffsets();
        }

        private void ExitBattleLayoutMode()
        {
            _battleLayoutActive = false;
            _stickyLayer.gameObject.SetActive(true);
            ApplyDestinationOffsets();
        }

        private void ApplyDestinationOffsets()
        {
            _destinationLayer.offsetMin = _battleLayoutActive
                ? Vector2.zero
                : new Vector2(0f, EpochUiTokens.StickyRegionHeight);
            _destinationLayer.offsetMax = Vector2.zero;
        }

        private void DestroyMatchHost()
        {
            if (_matchHost is null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(_matchHost);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(_matchHost);
            }

            _matchHost = null;
        }

        private void ShowCapitalCompletion(LandmarkUpgradeReceipt receipt)
        {
            CloseOverlay();
            IsCapitalCompletionOpen = true;
            LastUpgradeReceipt = receipt;
            _overlayLayer.gameObject.SetActive(true);
            _ = EpochUiFactory.Panel(
                "Overlay Dimmer", _overlayLayer, EpochUiTokens.Dimmer, Vector2.zero, Vector2.one);
            bool worldComplete = receipt.UnlockedCapitalId is null;
            RectTransform modal = EpochUiFactory.Panel(
                "Capital Completion", _overlayLayer, Color.white,
                new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.75f));
            EpochArtCatalog.ApplySprite(
                modal.GetComponent<Image>(),
                worldComplete ? EpochArtCatalog.WorldCompleteChrome : EpochArtCatalog.CapitalCompleteChrome,
                sliced: true);
            string next = worldComplete
                ? "WORLD COMPLETE\nALL CAPITALS REVISITABLE VIA WORLD"
                : "UNLOCKED + AUTO-FOCUSED\n" + receipt.UnlockedCapitalId;
            string headline = worldComplete ? "WORLD COMPLETE" : "CAPITAL COMPLETE";
            EpochUiFactory.Text(
                "Completion Summary",
                headline + "\n\n+" + receipt.CompletionGoldAwarded + " GOLD\n\n" + next +
                "\n\nBALANCE  " + receipt.GoldBalance + " GOLD",
                modal, _font, EpochUiTokens.TextHeading, EpochUiTokens.Text,
                TextAnchor.MiddleCenter, FontStyle.Bold,
                new Vector2(0.07f, 0.24f), new Vector2(0.93f, 0.94f));
            EpochUiFactory.Button(
                "Dismiss Completion", "CONTINUE", modal, _font, EpochButtonStyle.PRIMARY,
                DismissOverlay, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.22f));
        }

        private void StickyPrimary(
            string label,
            UnityEngine.Events.UnityAction action,
            bool interactable = true)
        {
            EpochUiFactory.Button(
                "Primary Sticky Action", label,
                _stickyLayer, _font, EpochButtonStyle.PRIMARY, action,
                Vector2.zero, Vector2.one, interactable);
        }

        private void CloseOverlay()
        {
            IsModalOpen = false;
            IsSheetOpen = false;
            IsCapitalCompletionOpen = false;
            _selectedLandmarkId = null;
            CurrentUpgradePresentation = null;
            EpochUiFactory.Clear(_overlayLayer);
            _overlayLayer.gameObject.SetActive(false);
        }

        private void ShowFatal(string message)
        {
            EpochUiFactory.Clear(_destinationLayer);
            EpochUiFactory.Clear(_stickyLayer);
            EpochUiFactory.Text(
                "Fatal", "EPOCH COULD NOT START\n\n" + message,
                _destinationLayer, _font, EpochUiTokens.TextBody, EpochUiTokens.Snapshot,
                TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.8f));
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() is not null)
            {
                return;
            }

            GameObject system = new GameObject("EventSystem");
            system.AddComponent<EventSystem>();
            system.AddComponent<StandaloneInputModule>();
        }

        private void EnsureCamera()
        {
            Camera? camera = FindAnyObjectByType<Camera>();
            if (camera is null)
            {
                GameObject cameraObject = new GameObject("EPOCH Camera", typeof(Camera));
                cameraObject.transform.SetParent(transform, false);
                camera = cameraObject.GetComponent<Camera>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = EpochUiTokens.Background;
            camera.cullingMask = 0;
            camera.depth = -100;
        }

#if UNITY_EDITOR
        public void InitializeForEditorSmoke() => Awake();

        public void SmokeHostedBattleStart()
        {
            EpochMatchController controller = SmokeHostedBattleMount();
            if (!controller.IsReady || !controller.IsHostedInShell || !_canvas.enabled)
            {
                throw new InvalidOperationException(
                    "The existing battle presentation did not mount through EpochGameSession. " +
                    controller.StartupError);
            }

            DestroyMatchHost();
            ExitBattleLayoutMode();
            ShowCapital();
        }

        public EpochMatchController SmokeHostedBattleMount()
        {
            StartBattle();
            if (_game.Battle is null || _matchHost is null)
            {
                throw new InvalidOperationException("The hosted battle boundary did not initialize.");
            }

            if (!_canvas.enabled)
            {
                throw new InvalidOperationException("Group 5 requires the shared shell canvas to remain enabled.");
            }

            if (_matchHost.transform.parent != _destinationLayer)
            {
                throw new InvalidOperationException("Hosted battle must mount under the shell destination layer.");
            }

            if (_stickyLayer.gameObject.activeSelf || _battleLayoutActive == false)
            {
                throw new InvalidOperationException("Mid-battle sticky primary must stay disabled.");
            }

            EpochMatchController? controller = _matchHost.GetComponent<EpochMatchController>();
#if UNITY_EDITOR
            controller?.InitializeForEditorSmoke();
#endif
            if (controller is null || !controller.IsReady || !controller.IsHostedInShell)
            {
                throw new InvalidOperationException(
                    "The existing battle presentation did not mount through EpochGameSession. " +
                    (controller?.StartupError ?? string.Empty));
            }

            return controller;
        }

        public void StopHostedPresentationForEditorSmoke()
        {
            DestroyMatchHost();
            IsReplayHosted = false;
            ExitBattleLayoutMode();
            _canvas.enabled = true;
        }

        public void CaptureHostedBattleForEditorSmoke(
            string path,
            int width,
            int height,
            Rect safeArea)
        {
            if (_matchHost is null)
            {
                throw new InvalidOperationException("Hosted battle capture requires an active shell-hosted battle.");
            }

            ApplyViewport(safeArea, width, height);
            EpochMatchController controller = _matchHost.GetComponent<EpochMatchController>() ??
                throw new InvalidOperationException("Hosted battle controller is missing.");
            controller.ApplyResponsiveProfile(ResponsiveProfile);
            controller.CloseHelpForEditorCapture();
            CaptureCurrentCanvas(path, width, height);
        }

        public bool ShellCanvasEnabled => _canvas.enabled;

        public bool BattleUsesShellDestination =>
            _matchHost is not null && _matchHost.transform.parent == _destinationLayer;

        public bool StickyVisible => _stickyLayer.gameObject.activeSelf;

        public void Preview(EpochShellPreviewSurface surface)
        {
            switch (surface)
            {
                case EpochShellPreviewSurface.CAPITAL:
                    ShowCapital();
                    break;
                case EpochShellPreviewSurface.BATTLE:
                    ShowBattlePlaceholder();
                    break;
                case EpochShellPreviewSurface.RESULT:
                    ShowResultPreview();
                    break;
                case EpochShellPreviewSurface.UPGRADE:
                    ShowCapital();
                    OpenUpgrade(ProgressionCatalog.LandmarkIds[0]);
                    break;
                case EpochShellPreviewSurface.WORLD:
                    ShowCapital();
                    OpenWorld();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(surface));
            }
        }

        public void CaptureEditorPreview(
            string path,
            int width,
            int height,
            Rect safeArea,
            EpochShellPreviewSurface surface)
        {
            Preview(surface);
            ApplyViewport(safeArea, width, height);
            Preview(surface);
            CaptureCurrentCanvas(path, width, height);
        }

        public void CaptureCurrentResultForEditorSmoke(
            string path,
            int width,
            int height,
            Rect safeArea)
        {
            ApplyViewport(safeArea, width, height);
            ShowResultRewards();
            CaptureCurrentCanvas(path, width, height);
        }

        public void CaptureWorldForEditorSmoke(
            string path,
            int width,
            int height,
            Rect safeArea)
        {
            ApplyViewport(safeArea, width, height);
            OpenWorld();
            CaptureCurrentCanvas(path, width, height);
        }

        public void CaptureCapitalCompletionForEditorSmoke(
            string path,
            int width,
            int height,
            Rect safeArea)
        {
            if (LastUpgradeReceipt is null || !LastUpgradeReceipt.CapitalCompleted)
            {
                throw new InvalidOperationException("Capital completion capture requires a completion receipt.");
            }

            ApplyViewport(safeArea, width, height);
            ShowCapital();
            ShowCapitalCompletion(LastUpgradeReceipt);
            CaptureCurrentCanvas(path, width, height);
        }

        private void CaptureCurrentCanvas(string path, int width, int height)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            Camera? camera = FindAnyObjectByType<Camera>();
            if (camera is null)
            {
                throw new InvalidOperationException("Preview camera is missing.");
            }

            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            camera.cullingMask = ~0;
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = camera;
            _canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image);
            camera.targetTexture = null;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.worldCamera = null;
            camera.cullingMask = 0;
        }
#endif

        private sealed class ShellMemoryProgressionStore : ProgressionStore
        {
            private string? _state;

            public string? Load() => _state;

            public void Save(string serializedState) => _state = serializedState;
        }
    }
}
