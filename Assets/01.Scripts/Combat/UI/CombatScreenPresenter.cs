using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connects inspector-wired combat UI views to combat runtime state and combat events.
/// </summary>
[DefaultExecutionOrder(-40)]
public class CombatScreenPresenter : MonoBehaviour
{
    private const string TurnIndexColor = "#7F8FA6";
    private const string PlayerTurnColor = "#5EC8FF";
    private const string EnemyTurnColor = "#FF9B7A";
    private const string SpecialEventColor = "#FFD966";
    private const string BonusTurnColor = "#7CFF9B";
    private const string DamageColor = "#FFB4A2";
    private const string ActionColor = "#EAF2FF";

    [Header("View References")]
    [SerializeField] private CombatActorPanelView playerStatusView;
    [SerializeField] private CombatActorPanelView enemyStatusView;
    [SerializeField] private HourglassSandView hourglassView;
    [SerializeField] private CombatLogView combatLogView;
    [SerializeField] private CombatFeedbackView combatFeedbackView;
    [SerializeField] private CombatActionPanelView actionPanelView;

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
        EventBus.Instance.Subscribe<CombatMinimumFallAppliedEvent>(OnCombatMinimumFallApplied);
        EventBus.Instance.Subscribe<CombatBonusTurnGrantedEvent>(OnCombatBonusTurnGranted);
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
        EventBus.Instance.Unsubscribe<CombatMinimumFallAppliedEvent>(OnCombatMinimumFallApplied);
        EventBus.Instance.Unsubscribe<CombatBonusTurnGrantedEvent>(OnCombatBonusTurnGranted);
        EventBus.Instance.Unsubscribe<CombatStrikeInputEvent>(OnCombatStrikeInput);
        EventBus.Instance.Unsubscribe<CombatPierceInputEvent>(OnCombatPierceInput);
        EventBus.Instance.Unsubscribe<CombatHexInputEvent>(OnCombatHexInput);
        EventBus.Instance.Unsubscribe<CombatGuardInputEvent>(OnCombatGuardInput);
        EventBus.Instance.Unsubscribe<CombatEndTurnInputEvent>(OnCombatEndTurnInput);
    }

    private void OnCombatStarted(CombatStartedEvent evt)
    {
        combatLogView?.AddLog($"{FormatTurnPrefix(evt.Snapshot.turn_index)}{FormatSpecial("전투 시작")}");
        RefreshViews();
    }

    private void OnCombatTurnStarted(CombatTurnStartedEvent evt)
    {
        combatLogView?.AddLog($"{FormatTurnPrefix(evt.Snapshot.turn_index)}{FormatTurnHeadline(evt.Snapshot.turn_state, "시작")}");
        RefreshViews();
    }

    private void OnCombatActionExecuted(CombatActionExecutedEvent evt)
    {
        if (evt.Snapshot.action_type == CombatActionType.EndTurn)
        {
            combatLogView?.AddLog($"{FormatTurnPrefix(evt.Snapshot.turn_index)}{FormatSpecial("플립")}");
        }
        else
        {
            string actor = evt.Snapshot.actor == CombatActorType.Player ? "플레이어" : "적";
            combatLogView?.AddLog($"{FormatTurnPrefix(evt.Snapshot.turn_index)}<color={ActionColor}>{actor} {ToKoreanActionName(evt.Snapshot.action_type)} 사용</color>");
        }

        if (evt.Snapshot.action_type == CombatActionType.EndTurn)
        {
            CacheCombatManager();
            CombatRuntimeState state = _combatManager != null ? _combatManager.RuntimeState : null;
            if (state != null)
            {
                CombatActorRuntime actor = state.GetActor(evt.Snapshot.turn_state);
                if (actor != null)
                {
                    hourglassView?.QueueFlipPreview(evt.Snapshot, state);
                }
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
        combatLogView?.AddLog($"{FormatTurnPrefix(evt.Snapshot.turn_index)}<b><color={ActionColor}>턴 종료</color></b>");
        RefreshViews();
    }

    private void OnCombatActorDamaged(CombatActorDamagedEvent evt)
    {
        CombatActorPanelView target = evt.Snapshot.actor == CombatActorType.Player ? playerStatusView : enemyStatusView;
        target?.PlayHitReaction();
        combatFeedbackView?.SpawnDamagePopup(target?.PopupAnchor, evt.Snapshot.damage, evt.Snapshot.actor == CombatActorType.Player ? new Color(1f, 0.35f, 0.35f, 1f) : new Color(1f, 0.65f, 0.3f, 1f));
        combatFeedbackView?.PlayScreenPulse();
        string victim = evt.Snapshot.actor == CombatActorType.Player ? "플레이어" : "적";
        combatLogView?.AddLog($"{FormatTurnPrefix(evt.Snapshot.turn_index)}<color={DamageColor}>{victim}에게 {evt.Snapshot.damage} 피해</color>");
        RefreshViews();
    }

    private void OnCombatBreakTriggered(CombatBreakTriggeredEvent evt)
    {
        CombatActorPanelView target = evt.Snapshot.actor == CombatActorType.Player ? playerStatusView : enemyStatusView;
        combatFeedbackView?.ShowBreakText(target?.PopupAnchor);
        combatFeedbackView?.PlayScreenPulse();
        combatLogView?.AddLog($"{FormatTurnPrefix(evt.Snapshot.turn_index)}{FormatSpecial(">>> 브레이크!")}");
        RefreshViews();
    }

    private void OnCombatGroggyApplied(CombatGroggyAppliedEvent evt)
    {
        CombatActorPanelView target = evt.Snapshot.actor == CombatActorType.Player ? playerStatusView : enemyStatusView;
        combatFeedbackView?.ShowGroggyText(target?.PopupAnchor);
        combatFeedbackView?.PlayScreenPulse();
        combatLogView?.AddLog($"{FormatTurnPrefix(evt.Snapshot.turn_index)}{FormatSpecial(">>> 그로기")}");
        RefreshViews();
    }

    private void OnCombatEnded(CombatEndedEvent evt)
    {
        combatLogView?.AddLog($"{FormatTurnPrefix(evt.Snapshot.turn_index)}{FormatSpecial(evt.PlayerWon ? "승리" : "패배")}");
        RefreshViews();
        hourglassView?.SetResultText(evt.PlayerWon);
        SetAllButtonsInteractable(false);
    }

    private void OnCombatMinimumFallApplied(CombatMinimumFallAppliedEvent evt)
    {
        if (evt.ForcedAmount <= 0)
        {
            return;
        }

        CacheCombatManager();
        string actor = evt.Actor == CombatActorType.Player ? "플레이어" : "적";
        int turnIndex = _combatManager != null && _combatManager.RuntimeState != null ? _combatManager.RuntimeState.TurnIndex : 0;
        combatLogView?.AddLog($"{FormatTurnPrefix(turnIndex)}{FormatSpecial($">>> 최소 낙하 +{evt.ForcedAmount} ({actor})")}");
        RefreshViews();
    }

    private void OnCombatBonusTurnGranted(CombatBonusTurnGrantedEvent evt)
    {
        int turnIndex = evt.Snapshot.turn_index;
        string actor = evt.Actor == CombatActorType.Player ? "플레이어" : "적";
        combatLogView?.AddLog($"{FormatTurnPrefix(turnIndex)}<b><color={BonusTurnColor}>>> 보너스 턴! {actor}이(가) 한 번 더 행동</color></b>");
    }

    private void CacheCombatManager()
    {
        if (_combatManager == null)
        {
            _combatManager = HourglassCombatManager.Instance;
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
        enemyStatusView?.ApplyEnemyState(state.Enemy, state.MaxEnemyGuard, state.ThreatCap, isEnemyTurn);
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

        return true;
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

        int preview = ComputeNextUpperAfterMinimumFall(currentActor.AvailableSand, currentActor.TransferredSand, state.MinimumFall);
        bool nextIsEnemy = state.TurnState == CombatTurnState.PlayerTurn;
        if (nextIsEnemy && state.Enemy != null && (state.Enemy.GroggyPending || state.Enemy.GroggyActive))
        {
            nextIsEnemy = false;
        }

        actionPanelView.SetEndTurnPreview(nextIsEnemy, preview);
    }

    private void OnCombatStrikeInput(CombatStrikeInputEvent evt) => TryRequestFromInput(actionPanelView != null ? actionPanelView.StrikeButton : null, RequestStrike);
    private void OnCombatPierceInput(CombatPierceInputEvent evt) => TryRequestFromInput(actionPanelView != null ? actionPanelView.PierceButton : null, RequestPierce);
    private void OnCombatHexInput(CombatHexInputEvent evt) => TryRequestFromInput(actionPanelView != null ? actionPanelView.HexButton : null, RequestHex);
    private void OnCombatGuardInput(CombatGuardInputEvent evt) => TryRequestFromInput(actionPanelView != null ? actionPanelView.GuardButton : null, RequestGuard);
    private void OnCombatEndTurnInput(CombatEndTurnInputEvent evt) => TryRequestFromInput(actionPanelView != null ? actionPanelView.EndTurnButton : null, RequestEndTurn);

    private void TryRequestFromInput(Button button, System.Action request)
    {
        if (hourglassView != null && hourglassView.IsTransitioning)
        {
            return;
        }

        if (button == null || request == null || !button.interactable)
        {
            return;
        }

        request.Invoke();
    }

    private void RequestStrike()
    {
        if (hourglassView != null && hourglassView.IsTransitioning) return;
        CacheCombatManager();
        _combatManager?.RequestStrike();
    }

    private void RequestPierce()
    {
        if (hourglassView != null && hourglassView.IsTransitioning) return;
        CacheCombatManager();
        _combatManager?.RequestPierce();
    }

    private void RequestHex()
    {
        if (hourglassView != null && hourglassView.IsTransitioning) return;
        CacheCombatManager();
        _combatManager?.RequestHex();
    }

    private void RequestGuard()
    {
        if (hourglassView != null && hourglassView.IsTransitioning) return;
        CacheCombatManager();
        _combatManager?.RequestGuard();
    }

    private void RequestEndTurn()
    {
        if (hourglassView != null && hourglassView.IsTransitioning) return;
        CacheCombatManager();
        _combatManager?.RequestEndTurn();
    }

    private static string FormatTurnPrefix(int turnIndex)
    {
        return $"<color={TurnIndexColor}>[T{Mathf.Max(0, turnIndex):00}]</color> ";
    }

    private static string FormatTurnHeadline(CombatTurnState turnState, string phase)
    {
        if (turnState == CombatTurnState.PlayerTurn)
        {
            return $"<b><color={PlayerTurnColor}>플레이어 턴 {phase}</color></b>";
        }

        if (turnState == CombatTurnState.EnemyTurn)
        {
            return $"<b><color={EnemyTurnColor}>적 턴 {phase}</color></b>";
        }

        return $"<b><color={ActionColor}>{turnState} {phase}</color></b>";
    }

    private static string FormatSpecial(string message)
    {
        return $"<b><color={SpecialEventColor}>{message}</color></b>";
    }

    private static string ToKoreanActionName(CombatActionType actionType)
    {
        switch (actionType)
        {
            case CombatActionType.Strike: return "강타";
            case CombatActionType.Pierce: return "관통";
            case CombatActionType.Hex: return "저주";
            case CombatActionType.Guard: return "방어";
            case CombatActionType.EndTurn: return "턴 종료";
            case CombatActionType.RecoverGuard: return "가드 회복";
            case CombatActionType.WeakAttack: return "약공격";
            case CombatActionType.HeavyAttack: return "강공격";
            case CombatActionType.HeavyAttackPlus: return "강공격+";
            case CombatActionType.DesperationStrike: return "발악";
            case CombatActionType.DoubleAction: return "연속 행동";
            default: return "알 수 없음";
        }
    }

    private static int ComputeNextUpperAfterMinimumFall(int availableSand, int transferredSand, int minimumFall)
    {
        int upper = Mathf.Max(0, availableSand);
        int lower = Mathf.Max(0, transferredSand);
        int safeMinimum = Mathf.Max(0, minimumFall);
        if (safeMinimum <= 0 || lower >= safeMinimum)
        {
            return lower;
        }

        int forcedFall = Mathf.Min(safeMinimum - lower, upper);
        return lower + forcedFall;
    }

}
