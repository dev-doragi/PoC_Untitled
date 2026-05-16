using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connects inspector-wired combat UI views to combat runtime state and combat events.
/// </summary>
[DefaultExecutionOrder(-40)]
public class CombatScreenPresenter : MonoBehaviour
{
    [Header("View References")]
    [SerializeField] private CombatActorPanelView playerStatusView;
    [SerializeField] private CombatActorPanelView enemyStatusView;
    [SerializeField] private HourglassSandView hourglassView;
    [SerializeField] private CombatLogView combatLogView;
    [SerializeField] private CombatFeedbackView combatFeedbackView;
    [SerializeField] private CombatActionPanelView actionPanelView;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private SpriteRenderer enemySpriteRenderer;

    [Header("Action Costs")]
    [SerializeField] private int strikeCost = 3;
    [SerializeField] private int pierceCost = 3;
    [SerializeField] private int hexCost = 3;
    [SerializeField] private int guardCost = 2;

    [Header("Bars")]
    [SerializeField] private float playerGuardBarMax = 10f;

    private HourglassCombatManager _combatManager;

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
        EventBus.Instance.Subscribe<CombatStrikeInputEvent>(OnCombatStrikeInput);
        EventBus.Instance.Subscribe<CombatPierceInputEvent>(OnCombatPierceInput);
        EventBus.Instance.Subscribe<CombatHexInputEvent>(OnCombatHexInput);
        EventBus.Instance.Subscribe<CombatGuardInputEvent>(OnCombatGuardInput);
        EventBus.Instance.Subscribe<CombatEndTurnInputEvent>(OnCombatEndTurnInput);
    }

    private void Start()
    {
        CacheCombatManager();
        if (_combatManager != null)
        {
            hourglassView?.SetFlipDuration(_combatManager.FlipDuration);
        }

        combatFeedbackView?.Initialize();
        actionPanelView?.SetStaticTexts();
        BindButtons();
        RefreshViews();
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
        EventBus.Instance.Unsubscribe<CombatStrikeInputEvent>(OnCombatStrikeInput);
        EventBus.Instance.Unsubscribe<CombatPierceInputEvent>(OnCombatPierceInput);
        EventBus.Instance.Unsubscribe<CombatHexInputEvent>(OnCombatHexInput);
        EventBus.Instance.Unsubscribe<CombatGuardInputEvent>(OnCombatGuardInput);
        EventBus.Instance.Unsubscribe<CombatEndTurnInputEvent>(OnCombatEndTurnInput);
    }

    private void OnCombatStarted(CombatStartedEvent evt)
    {
        combatLogView?.AddLog("Combat Started");
        RefreshViews();
    }

    private void OnCombatTurnStarted(CombatTurnStartedEvent evt)
    {
        combatLogView?.AddLog(evt.Snapshot.turn_state == CombatTurnState.PlayerTurn ? "Player Turn Start" : "Enemy Turn Start");
        RefreshViews();
    }

    private void OnCombatActionExecuted(CombatActionExecutedEvent evt)
    {
        combatLogView?.AddLog($"{evt.Snapshot.actor} used {evt.Snapshot.action_type}");

        if (evt.Snapshot.action_type == CombatActionType.EndTurn)
        {
            CacheCombatManager();
            CombatRuntimeState state = _combatManager != null ? _combatManager.RuntimeState : null;
            if (state != null)
            {
                CombatActorRuntime actor = state.GetActor(evt.Snapshot.turn_state);
                int maxActionSand = actor != null ? actor.MaxActionSand : 3;
                hourglassView?.PrepareFlipTransferPreview(evt.Snapshot, maxActionSand, state.FlipTransfer);
            }
        }

        if (evt.Snapshot.actor == CombatActorType.Player)
        {
            playerStatusView?.PlayAttackLunge(1f);
        }
        else if (evt.Snapshot.actor == CombatActorType.Enemy)
        {
            enemyStatusView?.PlayAttackLunge(-1f);
        }

        RefreshViews();
    }

    private void OnCombatTurnEnded(CombatTurnEndedEvent evt)
    {
        combatLogView?.AddLog($"Turn {evt.Snapshot.turn_index} End");
        RefreshViews();
    }

    private void OnCombatActorDamaged(CombatActorDamagedEvent evt)
    {
        CombatActorPanelView target = evt.Snapshot.actor == CombatActorType.Player ? playerStatusView : enemyStatusView;
        target?.PlayHitReaction();
        combatFeedbackView?.SpawnDamagePopup(target?.PopupAnchor, evt.Snapshot.damage, evt.Snapshot.actor == CombatActorType.Player ? new Color(1f, 0.35f, 0.35f, 1f) : new Color(1f, 0.65f, 0.3f, 1f));
        combatFeedbackView?.PlayScreenPulse();
        combatLogView?.AddLog($"Damage {evt.Snapshot.damage}");
        RefreshViews();
    }

    private void OnCombatBreakTriggered(CombatBreakTriggeredEvent evt)
    {
        CombatActorPanelView target = evt.Snapshot.actor == CombatActorType.Player ? playerStatusView : enemyStatusView;
        combatFeedbackView?.ShowBreakText(target?.PopupAnchor);
        combatFeedbackView?.PlayScreenPulse();
        combatLogView?.AddLog("BREAK");
        RefreshViews();
    }

    private void OnCombatGroggyApplied(CombatGroggyAppliedEvent evt)
    {
        CombatActorPanelView target = evt.Snapshot.actor == CombatActorType.Player ? playerStatusView : enemyStatusView;
        combatFeedbackView?.ShowGroggyText(target?.PopupAnchor);
        combatFeedbackView?.PlayScreenPulse();
        combatLogView?.AddLog("GROGGY");
        RefreshViews();
    }

    private void OnCombatEnded(CombatEndedEvent evt)
    {
        combatLogView?.AddLog(evt.PlayerWon ? "Victory" : "Defeat");
        RefreshViews();
        hourglassView?.SetResultText(evt.PlayerWon);
        SetAllButtonsInteractable(false);
    }

    private void CacheCombatManager()
    {
        if (_combatManager == null)
        {
            _combatManager = HourglassCombatManager.Instance;
        }

        if (_combatManager == null)
        {
            _combatManager = FindAnyObjectByType<HourglassCombatManager>();
        }
    }

    private void BindButtons()
    {
        if (actionPanelView == null)
        {
            return;
        }

        BindButton(actionPanelView.StrikeButton, RequestStrike);
        BindButton(actionPanelView.PierceButton, RequestPierce);
        BindButton(actionPanelView.HexButton, RequestHex);
        BindButton(actionPanelView.GuardButton, RequestGuard);
        BindButton(actionPanelView.EndTurnButton, RequestEndTurn);
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void RefreshViews()
    {
        CacheCombatManager();
        CombatRuntimeState state = _combatManager != null ? _combatManager.RuntimeState : null;
        if (state == null || state.Player == null || state.Enemy == null)
        {
            SetAllButtonsInteractable(false);
            return;
        }

        bool isPlayerTurn = state.TurnState == CombatTurnState.PlayerTurn;
        bool isEnemyTurn = state.TurnState == CombatTurnState.EnemyTurn;

        playerStatusView?.ApplyPlayerState(state.Player, isPlayerTurn, Mathf.Max(1f, playerGuardBarMax));
        enemyStatusView?.ApplyEnemyState(state.Enemy, state.MaxEnemyGuard, state.PrepCap, isEnemyTurn);
        UpdateActorSpriteVisibility(state);
        hourglassView?.Refresh(state);
        UpdateEndTurnPreview(state);
        UpdateActionButtons(state);
    }

    private void UpdateActionButtons(CombatRuntimeState state)
    {
        bool isPlayerTurn = state != null && state.TurnState == CombatTurnState.PlayerTurn && state.Player != null;
        CombatActorRuntime player = state != null ? state.Player : null;

        if (actionPanelView != null)
        {
            actionPanelView.SetInteractable(
                isPlayerTurn && CanUse(player, strikeCost),
                isPlayerTurn && CanUse(player, pierceCost),
                isPlayerTurn && CanUse(player, hexCost),
                isPlayerTurn && CanUse(player, guardCost),
                isPlayerTurn);
        }
    }

    private static bool CanUse(CombatActorRuntime actor, int cost)
    {
        if (actor == null)
        {
            return false;
        }

        if (cost <= 0)
        {
            return false;
        }

        if (actor.AvailableSand < cost)
        {
            return false;
        }

        return actor.TransferredSand + cost <= actor.MaxActionSand;
    }

    private void SetAllButtonsInteractable(bool interactable)
    {
        actionPanelView?.SetInteractable(interactable, interactable, interactable, interactable, interactable);
    }

    private void UpdateEndTurnPreview(CombatRuntimeState state)
    {
        if (actionPanelView == null || state == null)
        {
            return;
        }

        CombatActorRuntime currentActor = state.GetActor(state.TurnState);
        if (currentActor == null)
        {
            actionPanelView.SetEndTurnPreview(false, 0);
            return;
        }

        int preview = Mathf.Max(0, currentActor.TransferredSand + state.FlipTransfer);
        bool nextIsEnemy = state.TurnState == CombatTurnState.PlayerTurn;
        if (nextIsEnemy && state.Enemy != null && (state.Enemy.GroggyPending || state.Enemy.GroggyActive))
        {
            preview = Mathf.Max(0, Mathf.CeilToInt(preview * state.GroggyIncomingSandMultiplier));
        }

        actionPanelView.SetEndTurnPreview(nextIsEnemy, preview);
    }

    private void UpdateActorSpriteVisibility(CombatRuntimeState state)
    {
        if (state == null)
        {
            return;
        }

        if (playerSpriteRenderer != null)
        {
            playerSpriteRenderer.gameObject.SetActive(state.Player != null && state.Player.CurrentHp > 0);
        }

        if (enemySpriteRenderer != null)
        {
            enemySpriteRenderer.gameObject.SetActive(state.Enemy != null && state.Enemy.CurrentHp > 0);
        }
    }

    private void OnCombatStrikeInput(CombatStrikeInputEvent evt) => TryRequestFromInput(actionPanelView != null ? actionPanelView.StrikeButton : null, RequestStrike);
    private void OnCombatPierceInput(CombatPierceInputEvent evt) => TryRequestFromInput(actionPanelView != null ? actionPanelView.PierceButton : null, RequestPierce);
    private void OnCombatHexInput(CombatHexInputEvent evt) => TryRequestFromInput(actionPanelView != null ? actionPanelView.HexButton : null, RequestHex);
    private void OnCombatGuardInput(CombatGuardInputEvent evt) => TryRequestFromInput(actionPanelView != null ? actionPanelView.GuardButton : null, RequestGuard);
    private void OnCombatEndTurnInput(CombatEndTurnInputEvent evt) => TryRequestFromInput(actionPanelView != null ? actionPanelView.EndTurnButton : null, RequestEndTurn);

    private static void TryRequestFromInput(Button button, System.Action request)
    {
        if (button == null || request == null || !button.interactable)
        {
            return;
        }

        request.Invoke();
    }

    private void RequestStrike()
    {
        CacheCombatManager();
        _combatManager?.RequestStrike();
    }

    private void RequestPierce()
    {
        CacheCombatManager();
        _combatManager?.RequestPierce();
    }

    private void RequestHex()
    {
        CacheCombatManager();
        _combatManager?.RequestHex();
    }

    private void RequestGuard()
    {
        CacheCombatManager();
        _combatManager?.RequestGuard();
    }

    private void RequestEndTurn()
    {
        CacheCombatManager();
        _combatManager?.RequestEndTurn();
    }

}
