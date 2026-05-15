using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds and drives a playable hourglass combat HUD using existing combat events and request APIs.
/// </summary>
[DefaultExecutionOrder(-40)]
[DisallowMultipleComponent]
public class CombatScreenPresenter : MonoBehaviour
{
    private static CombatScreenPresenter _instance;

    private readonly Color _playerColor = new Color(0.25f, 0.66f, 1f, 1f);
    private readonly Color _enemyColor = new Color(1f, 0.33f, 0.33f, 1f);
    private readonly Color _goldColor = new Color(0.98f, 0.82f, 0.34f, 1f);

    private TMP_FontAsset _font;
    private HourglassCombatManager _combatManager;

    private CombatActorPanelView _playerPanel;
    private CombatActorPanelView _enemyPanel;
    private HourglassSandView _hourglassView;
    private CombatLogView _combatLogView;
    private CombatFeedbackView _feedbackView;

    private Button _strikeButton;
    private Button _pierceButton;
    private Button _hexButton;
    private Button _guardButton;
    private Button _endTurnButton;

    private CombatLogSnapshot _lastSnapshot;
    private bool _hasSnapshot;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _font = Resources.Load<TMP_FontAsset>("TextMesh Pro/Fonts/Galmuri9 SDF");
        if (_font == null)
        {
            _font = TMP_Settings.defaultFontAsset;
        }

        EnsureUi();
        BindButtons();
        SetButtonsInteractable(false);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<CombatStartedEvent>(OnCombatStarted);
        EventBus.Instance.Subscribe<CombatTurnStartedEvent>(OnCombatTurnStarted);
        EventBus.Instance.Subscribe<CombatActionExecutedEvent>(OnCombatActionExecuted);
        EventBus.Instance.Subscribe<CombatTurnEndedEvent>(OnCombatTurnEnded);
        EventBus.Instance.Subscribe<CombatActorDamagedEvent>(OnCombatActorDamaged);
        EventBus.Instance.Subscribe<CombatBreakTriggeredEvent>(OnCombatBreakTriggered);
        EventBus.Instance.Subscribe<CombatGroggyAppliedEvent>(OnCombatGroggyApplied);
        EventBus.Instance.Subscribe<CombatEndedEvent>(OnCombatEnded);
    }

    private void Start()
    {
        CacheCombatManager();
        ApplyRuntimeConfig();
        if (_combatLogView != null)
        {
            _combatLogView.PushSystem("UI Ready", _goldColor);
        }

        TryRefreshFromRuntime();
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
        EventBus.Instance.Unsubscribe<CombatTurnStartedEvent>(OnCombatTurnStarted);
        EventBus.Instance.Unsubscribe<CombatActionExecutedEvent>(OnCombatActionExecuted);
        EventBus.Instance.Unsubscribe<CombatTurnEndedEvent>(OnCombatTurnEnded);
        EventBus.Instance.Unsubscribe<CombatActorDamagedEvent>(OnCombatActorDamaged);
        EventBus.Instance.Unsubscribe<CombatBreakTriggeredEvent>(OnCombatBreakTriggered);
        EventBus.Instance.Unsubscribe<CombatGroggyAppliedEvent>(OnCombatGroggyApplied);
        EventBus.Instance.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
    }

    private void OnCombatStarted(CombatStartedEvent evt)
    {
        ApplySnapshot(evt.Snapshot);
        _combatLogView?.PushSystem("Combat Started", _goldColor);
    }

    private void OnCombatTurnStarted(CombatTurnStartedEvent evt)
    {
        ApplySnapshot(evt.Snapshot);
        _combatLogView?.PushTurn(evt.Snapshot, _goldColor);
    }

    private void OnCombatActionExecuted(CombatActionExecutedEvent evt)
    {
        ApplySnapshot(evt.Snapshot);
        _combatLogView?.PushAction(evt.Snapshot, _playerColor, _enemyColor, _goldColor);

        if (evt.Snapshot.action_type == CombatActionType.EndTurn)
        {
            _hourglassView?.PlayFlipAnimation();
            return;
        }

        if (!IsOffensive(evt.Snapshot.action_type))
        {
            return;
        }

        if (evt.Snapshot.actor == CombatActorType.Player)
        {
            _playerPanel?.PlayAttackLunge(1f);
        }
        else if (evt.Snapshot.actor == CombatActorType.Enemy)
        {
            _enemyPanel?.PlayAttackLunge(-1f);
        }
    }

    private void OnCombatTurnEnded(CombatTurnEndedEvent evt)
    {
        ApplySnapshot(evt.Snapshot);
        _combatLogView?.PushHighlight("[TURN END]", _goldColor);
    }

    private void OnCombatActorDamaged(CombatActorDamagedEvent evt)
    {
        ApplySnapshot(evt.Snapshot);

        CombatActorPanelView target = evt.Snapshot.actor == CombatActorType.Player ? _playerPanel : _enemyPanel;
        target?.PlayHitReaction();

        Color damageColor = evt.Snapshot.actor == CombatActorType.Player ? _playerColor : _enemyColor;
        _feedbackView?.SpawnDamagePopup(target != null ? target.PopupAnchor : null, evt.Snapshot.damage, damageColor);
        _feedbackView?.PlayScreenPulse(new Color(1f, 1f, 1f, 1f));

        if (CameraManager.IsExisted && CameraManager.Instance != null)
        {
            CameraManager.Instance.ShakeWeak();
        }

        if (TimeManager.IsExisted && TimeManager.Instance != null)
        {
            EventBus.Instance.Publish(new HitStopRequestedEvent
            {
                Duration = 0.06f,
                TimeScale = 0.08f
            });
        }

        _combatLogView?.PushHighlight($"[HIT] {evt.Snapshot.damage}", evt.Snapshot.actor == CombatActorType.Player ? _playerColor : _enemyColor);
    }

    private void OnCombatBreakTriggered(CombatBreakTriggeredEvent evt)
    {
        ApplySnapshot(evt.Snapshot);

        if (evt.Snapshot.actor == CombatActorType.Enemy)
        {
            _feedbackView?.ShowBreakText(_enemyPanel != null ? _enemyPanel.PopupAnchor : null);
        }

        _combatLogView?.PushHighlight("BREAK", _goldColor);
    }

    private void OnCombatGroggyApplied(CombatGroggyAppliedEvent evt)
    {
        ApplySnapshot(evt.Snapshot);
        _combatLogView?.PushHighlight("GROGGY", new Color(0.45f, 0.9f, 1f, 1f));
    }

    private void OnCombatEnded(CombatEndedEvent evt)
    {
        ApplySnapshot(evt.Snapshot);
        _combatLogView?.PushSystem(evt.PlayerWon ? "Victory" : "Defeat", _goldColor);
        SetButtonsInteractable(false);
    }

    private void ApplySnapshot(in CombatLogSnapshot snapshot)
    {
        _lastSnapshot = snapshot;
        _hasSnapshot = true;

        int playerMaxHp = 1;
        int enemyMaxHp = 1;
        int prepCap = 3;
        int breakThreshold = 1;

        if (_combatManager != null && _combatManager.RuntimeState != null)
        {
            CombatRuntimeState state = _combatManager.RuntimeState;
            if (state.Player != null)
            {
                playerMaxHp = Mathf.Max(1, state.Player.MaxHp);
            }

            if (state.Enemy != null)
            {
                enemyMaxHp = Mathf.Max(1, state.Enemy.MaxHp);
            }

            prepCap = Mathf.Max(1, state.PrepCap);
            breakThreshold = Mathf.Max(1, state.BreakThreshold);
        }

        _playerPanel?.ApplySnapshot(snapshot, playerMaxHp, snapshot.turn_state == CombatTurnState.PlayerTurn);
        _enemyPanel?.ApplySnapshot(snapshot, enemyMaxHp, snapshot.turn_state == CombatTurnState.EnemyTurn);
        _enemyPanel?.ApplyEnemyExtra(
            breakThreshold,
            prepCap,
            snapshot.enemy_prep_stack,
            snapshot.enemy_break_progress,
            snapshot.enemy_groggy_pending,
            snapshot.enemy_groggy_active);
        _hourglassView?.ApplySnapshot(snapshot);

        bool canInput = snapshot.turn_state == CombatTurnState.PlayerTurn;
        SetButtonsInteractable(canInput);
    }

    private void TryRefreshFromRuntime()
    {
        if (_hasSnapshot)
        {
            ApplySnapshot(_lastSnapshot);
            return;
        }

        if (_combatManager == null)
        {
            return;
        }

        CombatRuntimeState state = _combatManager.RuntimeState;
        if (state == null || state.Player == null || state.Enemy == null)
        {
            return;
        }

        CombatLogSnapshot snapshot = new CombatLogSnapshot(
            state.TurnIndex,
            state.TurnState,
            CombatActorType.None,
            CombatActionType.None,
            0,
            0,
            state.Player.CurrentHp,
            state.Enemy.CurrentHp,
            state.Player.AvailableSand,
            state.Enemy.AvailableSand,
            state.Player.TransferredSand,
            state.Enemy.TransferredSand,
            state.Player.GuardValue,
            state.Enemy.EnemyPrepStack,
            state.Enemy.BreakProgress,
            state.Enemy.GroggyPending,
            state.Enemy.GroggyActive);

        ApplySnapshot(snapshot);
    }

    private void CacheCombatManager()
    {
        if (_combatManager != null)
        {
            return;
        }

        _combatManager = FindAnyObjectByType<HourglassCombatManager>();
    }

    private void ApplyRuntimeConfig()
    {
        if (_hourglassView == null)
        {
            return;
        }

        CacheCombatManager();
        if (_combatManager == null || _combatManager.RuntimeState == null || _combatManager.RuntimeState.Player == null)
        {
            _hourglassView.Configure(3, 1, 0.5f);
            return;
        }

        CombatRuntimeState state = _combatManager.RuntimeState;
        _hourglassView.Configure(state.Player.MaxActionSand, state.FlipTransfer, state.GroggyIncomingSandMultiplier);
    }

    private void BindButtons()
    {
        if (_strikeButton != null)
        {
            _strikeButton.onClick.RemoveAllListeners();
            _strikeButton.onClick.AddListener(() =>
            {
                CacheCombatManager();
                _combatManager?.RequestStrike();
            });
        }

        if (_pierceButton != null)
        {
            _pierceButton.onClick.RemoveAllListeners();
            _pierceButton.onClick.AddListener(() =>
            {
                CacheCombatManager();
                _combatManager?.RequestPierce();
            });
        }

        if (_hexButton != null)
        {
            _hexButton.onClick.RemoveAllListeners();
            _hexButton.onClick.AddListener(() =>
            {
                CacheCombatManager();
                _combatManager?.RequestHex();
            });
        }

        if (_guardButton != null)
        {
            _guardButton.onClick.RemoveAllListeners();
            _guardButton.onClick.AddListener(() =>
            {
                CacheCombatManager();
                _combatManager?.RequestGuard();
            });
        }

        if (_endTurnButton != null)
        {
            _endTurnButton.onClick.RemoveAllListeners();
            _endTurnButton.onClick.AddListener(() =>
            {
                CacheCombatManager();
                _combatManager?.RequestEndTurn();
            });
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (_strikeButton != null) _strikeButton.interactable = interactable;
        if (_pierceButton != null) _pierceButton.interactable = interactable;
        if (_hexButton != null) _hexButton.interactable = interactable;
        if (_guardButton != null) _guardButton.interactable = interactable;
        if (_endTurnButton != null) _endTurnButton.interactable = interactable;
    }

    private bool IsOffensive(CombatActionType actionType)
    {
        return actionType == CombatActionType.Strike
            || actionType == CombatActionType.Pierce
            || actionType == CombatActionType.Hex
            || actionType == CombatActionType.WeakAttack
            || actionType == CombatActionType.DesperationStrike;
    }

    private void EnsureUi()
    {
        EnsureEventSystem();

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("CombatCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        CreatePlayerPanel(canvas.transform as RectTransform);
        CreateEnemyPanel(canvas.transform as RectTransform);
        CreateHourglassPanel(canvas.transform as RectTransform);
        CreateSkillButtons(canvas.transform as RectTransform);
        CreateLogPanel(canvas.transform as RectTransform);
        CreateFeedbackLayer(canvas.transform as RectTransform);

        _playerPanel?.Initialize(true, "PLAYER", _playerColor, new Color(0.68f, 0.84f, 1f, 1f), _font);
        _enemyPanel?.Initialize(false, "ENEMY", _enemyColor, new Color(1f, 0.74f, 0.74f, 1f), _font);
        _hourglassView?.Initialize(_font, _playerColor, _enemyColor, _goldColor);
        _combatLogView?.Initialize(_font);
        _feedbackView?.Initialize(_font);
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObj = new GameObject("EventSystem", typeof(EventSystem));
        Type inputSystemType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemType != null)
        {
            eventSystemObj.AddComponent(inputSystemType);
            return;
        }

        eventSystemObj.AddComponent<StandaloneInputModule>();
    }

    private void CreatePlayerPanel(RectTransform parent)
    {
        GameObject panel = CreateUiObject("PlayerPanel", parent, new Vector2(0.02f, 0.24f), new Vector2(0.28f, 0.8f));
        Image bg = panel.AddComponent<Image>();

        GameObject motion = CreateUiObject("MotionRoot", panel.transform as RectTransform, Vector2.zero, Vector2.one);

        TMP_Text nameText = CreateText("Name", motion.transform as RectTransform, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.96f), 30f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "PLAYER");
        TMP_Text hpText = CreateText("HP", motion.transform as RectTransform, new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.8f), 28f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "HP");
        TMP_Text line1 = CreateText("Line1", motion.transform as RectTransform, new Vector2(0.06f, 0.45f), new Vector2(0.94f, 0.61f), 24f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, string.Empty);
        TMP_Text line2 = CreateText("Line2", motion.transform as RectTransform, new Vector2(0.06f, 0.29f), new Vector2(0.94f, 0.44f), 24f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, string.Empty);
        TMP_Text line3 = CreateText("Line3", motion.transform as RectTransform, new Vector2(0.06f, 0.13f), new Vector2(0.94f, 0.28f), 24f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, string.Empty);
        TMP_Text warning = CreateText("Warning", motion.transform as RectTransform, new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.12f), 20f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, string.Empty);

        Image flash = CreateImage("HitFlash", motion.transform as RectTransform, Vector2.zero, Vector2.one, Color.clear);
        flash.raycastTarget = false;

        CombatActorPanelView panelView = panel.AddComponent<CombatActorPanelView>();
        SetPrivate(panelView, "_panelRoot", panel.transform as RectTransform);
        SetPrivate(panelView, "_motionRoot", motion.transform as RectTransform);
        SetPrivate(panelView, "_popupAnchor", motion.transform as RectTransform);
        SetPrivate(panelView, "_background", bg);
        SetPrivate(panelView, "_hitFlash", flash);
        SetPrivate(panelView, "_nameText", nameText);
        SetPrivate(panelView, "_hpText", hpText);
        SetPrivate(panelView, "_line1Text", line1);
        SetPrivate(panelView, "_line2Text", line2);
        SetPrivate(panelView, "_line3Text", line3);
        SetPrivate(panelView, "_warningText", warning);
        SetPrivate(panelView, "_groggyBadge", (GameObject)null);

        _playerPanel = panelView;
    }

    private void CreateEnemyPanel(RectTransform parent)
    {
        GameObject panel = CreateUiObject("EnemyPanel", parent, new Vector2(0.72f, 0.24f), new Vector2(0.98f, 0.8f));
        Image bg = panel.AddComponent<Image>();

        GameObject motion = CreateUiObject("MotionRoot", panel.transform as RectTransform, Vector2.zero, Vector2.one);

        TMP_Text nameText = CreateText("Name", motion.transform as RectTransform, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.96f), 30f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "ENEMY");
        TMP_Text hpText = CreateText("HP", motion.transform as RectTransform, new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.8f), 28f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "HP");
        TMP_Text line1 = CreateText("Line1", motion.transform as RectTransform, new Vector2(0.06f, 0.45f), new Vector2(0.94f, 0.61f), 24f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, string.Empty);
        TMP_Text line2 = CreateText("Line2", motion.transform as RectTransform, new Vector2(0.06f, 0.29f), new Vector2(0.94f, 0.44f), 24f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, string.Empty);
        TMP_Text line3 = CreateText("Line3", motion.transform as RectTransform, new Vector2(0.06f, 0.13f), new Vector2(0.94f, 0.28f), 24f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, string.Empty);
        TMP_Text warning = CreateText("Warning", motion.transform as RectTransform, new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.12f), 20f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, string.Empty);

        Image flash = CreateImage("HitFlash", motion.transform as RectTransform, Vector2.zero, Vector2.one, Color.clear);
        flash.raycastTarget = false;

        GameObject badgeObj = CreateUiObject("GroggyBadge", motion.transform as RectTransform, new Vector2(0.58f, 0.82f), new Vector2(0.94f, 0.96f));
        Image badgeBg = badgeObj.AddComponent<Image>();
        badgeBg.color = new Color(1f, 0.78f, 0.16f, 0.95f);
        TMP_Text badgeText = CreateText("BadgeText", badgeObj.transform as RectTransform, Vector2.zero, Vector2.one, 22f, FontStyles.Bold, TextAlignmentOptions.Center, "GROGGY");
        badgeText.color = new Color(0.2f, 0.12f, 0.02f, 1f);
        badgeObj.SetActive(false);

        CombatActorPanelView panelView = panel.AddComponent<CombatActorPanelView>();
        SetPrivate(panelView, "_panelRoot", panel.transform as RectTransform);
        SetPrivate(panelView, "_motionRoot", motion.transform as RectTransform);
        SetPrivate(panelView, "_popupAnchor", motion.transform as RectTransform);
        SetPrivate(panelView, "_background", bg);
        SetPrivate(panelView, "_hitFlash", flash);
        SetPrivate(panelView, "_nameText", nameText);
        SetPrivate(panelView, "_hpText", hpText);
        SetPrivate(panelView, "_line1Text", line1);
        SetPrivate(panelView, "_line2Text", line2);
        SetPrivate(panelView, "_line3Text", line3);
        SetPrivate(panelView, "_warningText", warning);
        SetPrivate(panelView, "_groggyBadge", badgeObj);

        _enemyPanel = panelView;
    }

    private void CreateHourglassPanel(RectTransform parent)
    {
        GameObject root = CreateUiObject("HourglassPanel", parent, new Vector2(0.32f, 0.18f), new Vector2(0.68f, 0.86f));
        Image rootBg = root.AddComponent<Image>();
        rootBg.color = new Color(0.08f, 0.1f, 0.13f, 0.96f);
        HourglassSandView view = root.AddComponent<HourglassSandView>();
        view.BuildRuntimeChildren(_font);
        _hourglassView = view;
    }

    private void CreateSkillButtons(RectTransform parent)
    {
        GameObject root = CreateUiObject("SkillButtons", parent, new Vector2(0.12f, 0.02f), new Vector2(0.68f, 0.14f));
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.09f, 0.12f, 0.96f);

        HorizontalLayoutGroup layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.padding = new RectOffset(18, 18, 12, 12);

        _strikeButton = CreateButton(root.transform as RectTransform, "Strike", new Color(0.2f, 0.48f, 0.88f, 0.98f));
        _pierceButton = CreateButton(root.transform as RectTransform, "Pierce", new Color(0.2f, 0.52f, 0.88f, 0.98f));
        _hexButton = CreateButton(root.transform as RectTransform, "Hex", new Color(0.44f, 0.3f, 0.78f, 0.98f));
        _guardButton = CreateButton(root.transform as RectTransform, "Guard", new Color(0.22f, 0.64f, 0.54f, 0.98f));
        _endTurnButton = CreateButton(root.transform as RectTransform, "End Turn", new Color(0.85f, 0.36f, 0.2f, 0.98f));
    }

    private void CreateLogPanel(RectTransform parent)
    {
        GameObject root = CreateUiObject("CombatLogPanel", parent, new Vector2(0.7f, 0.02f), new Vector2(0.98f, 0.22f));
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.11f, 0.94f);

        TMP_Text logText = CreateText("LogText", root.transform as RectTransform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), 20f, FontStyles.Normal, TextAlignmentOptions.TopLeft, string.Empty);

        CombatLogView logView = root.AddComponent<CombatLogView>();
        SetPrivate(logView, "_logText", logText);
        _combatLogView = logView;
    }

    private void CreateFeedbackLayer(RectTransform parent)
    {
        GameObject root = CreateUiObject("FeedbackLayer", parent, Vector2.zero, Vector2.one);
        Image flash = CreateImage("ScreenFlash", root.transform as RectTransform, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f));
        flash.raycastTarget = false;

        GameObject popupLayer = CreateUiObject("PopupLayer", root.transform as RectTransform, Vector2.zero, Vector2.one);

        CombatFeedbackView view = root.AddComponent<CombatFeedbackView>();
        SetPrivate(view, "_popupLayer", popupLayer.transform as RectTransform);
        SetPrivate(view, "_screenFlashImage", flash);
        _feedbackView = view;
    }

    private Button CreateButton(RectTransform parent, string label, Color color)
    {
        GameObject buttonObj = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObj.transform.SetParent(parent, false);

        LayoutElement layoutElement = buttonObj.GetComponent<LayoutElement>();
        layoutElement.minHeight = 78f;

        Image image = buttonObj.GetComponent<Image>();
        image.color = color;

        Button button = buttonObj.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.08f;
        colors.pressedColor = color * 0.9f;
        colors.disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        TMP_Text text = CreateText("Label", buttonObj.transform as RectTransform, Vector2.zero, Vector2.one, 26f, FontStyles.Bold, TextAlignmentOptions.Center, label);
        text.color = Color.white;

        return button;
    }

    private GameObject CreateUiObject(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        return go;
    }

    private TMP_Text CreateText(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, float size, FontStyles style, TextAlignmentOptions anchor, string value)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TMP_Text text = textObj.GetComponent<TMP_Text>();
        text.font = _font;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = anchor;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = Color.white;
        text.text = value;
        text.raycastTarget = false;
        return text;
    }

    private Image CreateImage(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject imageObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = imageObj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObj.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void SetPrivate<TTarget, TValue>(TTarget target, string fieldName, TValue value)
        where TTarget : class
    {
        if (target == null)
        {
            return;
        }

        var field = typeof(TTarget).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(target, value);
    }
}
