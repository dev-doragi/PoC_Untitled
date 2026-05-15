using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles hourglass combat turn flow.
/// Owns combat state, receives player action requests, runs enemy turns,
/// and publishes combat events.
/// </summary>
[DefaultExecutionOrder(-70)]
public class HourglassCombatManager : Singleton<HourglassCombatManager>
{
    private const int DesperationMinSand = 1;

    [SerializeField] private HourglassCombatConfigSO _config;
    [SerializeField] private CombatActorDataSO _playerData;
    [SerializeField] private CombatActorDataSO _enemyData;
    [SerializeField] private CombatActionDataSO[] _actionDatas;

    private readonly CombatActionResolver _actionResolver = new CombatActionResolver();
    private readonly CombatTurnProcessor _turnProcessor = new CombatTurnProcessor();
    private readonly Dictionary<CombatActionType, CombatActionDataSO> _actionDataByType = new Dictionary<CombatActionType, CombatActionDataSO>();

    public CombatRuntimeState RuntimeState { get; private set; }

    protected override void OnBootstrap()
    {
        InitializeRuntime();
    }

    public void StartCombat()
    {
        InitializeRuntime();
        if (RuntimeState == null)
        {
            return;
        }

        RuntimeState.TurnState = CombatTurnState.PlayerTurn;
        EventBus.Instance.Publish(new CombatStartedEvent(RuntimeState.Player, RuntimeState.Enemy));
        EventBus.Instance.Publish(new CombatTurnStartedEvent(RuntimeState.TurnState));
        Debug.Log("[Combat] Combat Started");
        Debug.Log($"[Combat] Turn Started: {RuntimeState.TurnState}");
        PublishStateDebugLog();
    }

    public void RequestStrike()
    {
        RequestPlayerAction(CombatActionType.Strike);
    }

    public void RequestPierce()
    {
        RequestPlayerAction(CombatActionType.Pierce);
    }

    public void RequestHex()
    {
        RequestPlayerAction(CombatActionType.Hex);
    }

    public void RequestGuard()
    {
        RequestPlayerAction(CombatActionType.Guard);
    }

    public void RequestEndTurn()
    {
        RequestPlayerAction(CombatActionType.EndTurn);
    }

    private void RequestPlayerAction(CombatActionType actionType)
    {
        if (RuntimeState == null)
        {
            Debug.LogWarning($"[Combat] Action Failed: {actionType} | Reason: RuntimeState is not initialized.");
            return;
        }

        if (RuntimeState.IsCombatEnded)
        {
            Debug.LogWarning($"[Combat] Action Failed: {actionType} | Reason: CombatEnded state");
            return;
        }

        if (RuntimeState.TurnState != CombatTurnState.PlayerTurn)
        {
            Debug.LogWarning($"[Combat] Action Failed: {actionType} | Reason: current turn is not PlayerTurn");
            return;
        }

        TryExecutePlayerAction(actionType);
    }

    private void InitializeRuntime()
    {
        if (!ValidateAndBuildActionData())
        {
            RuntimeState = null;
            return;
        }

        RuntimeState = new CombatRuntimeState
        {
            TurnIndex = 0,
            TurnState = CombatTurnState.None,
            IsCombatEnded = false,
            FlipTransfer = _config.flipTransfer,
            BreakThreshold = _config.breakThreshold,
            PrepCap = _config.prepCap,
            GroggyIncomingSandMultiplier = _config.groggyIncomingSandMultiplier,
            Player = CombatActorRuntime.CreateFromData(_playerData, _config.defaultPlayerSand, _config.maxActionSand),
            Enemy = CombatActorRuntime.CreateFromData(_enemyData, _config.defaultEnemySand, _config.maxActionSand)
        };

        if (RuntimeState.Player == null || RuntimeState.Enemy == null)
        {
            Debug.LogError("[Combat] Failed to create actor runtime from actor data.", this);
            RuntimeState = null;
        }
    }

    private void TryExecutePlayerAction(CombatActionType actionType)
    {
        if (actionType == CombatActionType.EndTurn)
        {
            EventBus.Instance.Publish(new CombatActionExecutedEvent(CombatActorType.Player, actionType, 0, 0));
            Debug.Log("[Combat] Action Executed: Player EndTurn");
            EndPlayerTurn();
            return;
        }

        CombatActionDataSO actionData = GetActionData(actionType);
        if (actionData == null)
        {
            Debug.LogError($"[Combat] Action Failed: {actionType} | Reason: ActionData missing", this);
            return;
        }

        CombatActionResult result = _actionResolver.Resolve(
            RuntimeState.Player,
            RuntimeState.Enemy,
            actionData,
            RuntimeState.BreakThreshold,
            RuntimeState.PrepCap);

        if (!result.Succeeded)
        {
            Debug.LogWarning($"[Combat] Action Failed: {actionType} | Reason: {result.FailureReason}");
            return;
        }

        EventBus.Instance.Publish(new CombatActionExecutedEvent(CombatActorType.Player, result.ActionType, result.DamageDealt, result.SpentSand));
        if (result.DamageDealt > 0)
        {
            EventBus.Instance.Publish(new CombatActorDamagedEvent(CombatActorType.Enemy, result.DamageDealt));
        }

        if (result.BreakTriggered)
        {
            EventBus.Instance.Publish(new CombatBreakTriggeredEvent(CombatActorType.Enemy));
            Debug.Log("[Combat] Break Triggered: Enemy");
        }

        if (result.GroggyTriggered)
        {
            EventBus.Instance.Publish(new CombatGroggyAppliedEvent(CombatActorType.Enemy));
            Debug.Log("[Combat] Groggy Applied: Enemy(Pending)");
        }

        Debug.Log($"[Combat] Action Executed: Player {result.ActionType}");
        PublishStateDebugLog();

        if (RuntimeState.Enemy.IsDead)
        {
            EndCombat(true);
        }
    }

    private void EndPlayerTurn()
    {
        CombatTurnState endedTurn = RuntimeState.TurnState;
        _turnProcessor.EndTurn(RuntimeState);
        EventBus.Instance.Publish(new CombatTurnEndedEvent(endedTurn));
        Debug.Log("[Combat] Turn Ended: PlayerTurn");
        PublishStateDebugLog();

        if (RuntimeState.IsCombatEnded)
        {
            EndCombat(RuntimeState.Enemy.IsDead);
            return;
        }

        EventBus.Instance.Publish(new CombatTurnStartedEvent(RuntimeState.TurnState));
        Debug.Log($"[Combat] Turn Started: {RuntimeState.TurnState}");
        RunEnemyTurn();
    }

    private void RunEnemyTurn()
    {
        if (RuntimeState.IsCombatEnded || RuntimeState.TurnState != CombatTurnState.EnemyTurn)
        {
            return;
        }

        CombatActorRuntime enemy = RuntimeState.Enemy;
        CombatActionDataSO weakAttackData = GetActionData(CombatActionType.WeakAttack);
        CombatActionDataSO prepareData = GetActionData(CombatActionType.Prepare);
        CombatActionDataSO desperationData = GetActionData(CombatActionType.DesperationStrike);
        bool usedPrepareThisTurn = false;

        while (!RuntimeState.IsCombatEnded && enemy.AvailableSand > 0)
        {
            if (enemy.TransferredSand >= enemy.MaxActionSand)
            {
                break;
            }

            CombatActionType selectedAction = CombatActionType.EndTurn;
            if (enemy.GroggyActive)
            {
                if (weakAttackData != null && enemy.CanSpend(weakAttackData.sandCost))
                {
                    selectedAction = CombatActionType.WeakAttack;
                }
            }
            else
            {
                bool canDesperation = desperationData != null
                    && weakAttackData != null
                    && enemy.EnemyPrepStack == RuntimeState.PrepCap
                    && enemy.AvailableSand >= DesperationMinSand
                    && enemy.AvailableSand < weakAttackData.sandCost;
                if (canDesperation)
                {
                    selectedAction = CombatActionType.DesperationStrike;
                }
                else if (weakAttackData != null && enemy.CanSpend(weakAttackData.sandCost))
                {
                    selectedAction = CombatActionType.WeakAttack;
                }
                else if (!usedPrepareThisTurn && prepareData != null && enemy.CanSpend(prepareData.sandCost) && enemy.EnemyPrepStack < RuntimeState.PrepCap)
                {
                    selectedAction = CombatActionType.Prepare;
                }
            }

            if (selectedAction == CombatActionType.EndTurn)
            {
                break;
            }

            CombatActionDataSO actionData = GetActionData(selectedAction);
            if (actionData == null)
            {
                Debug.LogError($"[Combat] Action Failed: {selectedAction} | Reason: ActionData missing", this);
                break;
            }

            CombatActionResult result = _actionResolver.Resolve(
                RuntimeState.Enemy,
                RuntimeState.Player,
                actionData,
                RuntimeState.BreakThreshold,
                RuntimeState.PrepCap);

            if (!result.Succeeded)
            {
                Debug.LogWarning($"[Combat] Action Failed: {selectedAction} | Reason: {result.FailureReason}");
                break;
            }

            if (selectedAction == CombatActionType.Prepare)
            {
                usedPrepareThisTurn = true;
            }

            EventBus.Instance.Publish(new CombatActionExecutedEvent(CombatActorType.Enemy, result.ActionType, result.DamageDealt, result.SpentSand));
            if (result.DamageDealt > 0)
            {
                EventBus.Instance.Publish(new CombatActorDamagedEvent(CombatActorType.Player, result.DamageDealt));
            }

            if (result.BreakTriggered)
            {
                EventBus.Instance.Publish(new CombatBreakTriggeredEvent(CombatActorType.Player));
                Debug.Log("[Combat] Break Triggered: Player");
            }

            if (result.GroggyTriggered)
            {
                EventBus.Instance.Publish(new CombatGroggyAppliedEvent(CombatActorType.Player));
                Debug.Log("[Combat] Groggy Applied: Player(Pending)");
            }

            Debug.Log($"[Combat] Action Executed: Enemy {result.ActionType}");
            PublishStateDebugLog();

            if (RuntimeState.Player.IsDead)
            {
                EndCombat(false);
                return;
            }
        }

        EventBus.Instance.Publish(new CombatActionExecutedEvent(CombatActorType.Enemy, CombatActionType.EndTurn, 0, 0));
        Debug.Log("[Combat] Action Executed: Enemy EndTurn");

        if (RuntimeState.Player.IsDead)
        {
            EndCombat(false);
            return;
        }

        EndEnemyTurn();
    }

    private void EndEnemyTurn()
    {
        CombatTurnState endedTurn = RuntimeState.TurnState;
        _turnProcessor.EndTurn(RuntimeState);
        EventBus.Instance.Publish(new CombatTurnEndedEvent(endedTurn));
        Debug.Log("[Combat] Turn Ended: EnemyTurn");
        PublishStateDebugLog();

        if (RuntimeState.IsCombatEnded)
        {
            EndCombat(RuntimeState.Enemy.IsDead);
            return;
        }

        EventBus.Instance.Publish(new CombatTurnStartedEvent(RuntimeState.TurnState));
        Debug.Log($"[Combat] Turn Started: {RuntimeState.TurnState}");
    }

    private void EndCombat(bool playerWon)
    {
        if (RuntimeState.IsCombatEnded && RuntimeState.TurnState == CombatTurnState.Ended)
        {
            return;
        }

        RuntimeState.IsCombatEnded = true;
        RuntimeState.TurnState = CombatTurnState.Ended;
        EventBus.Instance.Publish(new CombatEndedEvent(playerWon));
        Debug.Log($"[Combat] Combat Ended: {(playerWon ? "Victory" : "Defeat")}");
        PublishStateDebugLog();
    }

    private void PublishStateDebugLog()
    {
        if (RuntimeState == null || RuntimeState.Player == null || RuntimeState.Enemy == null)
        {
            return;
        }

        Debug.Log($"[Combat] HP Changed - Player:{RuntimeState.Player.CurrentHp}/{RuntimeState.Player.MaxHp}, Enemy:{RuntimeState.Enemy.CurrentHp}/{RuntimeState.Enemy.MaxHp}");
        Debug.Log($"[Combat] flip transfer:{RuntimeState.FlipTransfer} | Sand Changed - Player A:{RuntimeState.Player.AvailableSand} T:{RuntimeState.Player.TransferredSand}, Enemy A:{RuntimeState.Enemy.AvailableSand} T:{RuntimeState.Enemy.TransferredSand}");
        Debug.Log($"[Combat] State - TurnIndex:{RuntimeState.TurnIndex} TurnState:{RuntimeState.TurnState} PlayerGuardValue:{RuntimeState.Player.GuardValue} EnemyPrepStack:{RuntimeState.Enemy.EnemyPrepStack} EnemyBreakProgress:{RuntimeState.Enemy.BreakProgress} EnemyGroggyPending:{RuntimeState.Enemy.GroggyPending} EnemyGroggyActive:{RuntimeState.Enemy.GroggyActive}");
    }

    private CombatActionDataSO GetActionData(CombatActionType actionType)
    {
        _actionDataByType.TryGetValue(actionType, out CombatActionDataSO data);
        return data;
    }

    private bool ValidateAndBuildActionData()
    {
        if (_config == null || _playerData == null || _enemyData == null || _actionDatas == null)
        {
            Debug.LogError("[Combat] Missing required SO references.", this);
            return false;
        }

        _actionDataByType.Clear();
        for (int i = 0; i < _actionDatas.Length; i++)
        {
            CombatActionDataSO data = _actionDatas[i];
            if (data == null)
            {
                continue;
            }

            if (_actionDataByType.ContainsKey(data.actionType))
            {
                Debug.LogError($"[Combat] Duplicate action data type detected: {data.actionType}", this);
                return false;
            }

            _actionDataByType[data.actionType] = data;
        }

        if (!HasRequiredActionData(CombatActionType.Strike)) return false;
        if (!HasRequiredActionData(CombatActionType.Pierce)) return false;
        if (!HasRequiredActionData(CombatActionType.Hex)) return false;
        if (!HasRequiredActionData(CombatActionType.Guard)) return false;
        if (!HasRequiredActionData(CombatActionType.Prepare)) return false;
        if (!HasRequiredActionData(CombatActionType.WeakAttack)) return false;
        if (!HasRequiredActionData(CombatActionType.DesperationStrike)) return false;

        return true;
    }

    private bool HasRequiredActionData(CombatActionType actionType)
    {
        if (_actionDataByType.ContainsKey(actionType))
        {
            return true;
        }

        Debug.LogError($"[Combat] Missing required action data: {actionType}", this);
        return false;
    }
}
