#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Epoch.Application.Progression;
using Epoch.Application.Runs;
using Epoch.Content.Loading;
using Epoch.Core.Content;
using Epoch.Core.Domain;
using Epoch.Core.Events;
using Epoch.Core.Rng;
using Epoch.Core.StateMachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Epoch.Presentation
{
    /// <summary>
    /// M4's single portrait match view. It builds a compact uGUI scene at runtime so the
    /// prototype has no fragile prefab state. Every gameplay value is projected from
    /// PlayableMatchSession; this component owns only selection and animation state.
    /// </summary>
    public sealed class EpochMatchController : MonoBehaviour
    {
        private static readonly Color Navy = Hex("101722");
        private static readonly Color Charcoal = Hex("18222E");
        private static readonly Color Parchment = Hex("E8D9B6");
        private static readonly Color Ink = Hex("20242B");
        private static readonly Color Gold = Hex("D6AE58");
        private static readonly Color Player = Hex("45B8A6");
        private static readonly Color Snapshot = Hex("D86B62");
        private Font _font = null!;
        private ValidatedContentSet _content = null!;
        private PlayableMatchSession _session = null!;
        private Canvas _canvas = null!;
        private RectTransform _header = null!;
        private RectTransform _board = null!;
        private RectTransform _hand = null!;
        private RectTransform _footer = null!;
        private RectTransform _overlay = null!;
        private Text _turnText = null!;
        private Text _playerText = null!;
        private Text _snapshotText = null!;
        private Text _statusText = null!;
        private Text _speedText = null!;
        private readonly RectTransform[,] _tiles = new RectTransform[3, 5];
        private readonly Button[] _laneTargets = new Button[3];
        private readonly Outline[] _laneOutlines = new Outline[3];
        private readonly Dictionary<string, RectTransform> _unitViews = new Dictionary<string, RectTransform>();
        private int? _selectedOffer;
        private bool _animating;
        private bool _skipRequested;
        private float _speed = 1f;
        private Coroutine? _forcedPassRoutine;
        private Sprite? _circleSprite;
        private EpochGameSession? _hostedGame;
        private Action? _hostedCompletion;
        private PlayableMatchSession? _hostedReplaySession;
        private IReadOnlyList<PresentationTurn>? _hostedReplayFrames;
        private Action? _hostedReplayCompletion;
        private RectTransform? _shellHost;
        private RectTransform _root = null!;
        private Text _seedText = null!;
        private EpochResponsiveProfile _responsiveProfile = EpochResponsiveProfile.REFERENCE;
        private bool _hostedInShell;

        public bool IsReady { get; private set; }
        public bool IsHostedInShell => _hostedInShell;
        public EpochResponsiveProfile ResponsiveProfile => _responsiveProfile;
        public string HeaderTurnText => _turnText != null ? _turnText.text : string.Empty;
        public string SeedChromeText => _seedText != null ? _seedText.text : string.Empty;
        public string PlayerChromeText => _playerText != null ? _playerText.text : string.Empty;
        public string SnapshotChromeText => _snapshotText != null ? _snapshotText.text : string.Empty;
        public bool HasStickyPrimaryAction => false;
        public bool HelpOverlayActive => _overlay != null && _overlay.gameObject.activeSelf;

        public string StartupError { get; private set; } = string.Empty;

        public void InitializeHosted(EpochGameSession game, Action completed)
        {
            if (IsReady)
            {
                throw new InvalidOperationException("The battle view is already initialized.");
            }

            _hostedGame = game ?? throw new ArgumentNullException(nameof(game));
            _hostedCompletion = completed ?? throw new ArgumentNullException(nameof(completed));
        }

        public void InitializeHostedReplay(
            PlayableMatchSession session,
            IReadOnlyList<PresentationTurn> frames,
            Action completed)
        {
            if (IsReady)
            {
                throw new InvalidOperationException("The battle view is already initialized.");
            }

            _hostedReplaySession = session ?? throw new ArgumentNullException(nameof(session));
            _hostedReplayFrames = frames ?? throw new ArgumentNullException(nameof(frames));
            _hostedReplayCompletion = completed ?? throw new ArgumentNullException(nameof(completed));
            if (frames.Count != RunState.TurnsPerRun || !session.IsComplete)
            {
                throw new InvalidOperationException("Only a complete recorded battle can be replayed.");
            }
        }

        public void BindShellHost(RectTransform hostRoot, EpochResponsiveProfile profile)
        {
            if (IsReady)
            {
                throw new InvalidOperationException("The battle view is already initialized.");
            }

            _shellHost = hostRoot ?? throw new ArgumentNullException(nameof(hostRoot));
            _responsiveProfile = profile;
            _hostedInShell = true;
        }

        public void ApplyResponsiveProfile(EpochResponsiveProfile profile)
        {
            _responsiveProfile = profile;
            if (_root is not null)
            {
                ApplyLayoutRegions();
            }
        }

        public void CloseHelpForEditorCapture()
        {
            if (_overlay is not null)
            {
                _overlay.gameObject.SetActive(false);
            }
        }

        private void Awake()
        {
            if (IsReady)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying && !_hostedInShell)
            {
                DontDestroyOnLoad(gameObject);
            }
            UnityEngine.Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (!_hostedInShell)
            {
                EnsureGameCamera();
                EnsureEventSystem();
            }
            BuildCanvas();

            try
            {
                string contentPath = Path.Combine(
                    UnityEngine.Application.dataPath, "..", "_aiinfodocs", "data", "epoch_v1_content.json");
                if (_hostedReplaySession is not null)
                {
                    _session = _hostedReplaySession;
                }
                else if (_hostedGame is not null)
                {
                    _session = _hostedGame.Battle ?? throw new InvalidOperationException("The hosted battle is missing.");
                }
                else
                {
                    _content = ContentLoader.Load(
                        File.ReadAllText(Path.GetFullPath(contentPath)), RunFactory.RulesVersion);
                    SeedCode seed = new SeedCode(SeedCodec.Encode(20260908UL));
                    _session = new PlayableMatchSession(seed, _content);
                }
                IsReady = true;
                if (_hostedReplayFrames is not null)
                {
                    _overlay.gameObject.SetActive(false);
                    RenderState(_hostedReplayFrames[0].PreState);
                    if (UnityEngine.Application.isPlaying)
                    {
                        StartCoroutine(Replay(_hostedReplayFrames));
                    }
                }
                else
                {
                    RenderChoice();
                    ShowHelp();
                }
            }
            catch (Exception exception)
            {
                StartupError = exception.ToString();
                Debug.LogException(exception);
                ShowFatal(exception.Message);
            }
        }

#if UNITY_EDITOR
        public void InitializeForEditorSmoke() => Awake();

        public void CaptureEditorPreview(string path)
        {
            Camera? camera = FindAnyObjectByType<Camera>();
            bool createdCamera = camera is null;
            GameObject? cameraObject = null;
            if (camera is null)
            {
                cameraObject = new GameObject("EPOCH Preview Camera");
                camera = cameraObject.AddComponent<Camera>();
            }

            CameraClearFlags previousClearFlags = camera.clearFlags;
            Color previousBackground = camera.backgroundColor;
            int previousCullingMask = camera.cullingMask;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Navy;
            camera.cullingMask = ~0;
            camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -10);

            RenderTexture target = new RenderTexture(390, 844, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = camera;
            _canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D image = new Texture2D(390, 844, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 390, 844), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = previous;

            DestroyImmediate(image);
            camera.targetTexture = null;
            target.Release();
            DestroyImmediate(target);
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.worldCamera = null;
            camera.clearFlags = previousClearFlags;
            camera.backgroundColor = previousBackground;
            camera.cullingMask = previousCullingMask;
            if (createdCamera && cameraObject is not null)
            {
                DestroyImmediate(cameraObject);
            }
        }
#endif

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

        private void EnsureGameCamera()
        {
            Camera? camera = FindAnyObjectByType<Camera>();
            if (camera is not null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Navy;
                return;
            }

            GameObject cameraObject = new GameObject("EPOCH Camera", typeof(Camera));
            cameraObject.transform.SetParent(transform, false);
            camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Navy;
            camera.cullingMask = 0;
            camera.depth = -100;
        }

        private void BuildCanvas()
        {
            RectTransform root;
            if (_shellHost is not null)
            {
                Canvas? shellCanvas = _shellHost.GetComponentInParent<Canvas>();
                if (shellCanvas is null)
                {
                    throw new InvalidOperationException("Shell-hosted battle requires a parent Canvas.");
                }

                _canvas = shellCanvas;
                root = MakePanel("Battle Root", _shellHost, Vector2.zero, Vector2.one, Navy);
            }
            else
            {
                GameObject canvasObject = new GameObject("Portrait Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(transform, false);
                _canvas = canvasObject.GetComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(390, 844);
                scaler.matchWidthOrHeight = 0.5f;
                root = MakePanel("Background", canvasObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Navy);
            }

            _root = root;
            _header = MakePanel("Header", root, Vector2.zero, Vector2.one, Charcoal);
            _board = MakePanel("Board", root, Vector2.zero, Vector2.one, Charcoal);
            _hand = MakePanel("Shared offers", root, Vector2.zero, Vector2.one, Navy);
            _footer = MakePanel("Footer", root, Vector2.zero, Vector2.one, Charcoal);
            ApplyLayoutRegions();

            _playerText = MakeText("PLAYER", _header, new Vector2(0.015f, 0.05f), new Vector2(0.34f, 0.95f), 15, Player, TextAnchor.MiddleLeft, FontStyle.Bold);
            _turnText = MakeText("TURN", _header, new Vector2(0.34f, 0.05f), new Vector2(0.66f, 0.95f), 16, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _snapshotText = MakeText("SNAPSHOT", _header, new Vector2(0.66f, 0.05f), new Vector2(0.985f, 0.95f), 15, Snapshot, TextAnchor.MiddleRight, FontStyle.Bold);
            _seedText = MakeText("SEED", _footer, new Vector2(0.58f, 0.55f), new Vector2(0.99f, 0.95f), 9, new Color(Parchment.r, Parchment.g, Parchment.b, 0.55f), TextAnchor.MiddleRight, FontStyle.Normal);

            Button speed = MakeButton("Speed", _footer, new Vector2(0.01f, 0.08f), new Vector2(0.20f, 0.92f), Charcoal, ToggleSpeed);
            _speedText = speed.GetComponentInChildren<Text>();
            _speedText.text = "FAST ×1";
            MakeButton("Skip", _footer, new Vector2(0.21f, 0.08f), new Vector2(0.40f, 0.92f), Charcoal, SkipAnimation)
                .GetComponentInChildren<Text>().text = "SKIP";
            MakeButton("Help", _footer, new Vector2(0.41f, 0.08f), new Vector2(0.57f, 0.92f), Charcoal, ShowHelp)
                .GetComponentInChildren<Text>().text = "HELP";
            _statusText = MakeText("Pick 1 card", _footer, new Vector2(0.58f, 0.05f), new Vector2(0.99f, 0.52f), 11, Parchment, TextAnchor.MiddleRight, FontStyle.Normal);

            _overlay = MakePanel("Overlay", root, Vector2.zero, Vector2.one, new Color(0.02f, 0.035f, 0.055f, 0.95f));
            _overlay.GetComponent<Image>().raycastTarget = true;
            _overlay.gameObject.SetActive(false);
        }

        private void ApplyLayoutRegions()
        {
            EpochBattleBoardPresentation.ApplyRegions(_header, _board, _hand, _footer, _responsiveProfile);
        }

        private void RenderChoice()
        {
            _selectedOffer = null;
            RenderState(_session.State);
            RenderCards();
            SetLaneTargeting(null);
            _statusText.text = "PICK 1 CARD BELOW";

            if (_session.IsForcedPass && !_session.IsComplete)
            {
                _statusText.text = "NO AFFORDABLE MOVE · TURN SKIPPED";
                if (_forcedPassRoutine is not null)
                {
                    StopCoroutine(_forcedPassRoutine);
                }

                _forcedPassRoutine = StartCoroutine(ForcedPass());
            }
        }

        private void RenderState(RunState state)
        {
            _turnText.text = "TURN " + state.Turn + "/24\n< " + AgeName(state.Age) + " >";
            _playerText.text = "PLAYER\nSCORE  " + state.Player.Score + "\nGrowth " + state.Player.Growth + "  ·  Insight " + state.Player.Insight;
            _snapshotText.text = "SNAPSHOT\nSCORE  " + state.Snapshot.Score + "\nGrowth " + state.Snapshot.Growth + "  ·  Insight " + state.Snapshot.Insight;
            _seedText.text = "SEED  " + state.Seed.MasterSeed.Text;
            RenderBoard(state);
        }

        private void RenderBoard(RunState state)
        {
            ClearChildren(_board);
            _unitViews.Clear();

            for (int laneIndex = 0; laneIndex < 3; laneIndex++)
            {
                LaneState lane = state.Lanes[laneIndex];
                float left = 0.012f + (laneIndex * 0.33f);
                float right = left + 0.316f;
                Color tint = EpochBattleBoardPresentation.LaneSurface(lane.Modifier);
                RectTransform lanePanel = MakePanel("Lane " + lane.Id, _board,
                    new Vector2(left, 0.02f), new Vector2(right, 0.98f), tint);
                Outline outline = lanePanel.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0, 0, 0, 0);
                outline.effectDistance = new Vector2(3, 3);
                _laneOutlines[laneIndex] = outline;

                int playerStructures = CountStructures(state.Player, lane.Id);
                int snapshotStructures = CountStructures(state.Snapshot, lane.Id);
                MakeText(lane.Modifier + "\n" + EpochBattleBoardPresentation.LaneRule(lane.Modifier), lanePanel, new Vector2(0, 0.90f), new Vector2(1, 1), 9, Parchment, TextAnchor.MiddleCenter, FontStyle.Bold);
                MakeText("ENEMY BUILDS " + snapshotStructures, lanePanel, new Vector2(0.03f, 0.83f), new Vector2(0.97f, 0.90f), 8, Snapshot, TextAnchor.MiddleCenter, FontStyle.Normal);
                MakeText("YOUR BUILDS " + playerStructures, lanePanel, new Vector2(0.03f, 0.01f), new Vector2(0.97f, 0.08f), 8, Player, TextAnchor.MiddleCenter, FontStyle.Normal);

                for (int tileIndex = 5; tileIndex >= 1; tileIndex--)
                {
                    int visual = 5 - tileIndex;
                    float top = 0.835f - (visual * 0.15f);
                    float bottom = top - 0.135f;
                    Color tileColor = tileIndex == 3
                        ? new Color(Gold.r, Gold.g, Gold.b, 0.28f)
                        : new Color(Parchment.r, Parchment.g, Parchment.b, 0.11f);
                    RectTransform tile = MakePanel("Tile " + tileIndex, lanePanel,
                        new Vector2(0.055f, bottom), new Vector2(0.945f, top), tileColor);
                    _tiles[laneIndex, tileIndex - 1] = tile;
                    MakeText(tileIndex == 3 ? "CONTESTED" : tileIndex.ToString(), tile,
                        new Vector2(0.02f, 0.68f), new Vector2(0.98f, 0.98f), 8,
                        tileIndex == 3 ? Gold : new Color(1, 1, 1, 0.36f), TextAnchor.UpperCenter, FontStyle.Bold);

                    if (tileIndex == 3 && lane.OwnedBy is not null)
                    {
                        Outline held = tile.gameObject.AddComponent<Outline>();
                        held.effectColor = lane.OwnedBy == Side.PLAYER ? Player : Snapshot;
                        held.effectDistance = new Vector2(2, 2);
                    }
                }

                Button laneTarget = lanePanel.gameObject.AddComponent<Button>();
                lanePanel.GetComponent<Image>().raycastTarget = true;
                laneTarget.transition = Selectable.Transition.None;
                int captured = laneIndex;
                laneTarget.onClick.AddListener(() => OnLaneClicked((LaneId)captured));
                _laneTargets[laneIndex] = laneTarget;
            }

            AddUnits(state.Player.Units);
            AddUnits(state.Snapshot.Units);
        }

        private void AddUnits(IReadOnlyList<UnitInstance> units)
        {
            for (int i = 0; i < units.Count; i++)
            {
                CreateUnitView(units[i], units[i].TileIndex);
            }
        }

        private RectTransform CreateUnitView(UnitInstance unit, int tileIndex)
        {
            RectTransform tile = _tiles[(int)unit.LaneId, tileIndex - 1];
            GameObject token = new GameObject("UNIT_" + unit.InstanceId.Value, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = token.GetComponent<RectTransform>();
            rect.SetParent(tile, false);
            rect.anchorMin = rect.anchorMax = new Vector2(unit.Owner == Side.PLAYER ? 0.28f : 0.72f, 0.40f);
            rect.sizeDelta = new Vector2(40, 40);
            rect.anchoredPosition = StackOffset(unit, tileIndex);

            Image image = token.GetComponent<Image>();
            image.color = unit.Owner == Side.PLAYER ? Player : Snapshot;
            if (unit.UnitClass == UnitClass.SPEAR)
            {
                image.sprite = CircleSprite();
            }
            else if (unit.UnitClass == UnitClass.HORSE)
            {
                rect.localRotation = Quaternion.Euler(0, 0, 45);
            }

            Text label = MakeText(UnitMark(unit) + "\nHP " + unit.Hp, rect,
                Vector2.zero, Vector2.one, 7, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (unit.UnitClass == UnitClass.HORSE)
            {
                label.rectTransform.localRotation = Quaternion.Euler(0, 0, -45);
            }

            _unitViews[unit.InstanceId.Value] = rect;
            return rect;
        }

        private Vector2 StackOffset(UnitInstance unit, int tileIndex)
        {
            IReadOnlyList<UnitInstance> units = unit.Owner == Side.PLAYER ? _session.State.Player.Units : _session.State.Snapshot.Units;
            int same = 0;
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].LaneId == unit.LaneId && units[i].TileIndex == tileIndex &&
                    string.CompareOrdinal(units[i].InstanceId.Value, unit.InstanceId.Value) < 0)
                {
                    same++;
                }
            }

            return new Vector2((same - 1) * 12f, -2f);
        }

        private void RenderCards()
        {
            ClearChildren(_hand);
            IReadOnlyList<CardPresentation> cards = _session.Cards();
            for (int i = 0; i < cards.Count; i++)
            {
                CardPresentation card = cards[i];
                float left = 0.005f + (i * 0.333f);
                float right = left + 0.323f;
                Color color = CardColor(card);
                Button button = MakeButton("Offer " + i, _hand,
                    new Vector2(left, 0.04f), new Vector2(right, 0.96f), color, () => OnCardClicked(card.OfferIndex));
                button.interactable = card.IsLegal && !_animating;
                Outline outline = button.gameObject.AddComponent<Outline>();
                outline.effectColor = _selectedOffer == i ? Gold : new Color(0, 0, 0, 0.45f);
                outline.effectDistance = _selectedOffer == i ? new Vector2(4, 4) : new Vector2(1, 1);

                string type = card.Definition.IsKeystone ? "KEYSTONE" : card.Definition.CardType.ToString();
                string resource = card.Definition.CostResource == ResourceType.GROWTH ? "GROWTH" : "INSIGHT";
                string reason = card.IsLegal ? "PICK THIS" : IllegalLabel(card.IllegalReason);
                button.GetComponentInChildren<Text>().text =
                    type + " · COST " + card.Cost + " " + resource + "\n" +
                    card.Definition.DisplayName + "\n" + card.Definition.ShortEffectLine + "\n" + reason;
                button.GetComponentInChildren<Text>().fontSize = 9;
            }

            if (_selectedOffer is not null)
            {
                Button cancel = MakeButton("Cancel", _hand, new Vector2(0.36f, 0.01f), new Vector2(0.64f, 0.24f), Snapshot, CancelTargeting);
                cancel.GetComponentInChildren<Text>().text = "CANCEL";
            }
        }

        private void OnCardClicked(int offerIndex)
        {
            if (_animating || _session.IsComplete)
            {
                return;
            }

            CardPresentation card = _session.Cards()[offerIndex];
            if (!card.IsLegal)
            {
                return;
            }

            if (card.Definition.CardType == CardType.ADVANCE)
            {
                Resolve(new Selection(offerIndex, null));
                return;
            }

            _selectedOffer = offerIndex;
            SetLaneTargeting(card.LegalLanes);
            _statusText.text = "NOW PICK A GLOWING LANE";
            RenderCards();
        }

        private void OnLaneClicked(LaneId lane)
        {
            if (_animating || _selectedOffer is null)
            {
                return;
            }

            CardPresentation card = _session.Cards()[_selectedOffer.Value];
            for (int i = 0; i < card.LegalLanes.Count; i++)
            {
                if (card.LegalLanes[i] == lane)
                {
                    Resolve(new Selection(_selectedOffer.Value, lane));
                    return;
                }
            }
        }

        private void CancelTargeting()
        {
            _selectedOffer = null;
            SetLaneTargeting(null);
            RenderCards();
            _statusText.text = "PICK 1 CARD BELOW";
        }

        private void Resolve(Selection selection)
        {
            _selectedOffer = null;
            PresentationTurn turn = _hostedGame is null
                ? _session.Submit(selection)
                : selection.IsPass
                    ? _hostedGame.SubmitForcedPass()
                    : _hostedGame.SubmitBattleSelection(selection);
            StartCoroutine(PlayTurn(turn));
        }

        private IEnumerator ForcedPass()
        {
            yield return new WaitForSecondsRealtime(0.65f);
            if (!_animating && _session.IsForcedPass)
            {
                Resolve(Selection.Pass);
            }
        }

        private IEnumerator PlayTurn(PresentationTurn turn)
        {
            _animating = true;
            _skipRequested = false;
            RenderState(turn.PreState);
            ClearChildren(_hand);
            SetLaneTargeting(null);

            for (int i = 0; i < turn.Events.Count && !_skipRequested; i++)
            {
                if (turn.Events[i] is not RecordedEvent recorded)
                {
                    continue;
                }

                _statusText.text = EventLabel(recorded);
                if (recorded.EventType == SimulationEventType.UNIT_MOVED)
                {
                    yield return AnimateMove(recorded, turn.PostState);
                }
                else if (recorded.EventType == SimulationEventType.COMBAT_RESOLVED)
                {
                    yield return AnimateCombat(recorded);
                }
                else if (recorded.EventType == SimulationEventType.UNIT_DESTROYED)
                {
                    yield return AnimateDeath(recorded);
                }
                else if (recorded.EventType == SimulationEventType.AGE_ADVANCED)
                {
                    yield return AgeTransition(Value(recorded.Payload, "age"));
                }
                else
                {
                    yield return Delay(0.07f);
                }
            }

            RenderState(turn.PostState);
            _animating = false;
            _skipRequested = false;

            if (_session.IsComplete)
            {
                if (_hostedCompletion is not null)
                {
                    _hostedCompletion();
                    Destroy(gameObject);
                }
                else
                {
                    ShowResult();
                }
            }
            else
            {
                RenderChoice();
            }
        }

        private IEnumerator AnimateMove(RecordedEvent recorded, RunState postState)
        {
            string id = Value(recorded.Payload, "unit");
            LaneId lane = (LaneId)Enum.Parse(typeof(LaneId), Value(recorded.Payload, "lane"));
            int from = int.Parse(Value(recorded.Payload, "from"));
            int to = int.Parse(Value(recorded.Payload, "to"));
            if (!_unitViews.TryGetValue(id, out RectTransform rect))
            {
                UnitInstance? unit = FindUnit(postState, id);
                if (unit is null)
                {
                    yield break;
                }

                rect = CreateUnitView(unit, from);
            }

            RectTransform target = _tiles[(int)lane, to - 1];
            Vector3 start = rect.position;
            rect.SetParent(target, true);
            Vector3 end = target.TransformPoint(Vector3.zero);
            float elapsed = 0;
            float duration = 0.2f / _speed;
            while (elapsed < duration && !_skipRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                rect.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, elapsed / duration));
                yield return null;
            }

            rect.anchoredPosition = Vector2.zero;
        }

        private IEnumerator AnimateCombat(RecordedEvent recorded)
        {
            int lane = (int)(LaneId)Enum.Parse(typeof(LaneId), Value(recorded.Payload, "lane"));
            int tile = int.Parse(Value(recorded.Payload, "tile"));
            RectTransform site = _tiles[lane, tile - 1];
            Image flash = MakePanel("Combat flash", site, Vector2.zero, Vector2.one, new Color(1, 0.86f, 0.35f, 0.75f)).GetComponent<Image>();
            Text damage = MakeText(
                "⚔  " + Value(recorded.Payload, "playerDamage") + " / " + Value(recorded.Payload, "snapshotDamage"),
                site, new Vector2(-0.1f, 0.15f), new Vector2(1.1f, 0.85f), 15, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            float elapsed = 0;
            float duration = 0.26f / _speed;
            while (elapsed < duration && !_skipRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - (elapsed / duration);
                flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, alpha * 0.75f);
                damage.color = new Color(1, 1, 1, alpha);
                yield return null;
            }

            Destroy(flash.gameObject);
            Destroy(damage.gameObject);
        }

        private IEnumerator AnimateDeath(RecordedEvent recorded)
        {
            string id = Value(recorded.Payload, "unit");
            if (_unitViews.TryGetValue(id, out RectTransform rect))
            {
                rect.localScale = Vector3.one * 1.35f;
                yield return Delay(0.12f);
                Destroy(rect.gameObject);
                _unitViews.Remove(id);
            }
        }

        private IEnumerator AgeTransition(string age)
        {
            _overlay.gameObject.SetActive(true);
            ClearChildren(_overlay);
            MakeText("A NEW AGE\n" + AgeName(int.Parse(age)), _overlay,
                new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.62f), 30, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            yield return Delay(0.55f);
            _overlay.gameObject.SetActive(false);
        }

        private IEnumerator Delay(float seconds)
        {
            float elapsed = 0;
            float duration = seconds / _speed;
            while (elapsed < duration && !_skipRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void SkipAnimation()
        {
            if (_animating)
            {
                _skipRequested = true;
            }
        }

        private void ToggleSpeed()
        {
            _speed = _speed < 2 ? 4 : 1;
            _speedText.text = "FAST ×" + (int)_speed;
        }

        private void ShowResult()
        {
            MatchResult result = _session.State.Result!;
            _overlay.gameObject.SetActive(true);
            ClearChildren(_overlay);
            string outcome = result.Outcome.ToString();
            MakeText(outcome, _overlay, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.82f), 38,
                result.Outcome == MatchOutcome.VICTORY ? Gold : result.Outcome == MatchOutcome.DEFEAT ? Snapshot : Parchment,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            MakeText(result.PlayerScore + "  —  " + result.SnapshotScore + "\nSEED  " + result.Seed.Text,
                _overlay, new Vector2(0.1f, 0.48f), new Vector2(0.9f, 0.65f), 20, Parchment, TextAnchor.MiddleCenter, FontStyle.Bold);
            MakeText(_session.VerifyReplay() ? "REPLAY VERIFIED" : "REPLAY CHECK FAILED",
                _overlay, new Vector2(0.1f, 0.40f), new Vector2(0.9f, 0.47f), 11, Player, TextAnchor.MiddleCenter, FontStyle.Bold);
            Button replay = MakeButton("Replay", _overlay, new Vector2(0.12f, 0.25f), new Vector2(0.88f, 0.35f), Charcoal, BeginReplay);
            replay.GetComponentInChildren<Text>().text = "REPLAY MATCH";
            Button restart = MakeButton("Restart", _overlay, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.22f), Gold, RestartSameSeed);
            restart.GetComponentInChildren<Text>().text = "RESTART · SAME SEED";
            restart.GetComponentInChildren<Text>().color = Ink;
        }

        private void RestartSameSeed()
        {
            _overlay.gameObject.SetActive(false);
            _session = _session.RestartSameSeed();
            RenderChoice();
        }

        private void BeginReplay()
        {
            List<PresentationTurn> frames = new List<PresentationTurn>(_session.Turns);
            _overlay.gameObject.SetActive(false);
            StartCoroutine(Replay(frames));
        }

        private IEnumerator Replay(IReadOnlyList<PresentationTurn> frames)
        {
            _animating = true;
            for (int i = 0; i < frames.Count && !_skipRequested; i++)
            {
                RenderState(frames[i].PostState);
                _statusText.text = "REPLAY · TURN " + (i + 1) + "/24";
                yield return Delay(0.12f);
            }

            _animating = false;
            _skipRequested = false;
            RenderState(_session.State);
            if (_hostedReplayCompletion is not null)
            {
                _hostedReplayCompletion();
                Destroy(gameObject);
            }
            else
            {
                ShowResult();
            }
        }

        private void SetLaneTargeting(IReadOnlyList<LaneId>? legal)
        {
            for (int i = 0; i < 3; i++)
            {
                bool enabled = false;
                if (legal is not null)
                {
                    for (int j = 0; j < legal.Count; j++)
                    {
                        enabled |= legal[j] == (LaneId)i;
                    }
                }

                _laneTargets[i].enabled = enabled;
                _laneOutlines[i].effectColor = enabled ? Gold : new Color(0, 0, 0, 0);
            }
        }

        private void ShowHelp()
        {
            if (_animating)
            {
                return;
            }

            _overlay.gameObject.SetActive(true);
            ClearChildren(_overlay);
            MakeText("HOW TO PLAY", _overlay,
                new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.94f), 26, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            MakeText(
                "GOAL\nHave the higher score after 24 turns.\n\n" +
                "EACH TURN · PICK 1 CARD\n" +
                "BUILD = earn more resources every turn\n" +
                "TRAIN = add a unit that marches and fights\n" +
                "ADVANCE = buy a permanent upgrade\n\n" +
                "THE BOARD\nUnits march toward the gold center tile.\n" +
                "Hold that center to earn resources and score.\n\n" +
                "SHAPES\n■ Sword    ● Spear    ◆ Horse\n" +
                "The number inside is HP (health).\n\n" +
                "Teal = YOU    Coral = SNAPSHOT opponent",
                _overlay, new Vector2(0.09f, 0.23f), new Vector2(0.91f, 0.83f), 14,
                Parchment, TextAnchor.MiddleCenter, FontStyle.Normal);
            Button start = MakeButton("Start", _overlay,
                new Vector2(0.18f, 0.10f), new Vector2(0.82f, 0.19f), Gold, CloseHelp);
            start.GetComponentInChildren<Text>().text = "GOT IT · START PLAYING";
            start.GetComponentInChildren<Text>().color = Ink;
        }

        private void CloseHelp()
        {
            _overlay.gameObject.SetActive(false);
        }

        private void ShowFatal(string message)
        {
            _overlay.gameObject.SetActive(true);
            ClearChildren(_overlay);
            MakeText("EPOCH COULD NOT START\n\n" + message, _overlay,
                new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.82f), 16, Snapshot, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private static int CountStructures(PlayerState state, LaneId lane)
        {
            int count = 0;
            for (int i = 0; i < state.Structures.Count; i++)
            {
                count += state.Structures[i].LaneId == lane ? 1 : 0;
            }

            return count;
        }

        private static UnitInstance? FindUnit(RunState state, string id)
        {
            for (int i = 0; i < state.Player.Units.Count; i++)
            {
                if (state.Player.Units[i].InstanceId.Value == id) return state.Player.Units[i];
            }
            for (int i = 0; i < state.Snapshot.Units.Count; i++)
            {
                if (state.Snapshot.Units[i].InstanceId.Value == id) return state.Snapshot.Units[i];
            }
            return null;
        }

        private static string EventLabel(RecordedEvent item)
        {
            return item.EventType switch
            {
                SimulationEventType.CARD_SELECTED => item.Side + " SELECTS",
                SimulationEventType.COST_PAID => "COST PAID",
                SimulationEventType.EFFECT_APPLIED => "CARD RESOLVES",
                SimulationEventType.INCOME_GRANTED => item.Side + " INCOME",
                SimulationEventType.UNIT_MOVED => "UNITS ADVANCE",
                SimulationEventType.COMBAT_RESOLVED => "COMBAT",
                SimulationEventType.UNIT_DESTROYED => "UNIT FALLS",
                SimulationEventType.SCORE_CHANGED => item.Side + " SCORES",
                _ => item.EventType.ToString().Replace('_', ' '),
            };
        }

        private static string Value(string payload, string key)
        {
            string[] parts = payload.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                int equals = parts[i].IndexOf('=');
                if (equals > 0 && parts[i].Substring(0, equals) == key)
                {
                    return parts[i].Substring(equals + 1);
                }
            }
            return string.Empty;
        }

        private static string UnitMark(UnitInstance unit) =>
            unit.UnitClass == UnitClass.SWORD ? "SW" + (unit.HasReach ? " R" : string.Empty)
            : unit.UnitClass == UnitClass.SPEAR ? "SP" + (unit.HasReach ? " R" : string.Empty)
            : "HO" + (unit.HasReach ? " R" : string.Empty);

        private static string IllegalLabel(ValidationError error) => error switch
        {
            ValidationError.ERR_UNAFFORDABLE => "NOT ENOUGH RESOURCES",
            ValidationError.ERR_LANE_FULL => "LANES FULL",
            ValidationError.ERR_DUPLICATE_PERK => "ALREADY OWNED",
            ValidationError.ERR_KEYSTONE_ALREADY_TAKEN => "KEYSTONE TAKEN",
            ValidationError.ERR_AGE_LOCKED => "AGE LOCKED",
            _ => "UNAVAILABLE",
        };

        private static string AgeName(int age) => age switch
        {
            1 => "DAWN",
            2 => "BRONZE",
            3 => "STEEL",
            _ => "MODERN",
        };

        private static Color CardColor(CardPresentation card)
        {
            if (!card.IsLegal) return new Color(0.25f, 0.27f, 0.29f, 0.8f);
            if (card.Definition.IsKeystone) return new Color(0.58f, 0.43f, 0.16f, 1f);
            return card.Definition.CardType == CardType.BUILD ? new Color(0.20f, 0.39f, 0.28f, 1f)
                : card.Definition.CardType == CardType.TRAIN ? new Color(0.43f, 0.24f, 0.24f, 1f)
                : new Color(0.23f, 0.31f, 0.49f, 1f);
        }

        private Sprite CircleSprite()
        {
            if (_circleSprite is not null) return _circleSprite;
            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float radius = center;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                pixels[(y * size) + x] = dx * dx + dy * dy <= radius * radius ? Color.white : Color.clear;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            _circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
            return _circleSprite;
        }

        private RectTransform MakePanel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect, min, max);
            Image image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private Text MakeText(string value, Transform parent, Vector2 min, Vector2 max, int size, Color color, TextAnchor anchor, FontStyle style)
        {
            GameObject item = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect, min, max);
            Text text = item.GetComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private Button MakeButton(string name, Transform parent, Vector2 min, Vector2 max, Color color, UnityEngine.Events.UnityAction action)
        {
            RectTransform panel = MakePanel(name, parent, min, max, color);
            panel.GetComponent<Image>().raycastTarget = true;
            Button button = panel.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.55f);
            button.colors = colors;
            button.onClick.AddListener(action);
            MakeText(name, panel, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), 12, Parchment, TextAnchor.MiddleCenter, FontStyle.Bold);
            return button;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color color);
            return color;
        }
    }

    /// <summary>
    /// Modular board/lane presentation hooks so responsive reflow can retarget surfaces
    /// without rewriting combat projection.
    /// </summary>
    public static class EpochBattleBoardPresentation
    {
        private static readonly Color River = Hex("294D63");
        private static readonly Color Highland = Hex("574A3C");
        private static readonly Color Coast = Hex("245D61");

        public static Color LaneSurface(LaneModifier modifier) => modifier switch
        {
            LaneModifier.RIVER => River,
            LaneModifier.HIGHLAND => Highland,
            _ => Coast,
        };

        public static string LaneRule(LaneModifier modifier) => modifier switch
        {
            LaneModifier.RIVER => "Growth builds +1",
            LaneModifier.HIGHLAND => "Center defender +3",
            _ => "Units move 2 tiles",
        };

        public static void ApplyRegions(
            RectTransform header,
            RectTransform board,
            RectTransform hand,
            RectTransform footer,
            EpochResponsiveProfile profile)
        {
            Vector2 headerMin;
            Vector2 headerMax;
            Vector2 boardMin;
            Vector2 boardMax;
            Vector2 handMin;
            Vector2 handMax;
            Vector2 footerMin;
            Vector2 footerMax;

            if (profile == EpochResponsiveProfile.SHORT)
            {
                headerMin = new Vector2(0.025f, 0.885f);
                headerMax = new Vector2(0.975f, 0.985f);
                boardMin = new Vector2(0.025f, 0.345f);
                boardMax = new Vector2(0.975f, 0.875f);
                handMin = new Vector2(0.025f, 0.105f);
                handMax = new Vector2(0.975f, 0.335f);
                footerMin = new Vector2(0.025f, 0.015f);
                footerMax = new Vector2(0.975f, 0.095f);
            }
            else if (profile == EpochResponsiveProfile.TALL)
            {
                headerMin = new Vector2(0.04f, 0.875f);
                headerMax = new Vector2(0.96f, 0.97f);
                boardMin = new Vector2(0.04f, 0.275f);
                boardMax = new Vector2(0.96f, 0.86f);
                handMin = new Vector2(0.04f, 0.09f);
                handMax = new Vector2(0.96f, 0.26f);
                footerMin = new Vector2(0.04f, 0.02f);
                footerMax = new Vector2(0.96f, 0.08f);
            }
            else
            {
                headerMin = new Vector2(0.025f, 0.865f);
                headerMax = new Vector2(0.975f, 0.985f);
                boardMin = new Vector2(0.025f, 0.315f);
                boardMax = new Vector2(0.975f, 0.855f);
                handMin = new Vector2(0.025f, 0.09f);
                handMax = new Vector2(0.975f, 0.305f);
                footerMin = new Vector2(0.025f, 0.015f);
                footerMax = new Vector2(0.975f, 0.08f);
            }

            Stretch(header, headerMin, headerMax);
            Stretch(board, boardMin, boardMax);
            Stretch(hand, handMin, handMax);
            Stretch(footer, footerMin, footerMax);
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color color);
            return color;
        }
    }
}
