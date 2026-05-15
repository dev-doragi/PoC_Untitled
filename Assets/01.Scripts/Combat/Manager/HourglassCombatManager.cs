using System.Collections.Generic;
using System.Collections;
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
    [SerializeField] private float _flipDuration = 0.45f;
    [SerializeField] private float _enemyTurnStartDelay = 0.4f;
    [SerializeField] private float _enemyActionDelay = 0.7f;
    [SerializeField] private float _enemyTurnEndDelay = 0.35f;

    private readonly CombatActionResolver _actionResolver = new CombatActionResolver();
    private readonly CombatTurnProcessor _turnProcessor = new CombatTurnProcessor();
    private readonly Dictionary<CombatActionType, CombatActionDataSO> _actionDataByType = new Dictionary<CombatActionType, CombatActionDataSO>();
    private Coroutine _enemyTurnRoutine;

    public CombatRuntimeState RuntimeState { get; private set; }
    public float FlipDuration => Mathf.Max(0f, _flipDuration);

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
        EventBus.Instance.Publish(new CombatStartedEvent(CreateSnapshot()));
        EventBus.Instance.Publish(new CombatTurnStartedEvent(CreateSnapshot()));
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
            MaxTransferSand = _config.maxTransferSand,
            MinimumTurnSand = _config.minimumTurnSand,
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
            return;
        }

        int expectedMaxActionSand = RuntimeState.MaxTransferSand - RuntimeState.FlipTransfer;
        if (RuntimeState.Player.MaxActionSand != expectedMaxActionSand || RuntimeState.Enemy.MaxActionSand != expectedMaxActionSand)
        {
            Debug.LogWarning($"[Combat] Config warning: MaxActionSand({RuntimeState.Player.MaxActionSand}) does not match MaxTransferSand({RuntimeState.MaxTransferSand}) - FlipTransfer({RuntimeState.FlipTransfer}) = {expectedMaxActionSand}.");
        }
    }

    private void TryExecutePlayerAction(CombatActionType actionType)
    {
        if (actionType == CombatActionType.EndTurn)
        {
            EventBus.Instance.Publish(new CombatActionExecutedEvent(CreateSnapshot(CombatActorType.Player, actionType, 0, 0)));
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

        EventBus.Instance.Publish(new CombatActionExecutedEvent(CreateSnapshot(CombatActorType.Player, result.ActionType, result.SpentSand, result.DamageDealt)));
        if (result.DamageDealt > 0)
        {
            EventBus.Instance.Publish(new CombatActorDamagedEvent(CreateSnapshot(CombatActorType.Enemy, result.ActionType, result.SpentSand, result.DamageDealt)));
        }

        if (result.BreakTriggered)
        {
            EventBus.Instance.Publish(new CombatBreakTriggeredEvent(CreateSnapshot(CombatActorType.Enemy, result.ActionType, result.SpentSand, result.DamageDealt)));
            Debug.Log("[Combat] Break Triggered: Enemy");
        }

        if (result.GroggyTriggered)
        {
            EventBus.Instance.Publish(new CombatGroggyAppliedEvent(CreateSnapshot(CombatActorType.Enemy, result.ActionType, result.SpentSand, result.DamageDealt)));
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
        _turnProcessor.EndTurn(RuntimeState);
        EventBus.Instance.Publish(new CombatTurnEndedEvent(CreateSnapshot()));
        Debug.Log("[Combat] Turn Ended: PlayerTurn");
        PublishStateDebugLog();

        if (RuntimeState.IsCombatEnded)
        {
            EndCombat(RuntimeState.Enemy.IsDead);
            return;
        }

        EventBus.Instance.Publish(new CombatTurnStartedEvent(CreateSnapshot()));
        Debug.Log($"[Combat] Turn Started: {RuntimeState.TurnState}");
        if (_enemyTurnRoutine != null)
        {
            StopCoroutine(_enemyTurnRoutine);
        }

        _enemyTurnRoutine = StartCoroutine(RunEnemyTurnSequence());
    }

    private IEnumerator RunEnemyTurnSequence()
    {
        if (RuntimeState.IsCombatEnded || RuntimeState.TurnState != CombatTurnState.EnemyTurn)
        {
            yield break;
        }

        float turnStartWait = Mathf.Max(0f, _enemyTurnStartDelay, _flipDuration);
        if (turnStartWait > 0f)
        {
            yield return new WaitForSeconds(turnStartWait);
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

            EventBus.Instance.Publish(new CombatActionRequestedEvent(selectedAction));
            if (_enemyActionDelay > 0f)
            {
                yield return new WaitForSeconds(_enemyActionDelay);
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

            EventBus.Instance.Publish(new CombatActionExecutedEvent(CreateSnapshot(CombatActorType.Enemy, result.ActionType, result.SpentSand, result.DamageDealt)));
            if (result.DamageDealt > 0)
            {
                EventBus.Instance.Publish(new CombatActorDamagedEvent(CreateSnapshot(CombatActorType.Player, result.ActionType, result.SpentSand, result.DamageDealt)));
            }

            if (result.BreakTriggered)
            {
                EventBus.Instance.Publish(new CombatBreakTriggeredEvent(CreateSnapshot(CombatActorType.Player, result.ActionType, result.SpentSand, result.DamageDealt)));
                Debug.Log("[Combat] Break Triggered: Player");
            }

            if (result.GroggyTriggered)
            {
                EventBus.Instance.Publish(new CombatGroggyAppliedEvent(CreateSnapshot(CombatActorType.Player, result.ActionType, result.SpentSand, result.DamageDealt)));
                Debug.Log("[Combat] Groggy Applied: Player(Pending)");
            }

            Debug.Log($"[Combat] Action Executed: Enemy {result.ActionType}");
            PublishStateDebugLog();

            if (RuntimeState.Player.IsDead)
            {
                EndCombat(false);
                yield break;
            }

        }

        if (_enemyTurnEndDelay > 0f)
        {
            yield return new WaitForSeconds(_enemyTurnEndDelay);
        }

        EventBus.Instance.Publish(new CombatActionExecutedEvent(CreateSnapshot(CombatActorType.Enemy, CombatActionType.EndTurn, 0, 0)));
        Debug.Log("[Combat] Action Executed: Enemy EndTurn");

        if (_flipDuration > 0f)
        {
            yield return new WaitForSeconds(_flipDuration);
        }

        if (RuntimeState.Player.IsDead)
        {
            EndCombat(false);
            yield break;
        }

        EndEnemyTurn();
        _enemyTurnRoutine = null;
    }

    private void EndEnemyTurn()
    {
        _turnProcessor.EndTurn(RuntimeState);
        EventBus.Instance.Publish(new CombatTurnEndedEvent(CreateSnapshot()));
        Debug.Log("[Combat] Turn Ended: EnemyTurn");
        PublishStateDebugLog();

        if (RuntimeState.IsCombatEnded)
        {
            EndCombat(RuntimeState.Enemy.IsDead);
            return;
        }

        EventBus.Instance.Publish(new CombatTurnStartedEvent(CreateSnapshot()));
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
        EventBus.Instance.Publish(new CombatEndedEvent(playerWon, CreateSnapshot()));
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
        ValidateRuntimeInvariants();
    }

    private void ValidateRuntimeInvariants()
    {
        if (RuntimeState.Player.AvailableSand < 0 || RuntimeState.Enemy.AvailableSand < 0)
        {
            Debug.LogWarning("[Combat] Invariant warning: AvailableSand < 0 detected.");
        }

        if (RuntimeState.Player.TransferredSand > RuntimeState.Player.MaxActionSand || RuntimeState.Enemy.TransferredSand > RuntimeState.Enemy.MaxActionSand)
        {
            Debug.LogWarning("[Combat] Invariant warning: TransferredSand > MaxActionSand detected.");
        }

        if (RuntimeState.Player.TransferredSand + RuntimeState.FlipTransfer > RuntimeState.MaxTransferSand
            || RuntimeState.Enemy.TransferredSand + RuntimeState.FlipTransfer > RuntimeState.MaxTransferSand)
        {
            Debug.LogWarning("[Combat] Invariant warning: TransferredSand + FlipTransfer > MaxTransferSand detected.");
        }

        if (RuntimeState.Enemy.EnemyPrepStack > RuntimeState.PrepCap)
        {
            Debug.LogWarning("[Combat] Invariant warning: EnemyPrepStack > PrepCap detected.");
        }

        if (RuntimeState.Enemy.BreakProgress < 0)
        {
            Debug.LogWarning("[Combat] Invariant warning: BreakProgress < 0 detected.");
        }
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

    private CombatLogSnapshot CreateSnapshot(
        CombatActorType actor = CombatActorType.None,
        CombatActionType actionType = CombatActionType.None,
        int spentSand = 0,
        int damage = 0)
    {
        if (RuntimeState == null || RuntimeState.Player == null || RuntimeState.Enemy == null)
        {
            return new CombatLogSnapshot(
                0,
                CombatTurnState.None,
                actor,
                actionType,
                spentSand,
                damage,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                false,
                false);
        }

        return new CombatLogSnapshot(
            RuntimeState.TurnIndex,
            RuntimeState.TurnState,
            actor,
            actionType,
            spentSand,
            damage,
            RuntimeState.Player.CurrentHp,
            RuntimeState.Enemy.CurrentHp,
            RuntimeState.Player.AvailableSand,
            RuntimeState.Enemy.AvailableSand,
            RuntimeState.Player.TransferredSand,
            RuntimeState.Enemy.TransferredSand,
            RuntimeState.Player.GuardValue,
            RuntimeState.Enemy.EnemyPrepStack,
            RuntimeState.Enemy.BreakProgress,
            RuntimeState.Enemy.GroggyPending,
            RuntimeState.Enemy.GroggyActive);
    }
}
