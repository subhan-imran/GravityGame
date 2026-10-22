using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GravityGame
{
    public sealed class MobileGameUI : MonoBehaviour
    {
        public SphericalPlayer player;
        public PlanetCamera cameraRig;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform safeRoot;
        [SerializeField] private GameObject gameplay;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject lossPanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private MobileTouchControl joystick;
        [SerializeField] private MobileTouchControl lookArea;
        [SerializeField] private MobileTouchControl jumpControl;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRetryButton;
        [SerializeField] private Button lossRetryButton;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Text starCounter;
        [SerializeField] private Text status;
        [SerializeField] private Text hint;
        [SerializeField] private Text jumpLabel;
        [SerializeField] private Image jumpImage;
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private MobileTutorial tutorial;
        [SerializeField] private MobileGameFeedback feedback;
        [SerializeField] private Button skipTutorialButton;
        [SerializeField] private Button replayTutorialButton;
        public MobileTutorial Tutorial => tutorial;
        public MobileGameFeedback Feedback => feedback;
        public Button SkipTutorialButton => skipTutorialButton;
        public Button ReplayTutorialButton => replayTutorialButton;
        private Rect lastSafeArea;
        private Vector2 lastScreen;
        private bool ownsPause;
        private float resumeTimeScale = 1f;
        private int lastRespawn;
        private bool wasLost;
        private bool wasWon;
        private bool changedMobileSettings;
        private int previousFrameRate;
        private ScreenOrientation previousOrientation;
        private bool previousLeft, previousRight, previousPortrait, previousUpsideDown;

        private static readonly Color Navy = new Color(0.035f, 0.075f, 0.14f, 0.96f);
        private static readonly Color Blue = new Color(0.12f, 0.58f, 0.84f, 1f);
        private static readonly Color Gold = new Color(1f, 0.78f, 0.36f, 1f);
        public MobileTouchControl Joystick => joystick;
        public MobileTouchControl LookArea => lookArea;
        public MobileTouchControl JumpControl => jumpControl;
        public Button PauseButton => pauseButton;
        public Button ResumeButton => resumeButton;
        public Button RetryButton => lossRetryButton;
        public Button PlayAgainButton => playAgainButton;
        public RectTransform SafeRoot => safeRoot;
        public EventSystem Events => eventSystem;
        public bool PauseVisible => pausePanel != null && pausePanel.activeSelf;
        public bool LossVisible => lossPanel != null && lossPanel.activeSelf;
        public string StarCounterText => starCounter != null ? starCounter.text : "";
        public bool VictoryVisible => victoryPanel != null && victoryPanel.activeSelf;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void PrepareMobileDisplay()
        {
            if (Application.isMobilePlatform) LockLandscape();
        }

        private static void LockLandscape()
        {
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;
            // Force the initial rotation instead of inheriting the phone's portrait state.
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        private void Awake() => Initialize();

        public void Initialize()
        {
            BuildUI();
            player.useMobileUI = true;
            cameraRig.assisted = true;
            var module = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (module == null) module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            if (module.actionsAsset == null) module.AssignDefaultActions();
            // Touch controls must not also navigate/select menu buttons.
            eventSystem.sendNavigationEvents = false;
            pauseButton.onClick.RemoveListener(PauseGame);
            pauseButton.onClick.AddListener(PauseGame);
            resumeButton.onClick.RemoveListener(ResumeGame);
            resumeButton.onClick.AddListener(ResumeGame);
            pauseRetryButton.onClick.RemoveListener(RetryFromCheckpoint);
            pauseRetryButton.onClick.AddListener(RetryFromCheckpoint);
            lossRetryButton.onClick.RemoveListener(RetryFromCheckpoint);
            lossRetryButton.onClick.AddListener(RetryFromCheckpoint);
            playAgainButton.onClick.RemoveListener(PlayAgain);
            playAgainButton.onClick.AddListener(PlayAgain);
            skipTutorialButton.onClick.RemoveListener(tutorial.Skip);
            skipTutorialButton.onClick.AddListener(tutorial.Skip);
            replayTutorialButton.onClick.RemoveListener(ReplayTutorial);
            replayTutorialButton.onClick.AddListener(ReplayTutorial);
            tutorial.Initialize();
            feedback.Initialize();
            lastRespawn = player.RespawnCount;
            if (Application.isPlaying && Application.isMobilePlatform && !changedMobileSettings)
            {
                changedMobileSettings = true;
                previousFrameRate = Application.targetFrameRate;
                previousOrientation = Screen.orientation;
                previousLeft = Screen.autorotateToLandscapeLeft;
                previousRight = Screen.autorotateToLandscapeRight;
                previousPortrait = Screen.autorotateToPortrait;
                previousUpsideDown = Screen.autorotateToPortraitUpsideDown;
                LockLandscape();
                Application.targetFrameRate = 60;
            }
            Cursor.lockState = CursorLockMode.None;
            Refresh();
        }

        public void BuildUI()
        {
            if (canvas != null)
            {
                BuildVictoryUI();
                BuildObjectiveUI();
                BuildCoachingUI();
                return;
            }
            var canvasObject = new GameObject("Mobile Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            safeRoot = Rect("Safe Area", canvas.transform);
            Stretch(safeRoot);
            gameplay = Rect("Gameplay", safeRoot).gameObject;
            Stretch((RectTransform)gameplay.transform);

            var look = Image("Drag to look", gameplay.transform, Color.clear);
            look.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            look.rectTransform.anchorMax = Vector2.one;
            look.rectTransform.offsetMin = look.rectTransform.offsetMax = Vector2.zero;
            lookArea = Touch(look, MobileControlKind.Look);

            var header = Image("Status card", gameplay.transform, Navy, true);
            Place(header.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(650f, 116f));
            header.raycastTarget = false;
            Label("Title", header.transform, "GRAVITY GAME", 20, TextAnchor.MiddleLeft, new Vector2(22f, -22f), new Vector2(600f, 28f));
            status = Label("Gravity status", header.transform, "AZURE  /  GRAVITY ACTIVE", 28, TextAnchor.MiddleLeft, new Vector2(22f, -56f), new Vector2(600f, 36f));
            hint = Label("Jump hint", header.transform, "Push the stick fully to run", 20, TextAnchor.MiddleLeft, new Vector2(22f, -92f), new Vector2(600f, 28f));
            pauseButton = MakeButton("Pause", gameplay.transform, "PAUSE", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(118f, 96f), Navy);

            var baseImage = Image("Movement joystick", gameplay.transform, new Color(0.08f, 0.22f, 0.34f, 0.86f), false, true);
            Place(baseImage.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(166f, 160f), new Vector2(220f, 220f));
            var knobImage = Image("Thumb", baseImage.transform, new Color(0.36f, 0.85f, 0.95f), false, true);
            Place(knobImage.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(88f, 88f));
            knobImage.raycastTarget = false;
            joystick = Touch(baseImage, MobileControlKind.Joystick);
            joystick.knob = knobImage.rectTransform;
            var moveLabel = Label("Move caption", gameplay.transform, "MOVE / RUN", 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(240f, 36f));
            Place(moveLabel.rectTransform, Vector2.zero, Vector2.one * 0.5f, new Vector2(166f, 32f), new Vector2(240f, 36f));

            var jumpButton = MakeButton("Jump", gameplay.transform, "JUMP", new Vector2(1f, 0f), Vector2.one * 0.5f, new Vector2(-142f, 160f), new Vector2(176f, 176f), Blue, true);
            jumpImage = jumpButton.image;
            jumpLabel = jumpButton.GetComponentInChildren<Text>();
            jumpControl = Touch(jumpImage, MobileControlKind.Jump);
            var caption = Label("Flight caption", gameplay.transform, "EXPLORE THE PLANETS. COLLECT EVERY STAR.", 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(560f, 48f));
            Place(caption.rectTransform, new Vector2(0.5f, 0f), Vector2.one * 0.5f, new Vector2(0f, 32f), new Vector2(560f, 48f));

            pausePanel = Modal("Pause overlay", "Paused", "Your jump can wait.\nResume when you're ready.", true);
            lossPanel = Modal("Loss overlay", "Lost in space", "You missed the next planet.\nTry again from your last safe landing.", false);
            pausePanel.SetActive(false);
            lossPanel.SetActive(false);
            BuildVictoryUI();
            BuildObjectiveUI();
            BuildCoachingUI();
            eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var events = new GameObject("Mobile Event System", typeof(EventSystem));
                events.transform.SetParent(transform, false);
                eventSystem = events.GetComponent<EventSystem>();
            }
        }

        private void BuildVictoryUI()
        {
            if (victoryPanel != null) return;
            victoryPanel = Modal("Victory overlay", "All stars collected!", "You explored the planets and found every star.", false, true);
            victoryPanel.SetActive(false);
        }

        private void BuildObjectiveUI()
        {
            if (starCounter == null)
            {
                var card = Image("Star counter card", gameplay.transform, Navy);
                Place(card.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-166f, -24f), new Vector2(196f, 96f));
                card.raycastTarget = false;
                starCounter = Label("Star count", card.transform, "STARS  0 / " + (player.stars != null ? player.stars.TotalCount : 0), 26, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(196f, 96f));
                Stretch(starCounter.rectTransform);
                starCounter.color = Gold;
            }
            // Also upgrade existing authored UI without rebuilding its controls.
            victoryPanel.transform.Find("Dialog/Heading").GetComponent<Text>().text = "All stars collected!";
            victoryPanel.transform.Find("Dialog/Description").GetComponent<Text>().text = "You explored the planets and found every star.";
            gameplay.transform.Find("Flight caption").GetComponent<Text>().text = "EXPLORE THE PLANETS. COLLECT EVERY STAR.";
        }

        private void BuildCoachingUI()
        {
            if (tutorial == null) tutorial = GetComponent<MobileTutorial>();
            if (tutorial == null) tutorial = gameObject.AddComponent<MobileTutorial>();
            tutorial.player = player;
            if (tutorial.card == null)
            {
                var card = Image("Tutorial card", gameplay.transform, Navy);
                card.raycastTarget = false;
                Place(card.rectTransform, new Vector2(0.5f, 0f), Vector2.one * 0.5f, new Vector2(0f, 166f), new Vector2(560f, 180f));
                tutorial.card = card.gameObject;
                tutorial.heading = Label("Tutorial heading", card.transform, "1 / 5  MOVE", 22, TextAnchor.MiddleLeft, new Vector2(18f, -26f), new Vector2(410f, 30f));
                tutorial.heading.color = Gold;
                tutorial.heading.resizeTextForBestFit = true;
                tutorial.heading.resizeTextMinSize = 16;
                tutorial.heading.resizeTextMaxSize = 22;
                tutorial.instructions = Label("Tutorial instructions", card.transform, "Drag the left stick to move.\nPush it farther to run.", 21, TextAnchor.MiddleLeft, new Vector2(18f, -110f), new Vector2(410f, 116f));
                tutorial.instructions.resizeTextForBestFit = true;
                tutorial.instructions.resizeTextMinSize = 16;
                tutorial.instructions.resizeTextMaxSize = 21;
                skipTutorialButton = MakeButton("Skip tutorial", card.transform, "SKIP", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(110f, 76f), new Color(0.18f, 0.28f, 0.4f));
                skipTutorialButton.GetComponentInChildren<Text>().fontSize = 18;
            }
            Place((RectTransform)tutorial.card.transform, new Vector2(0.5f, 0f), Vector2.one * 0.5f,
                new Vector2(0f, 166f), new Vector2(560f, 180f));
            Place(tutorial.heading.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(18f, -26f), new Vector2(410f, 30f));
            Place(tutorial.instructions.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(18f, -110f), new Vector2(410f, 116f));
            Place((RectTransform)skipTutorialButton.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-12f, 0f), new Vector2(110f, 76f));
            if (feedback == null) feedback = GetComponent<MobileGameFeedback>();
            if (feedback == null) feedback = gameObject.AddComponent<MobileGameFeedback>();
            feedback.player = player;
            feedback.cameraRig = cameraRig;
            if (feedback.toast == null)
            {
                feedback.toast = Label("Gameplay feedback", gameplay.transform, "", 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(600f, 44f));
                Place(feedback.toast.rectTransform, new Vector2(0.5f, 0f), Vector2.one * 0.5f, new Vector2(0f, 290f), new Vector2(600f, 44f));
                var pulse = Image("Landing pulse", jumpControl.transform, Color.clear, false, true);
                Stretch(pulse.rectTransform);
                pulse.rectTransform.SetAsFirstSibling();
                pulse.raycastTarget = false;
                feedback.landingPulse = pulse;
            }
            Place(feedback.toast.rectTransform, new Vector2(0.5f, 0f), Vector2.one * 0.5f,
                new Vector2(0f, 290f), new Vector2(600f, 44f));
            if (replayTutorialButton == null)
            {
                var dialog = pausePanel.transform.Find("Dialog");
                replayTutorialButton = MakeButton("Replay tutorial", dialog, "REPLAY TUTORIAL", Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -74f), new Vector2(432f, 76f), new Color(0.18f, 0.28f, 0.4f));
                Place((RectTransform)resumeButton.transform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, 16f), new Vector2(432f, 76f));
                Place((RectTransform)pauseRetryButton.transform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -164f), new Vector2(432f, 76f));
            }
        }

        public void ReplayTutorial()
        {
            ResumeGame();
            ResetControls();
            tutorial.StartTutorial();
            Refresh();
        }

        private GameObject Modal(string name, string title, string message, bool pause, bool victory = false)
        {
            var shade = Image(name, safeRoot, new Color(0.01f, 0.025f, 0.06f, 0.85f));
            Stretch(shade.rectTransform);
            var panel = Image("Dialog", shade.transform, Navy, true);
            Place(panel.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(560f, 480f));
            var heading = Label("Heading", panel.transform, title, 44, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(500f, 64f));
            Place(heading.rectTransform, new Vector2(0.5f, 1f), Vector2.one * 0.5f, new Vector2(0f, -64f), new Vector2(500f, 64f));
            var description = Label("Description", panel.transform, message, 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(500f, 80f));
            Place(description.rectTransform, new Vector2(0.5f, 1f), Vector2.one * 0.5f, new Vector2(0f, -146f), new Vector2(500f, 80f));
            if (victory)
            {
                heading.color = Gold;
                playAgainButton = MakeButton("Play again", panel.transform, "PLAY AGAIN", Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -130f), new Vector2(432f, 100f), Gold);
                playAgainButton.GetComponentInChildren<Text>().color = Navy;
            }
            else if (pause)
            {
                resumeButton = MakeButton("Resume", panel.transform, "RESUME", Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -40f), new Vector2(432f, 96f), Blue);
                pauseRetryButton = MakeButton("Retry", panel.transform, "RETRY FROM CHECKPOINT", Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -154f), new Vector2(432f, 96f), new Color(0.18f, 0.28f, 0.4f));
            }
            else lossRetryButton = MakeButton("Try again", panel.transform, "TRY AGAIN", Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -130f), new Vector2(432f, 100f), Blue);
            return shade.gameObject;
        }

        private MobileTouchControl Touch(Image image, MobileControlKind kind)
        {
            var control = image.gameObject.AddComponent<MobileTouchControl>();
            control.kind = kind;
            control.player = player;
            control.cameraRig = cameraRig;
            return control;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Image Image(string name, Transform parent, Color color, bool rounded = false, bool circle = false)
        {
            var node = Rect(name, parent).gameObject;
            Image image = circle ? node.AddComponent<MobileCircleImage>() : node.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text Label(string name, Transform parent, string value, int fontSize, TextAnchor alignment, Vector2 position, Vector2 size)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), position, size);
            return text;
        }

        private static Button MakeButton(string name, Transform parent, string value, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color color, bool circle = false)
        {
            var image = Image(name, parent, color, !circle, circle);
            Place(image.rectTransform, anchor, pivot, position, size);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            var label = Label("Label", image.transform, value, circle ? 30 : 24, TextAnchor.MiddleCenter, Vector2.zero, size);
            Stretch(label.rectTransform);
            return button;
        }

        private void Update()
        {
            if (player == null || canvas == null) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !player.HasLost && !player.HasWon)
            {
                if (player.IsPaused) ResumeGame(); else PauseGame();
            }
            if (lastRespawn != player.RespawnCount || wasLost != player.HasLost || wasWon != player.HasWon) ResetControls();
            lastRespawn = player.RespawnCount;
            wasLost = player.HasLost;
            wasWon = player.HasWon;
            tutorial.Tick(Time.deltaTime);
            feedback.Tick(Time.deltaTime);
            Refresh();
        }

        public void Refresh()
        {
            if (canvas == null || player == null) return;
            bool playing = !player.HasLost && !player.HasWon && !player.IsPaused;
            gameplay.SetActive(playing);
            pausePanel.SetActive(player.IsPaused && !player.HasLost && !player.HasWon);
            lossPanel.SetActive(player.HasLost && !player.HasWon);
            victoryPanel.SetActive(player.HasWon);
            status.text = player.IsInGravityField && player.planet != null
                ? player.planet.name.ToUpperInvariant() + "  /  GRAVITY ACTIVE"
                : "SPACE  /  " + player.SpaceTimeRemaining.ToString("0.0") + "s TO FIND GRAVITY";
            if (player.stars != null)
            {
                string progress = "STARS  " + player.stars.CollectedCount + " / " + player.stars.TotalCount;
                if (starCounter.text != progress) starCounter.text = progress;
            }
            hint.fontSize = 18;
            hint.text = player.IsGrounded
                ? player.JumpChain > 0 ? "Jump again in " + player.JumpWindowRemaining.ToString("0.0") + "s  /  next " + player.NextJump + "/3"
                    : "Chain 3 jumps to escape gravity - running is optional"
                : player.JumpChain > 0 && player.JumpChain < 3 ? "Jump " + player.JumpChain + "/3 - tap again just after landing"
                    : "Explore freely. Find all the stars to win.";
            jumpImage.color = player.EscapeJumpReady ? Gold : Blue;
            jumpLabel.text = player.EscapeJumpReady ? "ESCAPE\n3 / 3" : "JUMP\n" + player.NextJump + " / 3";
            jumpLabel.color = player.EscapeJumpReady ? Navy : Color.white;
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            if (screen != lastScreen || Screen.safeArea != lastSafeArea) ApplySafeArea(Screen.safeArea, screen);
            FitTutorialCard();
        }

        public void ApplySafeArea(Rect area, Vector2 screen)
        {
            if (safeRoot == null || screen.x <= 0f || screen.y <= 0f) return;
            safeRoot.anchorMin = new Vector2(Mathf.Clamp01(area.xMin / screen.x), Mathf.Clamp01(area.yMin / screen.y));
            safeRoot.anchorMax = new Vector2(Mathf.Clamp01(area.xMax / screen.x), Mathf.Clamp01(area.yMax / screen.y));
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            lastScreen = screen;
            lastSafeArea = area;
            Canvas.ForceUpdateCanvases();
            FitTutorialCard();
        }

        private void FitTutorialCard()
        {
            if (tutorial == null || tutorial.card == null || safeRoot == null) return;
            var card = (RectTransform)tutorial.card.transform;
            float width = Mathf.Clamp(safeRoot.rect.width - 580f, 280f, 560f);
            if (Mathf.Approximately(card.sizeDelta.x, width)) return;
            card.sizeDelta = new Vector2(width, 180f);
            tutorial.heading.rectTransform.sizeDelta = new Vector2(width - 150f, 30f);
            tutorial.instructions.rectTransform.sizeDelta = new Vector2(width - 150f, 116f);
        }

        public void PauseGame()
        {
            if (player == null || player.HasLost || player.HasWon || player.IsPaused) return;
            ownsPause = true;
            resumeTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            player.SetPaused(true);
            if (feedback != null) feedback.Tick(0f);
            ResetControls();
            Cursor.lockState = CursorLockMode.None;
            Refresh();
        }

        public void ResumeGame()
        {
            if (player == null || !player.IsPaused) return;
            player.SetPaused(false);
            if (ownsPause) Time.timeScale = resumeTimeScale;
            ownsPause = false;
            ResetControls();
            Refresh();
        }

        public void RetryFromCheckpoint()
        {
            ResumeGame();
            ResetControls();
            player.Respawn();
            feedback.Clear();
            tutorial.Tick(0f);
            Refresh();
        }

        public void PlayAgain()
        {
            ResumeGame();
            ResetControls();
            player.RestartRun();
            feedback.Clear();
            tutorial.Tick(0f);
            Refresh();
        }

        public void ResetControls()
        {
            if (joystick != null) joystick.ResetInput();
            if (lookArea != null) lookArea.ResetInput();
            if (jumpControl != null) jumpControl.ResetInput();
            if (player != null) player.ClearInput();
            if (cameraRig != null) cameraRig.ClearTouchLook();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) PauseGame();
            else if (changedMobileSettings) LockLandscape();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) PauseGame();
            else if (changedMobileSettings) LockLandscape();
        }

        private void OnDisable()
        {
            ResetControls();
            if (feedback != null) feedback.Clear();
            if (ownsPause)
            {
                Time.timeScale = resumeTimeScale;
                ownsPause = false;
                if (player != null) player.SetPaused(false);
            }
            if (changedMobileSettings)
            {
                Application.targetFrameRate = previousFrameRate;
                Screen.autorotateToLandscapeLeft = previousLeft;
                Screen.autorotateToLandscapeRight = previousRight;
                Screen.autorotateToPortrait = previousPortrait;
                Screen.autorotateToPortraitUpsideDown = previousUpsideDown;
                Screen.orientation = previousOrientation;
                changedMobileSettings = false;
            }
        }
    }
}
