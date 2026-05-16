using System.Collections;
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
    private enum EnemyIntentType
    {
        None = 0,
        RecoverGuard = 1,
        WeakAttack = 2,
        HeavyAttack = 3,
        HeavyAttackPlus = 4,
        DesperationStrike = 5,
        DoubleAction = 6
    }

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

        RuntimeState.TurnIndex = 0;
        RuntimeState.TurnState = CombatTurnState.PlayerTurn;
        RuntimeState.IsCombatEnded = false;
        RuntimeState.SyncActorSand();

        EventBus.Instance.Publish(new CombatStartedEvent(CreateSnapshot()));
        EventBus.Instance.Publish(new CombatTurnStartedEvent(CreateSnapshot()));
        Debug.Log("[Combat] Combat Started");
        Debug.Log($"[Combat] Turn Started: {RuntimeState.TurnState}");
        PublishStateDebugLog();
    }

    public void RequestStrike() => RequestPlayerAction(CombatActionType.Strike);
    public void RequestPierce() => RequestPlayerAction(CombatActionType.Pierce);
    public void RequestHex() => RequestPlayerAction(CombatActionType.Hex);
    public void RequestGuard() => RequestPlayerAction(CombatActionType.Guard);
    public void RequestEndTurn() => RequestPlayerAction(CombatActionType.EndTurn);

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

        int totalSand = Mathf.Max(1, _config.totalSand);
        int lockedSand = Mathf.Clamp(_config.lockedSand, 0, Mathf.Max(0, totalSand - 1));
        int unlockedSand = Mathf.Max(1, totalSand - lockedSand);
        int enemyGuard = Mathf.Max(1, _enemyData.baseGuard > 0 ? _enemyData.baseGuard : _config.breakThreshold);

        RuntimeState = new CombatRuntimeState
        {
            TurnIndex = 0,
            TurnState = CombatTurnState.None,
            IsCombatEnded = false,
            TotalSand = totalSand,
            LockedSand = lockedSand,
            MinimumFall = Mathf.Max(0, _config.minimumFall),
            MaxEnemyGuard = enemyGuard,
            ThreatCap = Mathf.Max(1, _config.threatCap),
            EnemyThreatGainPerTurn = Mathf.Max(0, _config.enemyThreatGainPerTurn),
            HexThreatDelta = _config.hexThreatDelta,
            BreakThreatDelta = _config.breakThreatDelta,
            ResetThreatOnBreak = _config.resetThreatOnBreak,
            EnemyRecoverGuardAmount = Mathf.Max(0, _config.enemyRecoverGuardAmount),
            EnemyHighSandRecoverGuardBonus = Mathf.Max(0, _config.enemyHighSandRecoverGuardBonus),
            EnemyWeakDamage = Mathf.Max(0, _config.enemyWeakDamage),
            EnemyHeavyDamage = Mathf.Max(0, _config.enemyHeavyDamage),
            EnemyHeavyPlusDamage = Mathf.Max(0, _config.enemyHeavyPlusDamage),
            EnemyDesperationDamage = Mathf.Max(0, _config.enemyDesperationDamage),
            EnemyDoubleActionFirstDamage = Mathf.Max(0, _config.enemyDoubleActionFirstDamage),
            EnemyDoubleActionSecondDamage = Mathf.Max(0, _config.enemyDoubleActionSecondDamage),
            AllowThreatMaxDoubleAction = _config.allowThreatMaxDoubleAction,
            Player = CombatActorRuntime.CreateFromData(_playerData, 0, unlockedSand),
            Enemy = CombatActorRuntime.CreateFromData(_enemyData, 0, unlockedSand, enemyGuard)
        };

        if (RuntimeState.Player == null || RuntimeState.Enemy == null)
        {
            Debug.LogError("[Combat] Failed to create actor runtime from actor data.", this);
            RuntimeState = null;
            return;
        }

        RuntimeState.UpperSand = Mathf.Clamp(_config.defaultPlayerSand, 0, unlockedSand);
        RuntimeState.LowerSand = unlockedSand - RuntimeState.UpperSand;
        RuntimeState.SyncActorSand();
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
            RuntimeState.MaxEnemyGuard,
            RuntimeState.HexThreatDelta);

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
            ApplyBreakThreatReduction(RuntimeState.Enemy);
            EventBus.Instance.Publish(new CombatBreakTriggeredEvent(CreateSnapshot(CombatActorType.Enemy, result.ActionType, result.SpentSand, result.DamageDealt)));
            Debug.Log("[Combat] Break Triggered: Enemy");
        }

        if (result.GroggyTriggered)
        {
            EventBus.Instance.Publish(new CombatGroggyAppliedEvent(CreateSnapshot(CombatActorType.Enemy, result.ActionType, result.SpentSand, result.DamageDealt)));
            Debug.Log("[Combat] Groggy Applied: Enemy(Pending)");
        }

        UpdateHourglassFromCurrentActor();
        Debug.Log($"[Combat] Action Executed: Player {result.ActionType}");
        PublishStateDebugLog();

        if (RuntimeState.Enemy.IsDead)
        {
            EndCombat(true);
        }
    }

    private void EndPlayerTurn()
    {
        CombatTurnProcessor.TurnTransitionResult transitionResult = _turnProcessor.EndTurn(RuntimeState);
        PublishTurnTransitionEvents(transitionResult);
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

        if (RuntimeState.TurnState != CombatTurnState.EnemyTurn)
        {
            return;
        }

        if (_enemyTurnRoutine != null)
        {
            StopCoroutine(_enemyTurnRoutine);
        }

        _enemyTurnRoutine = StartCoroutine(RunEnemyTurnSequence());
    }

    private IEnumerator RunEnemyTurnSequence()
    {
        if (RuntimeState == null || RuntimeState.IsCombatEnded || RuntimeState.TurnState != CombatTurnState.EnemyTurn)
        {
            yield break;
        }

        float turnStartWait = Mathf.Max(0f, _enemyTurnStartDelay, _flipDuration);
        if (turnStartWait > 0f)
        {
            yield return new WaitForSeconds(turnStartWait);
        }

        CombatActorRuntime enemy = RuntimeState.Enemy;
        bool threatMaxAtTurnStart = enemy != null && enemy.EnemyThreat >= RuntimeState.ThreatCap;
        bool enemyActed = false;

        if (!RuntimeState.IsCombatEnded && enemy != null && !enemy.IsDead)
        {
            EnemyIntentType intent = DetermineEnemyIntent(enemy);
            if (intent != EnemyIntentType.None)
            {
                EventBus.Instance.Publish(new CombatActionRequestedEvent(MapIntentToActionType(intent)));
                if (_enemyActionDelay > 0f)
                {
                    yield return new WaitForSeconds(_enemyActionDelay);
                }

                enemyActed = ExecuteEnemyIntent(enemy, intent, threatMaxAtTurnStart);
            }
        }

        ConsumeEnemyRemainingSand();
        ApplyEnemyThreatOnTurnEnd(threatMaxAtTurnStart, enemyActed);
        UpdateHourglassFromCurrentActor();
        PublishStateDebugLog();

        if (RuntimeState.Player.IsDead)
        {
            EndCombat(false);
            yield break;
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
        CombatTurnProcessor.TurnTransitionResult transitionResult = _turnProcessor.EndTurn(RuntimeState);
        PublishTurnTransitionEvents(transitionResult);
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

    private EnemyIntentType DetermineEnemyIntent(CombatActorRuntime enemy)
    {
        if (enemy == null || enemy.IsDead)
        {
            return EnemyIntentType.None;
        }

        int upperSand = Mathf.Max(0, enemy.AvailableSand);
        bool threatMax = enemy.EnemyThreat >= RuntimeState.ThreatCap;

        if (threatMax)
        {
            if (upperSand <= 2) return EnemyIntentType.DesperationStrike;
            if (upperSand <= 4) return EnemyIntentType.HeavyAttack;
            if (upperSand <= 6) return EnemyIntentType.HeavyAttackPlus;
            if (RuntimeState.AllowThreatMaxDoubleAction) return EnemyIntentType.DoubleAction;
            return EnemyIntentType.HeavyAttackPlus;
        }

        if (upperSand <= 2) return EnemyIntentType.RecoverGuard;
        if (upperSand <= 4) return EnemyIntentType.WeakAttack;
        if (upperSand <= 6) return EnemyIntentType.HeavyAttack;
        return EnemyIntentType.HeavyAttackPlus;
    }

    private bool ExecuteEnemyIntent(CombatActorRuntime enemy, EnemyIntentType intent, bool threatMaxAtTurnStart)
    {
        if (enemy == null || RuntimeState == null || intent == EnemyIntentType.None)
        {
            return false;
        }

        CombatActionType actionType = MapIntentToActionType(intent);
        int damage = 0;
        int guardGain = 0;

        if (intent == EnemyIntentType.RecoverGuard)
        {
            guardGain = RuntimeState.EnemyRecoverGuardAmount;
        }
        else if (intent == EnemyIntentType.WeakAttack)
        {
            damage = RuntimeState.EnemyWeakDamage;
        }
        else if (intent == EnemyIntentType.HeavyAttack)
        {
            damage = RuntimeState.EnemyHeavyDamage;
        }
        else if (intent == EnemyIntentType.HeavyAttackPlus)
        {
            damage = RuntimeState.EnemyHeavyPlusDamage;
            if (!threatMaxAtTurnStart && enemy.AvailableSand >= 7)
            {
                guardGain = RuntimeState.EnemyHighSandRecoverGuardBonus;
            }
        }
        else if (intent == EnemyIntentType.DesperationStrike)
        {
            damage = RuntimeState.EnemyDesperationDamage;
        }
        else if (intent == EnemyIntentType.DoubleAction)
        {
            int first = CombatActionResolver.ApplyEnemyIntentDamage(enemy, RuntimeState.Player, RuntimeState.EnemyDoubleActionFirstDamage);
            int second = CombatActionResolver.ApplyEnemyIntentDamage(enemy, RuntimeState.Player, RuntimeState.EnemyDoubleActionSecondDamage);
            damage = first + second;
        }

        if (guardGain > 0)
        {
            CombatActionResolver.ApplyEnemyRecoverGuard(enemy, guardGain);
        }

        if (damage > 0 && intent != EnemyIntentType.DoubleAction)
        {
            damage = CombatActionResolver.ApplyEnemyIntentDamage(enemy, RuntimeState.Player, damage);
        }

        // Enemy always executes one intent per turn. Move all remaining upper sand now
        // so UI tween mirrors player-side spend timing instead of snapping at turn end.
        int spentSand = Mathf.Max(0, enemy.AvailableSand);
        if (spentSand > 0)
        {
            enemy.AvailableSand = 0;
            enemy.TransferredSand += spentSand;
        }

        UpdateHourglassFromCurrentActor();
        EventBus.Instance.Publish(new CombatActionExecutedEvent(CreateSnapshot(CombatActorType.Enemy, actionType, spentSand, damage)));
        if (damage > 0)
        {
            EventBus.Instance.Publish(new CombatActorDamagedEvent(CreateSnapshot(CombatActorType.Player, actionType, 0, damage)));
        }

        return true;
    }

    private void ConsumeEnemyRemainingSand()
    {
        if (RuntimeState == null || RuntimeState.Enemy == null || RuntimeState.TurnState != CombatTurnState.EnemyTurn)
        {
            return;
        }

        int remaining = Mathf.Max(0, RuntimeState.Enemy.AvailableSand);
        if (remaining <= 0)
        {
            return;
        }

        RuntimeState.Enemy.AvailableSand = 0;
        RuntimeState.Enemy.TransferredSand += remaining;
    }

    private void ApplyEnemyThreatOnTurnEnd(bool threatMaxAtTurnStart, bool enemyActed)
    {
        if (RuntimeState == null || RuntimeState.Enemy == null || !enemyActed)
        {
            return;
        }

        if (threatMaxAtTurnStart)
        {
            RuntimeState.Enemy.EnemyThreat = 0;
            return;
        }

        RuntimeState.Enemy.EnemyThreat = Mathf.Clamp(
            RuntimeState.Enemy.EnemyThreat + RuntimeState.EnemyThreatGainPerTurn,
            0,
            RuntimeState.ThreatCap);
    }

    private static CombatActionType MapIntentToActionType(EnemyIntentType intent)
    {
        if (intent == EnemyIntentType.RecoverGuard) return CombatActionType.RecoverGuard;
        if (intent == EnemyIntentType.WeakAttack) return CombatActionType.WeakAttack;
        if (intent == EnemyIntentType.HeavyAttack) return CombatActionType.HeavyAttack;
        if (intent == EnemyIntentType.HeavyAttackPlus) return CombatActionType.HeavyAttackPlus;
        if (intent == EnemyIntentType.DesperationStrike) return CombatActionType.DesperationStrike;
        if (intent == EnemyIntentType.DoubleAction) return CombatActionType.DoubleAction;
        return CombatActionType.None;
    }

    private void PublishTurnTransitionEvents(CombatTurnProcessor.TurnTransitionResult result)
    {
        if (result.ForcedFallAmount > 0)
        {
            EventBus.Instance.Publish(new CombatMinimumFallAppliedEvent(result.ForcedFallActor, result.ForcedFallAmount, RuntimeState != null ? RuntimeState.MinimumFall : 0));
        }

        if (result.BonusTurnGranted)
        {
            EventBus.Instance.Publish(new CombatBonusTurnGrantedEvent(result.BonusActor, CreateSnapshot()));
        }
    }

    private void ApplyBreakThreatReduction(CombatActorRuntime enemy)
    {
        if (RuntimeState == null || enemy == null)
        {
            return;
        }

        if (RuntimeState.ResetThreatOnBreak)
        {
            enemy.EnemyThreat = 0;
            return;
        }

        enemy.EnemyThreat = Mathf.Clamp(enemy.EnemyThreat + RuntimeState.BreakThreatDelta, 0, RuntimeState.ThreatCap);
    }

    private void UpdateHourglassFromCurrentActor()
    {
        if (RuntimeState == null)
        {
            return;
        }

        CombatActorRuntime current = RuntimeState.GetActor(RuntimeState.TurnState);
        CombatActorRuntime opponent = RuntimeState.GetOpponent(RuntimeState.TurnState);
        if (current == null)
        {
            return;
        }

        int unlockedSand = Mathf.Max(1, RuntimeState.TotalSand - RuntimeState.LockedSand);
        int upper = Mathf.Clamp(current.AvailableSand, 0, unlockedSand);
        int lower = Mathf.Clamp(current.TransferredSand, 0, unlockedSand);

        if (upper + lower != unlockedSand)
        {
            lower = Mathf.Clamp(unlockedSand - upper, 0, unlockedSand);
        }

        RuntimeState.UpperSand = upper;
        RuntimeState.LowerSand = lower;
        current.AvailableSand = upper;
        current.TransferredSand = lower;

        if (opponent != null)
        {
            opponent.AvailableSand = 0;
            opponent.TransferredSand = 0;
        }
    }

    private void EndCombat(bool playerWon)
    {
        if (RuntimeState == null)
        {
            return;
        }

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
        Debug.Log($"[Combat] Sand State - Total:{RuntimeState.TotalSand} Upper:{RuntimeState.UpperSand} Lower:{RuntimeState.LowerSand} Locked:{RuntimeState.LockedSand}");
        Debug.Log($"[Combat] State - TurnIndex:{RuntimeState.TurnIndex} TurnState:{RuntimeState.TurnState} PlayerGuard:{RuntimeState.Player.GuardValue} EnemyThreat:{RuntimeState.Enemy.EnemyThreat}/{RuntimeState.ThreatCap} EnemyGuard:{RuntimeState.Enemy.EnemyGuard}/{RuntimeState.Enemy.MaxEnemyGuard} EnemyGroggyPending:{RuntimeState.Enemy.GroggyPending} EnemyGroggyActive:{RuntimeState.Enemy.GroggyActive}");
        ValidateRuntimeInvariants();
    }

    private void ValidateRuntimeInvariants()
    {
        if (RuntimeState == null || RuntimeState.Player == null || RuntimeState.Enemy == null)
        {
            return;
        }

        if (RuntimeState.UpperSand < 0 || RuntimeState.LowerSand < 0 || RuntimeState.LockedSand < 0)
        {
            Debug.LogWarning("[Combat] Invariant warning: Sand bucket value below zero detected.");
        }

        int total = RuntimeState.TotalSand;
        int sum = RuntimeState.UpperSand + RuntimeState.LowerSand + RuntimeState.LockedSand;
        if (sum != total)
        {
            Debug.LogWarning($"[Combat] Invariant warning: Upper+Lower+Locked({sum}) != Total({total}).");
        }

        if (RuntimeState.Player.AvailableSand < 0 || RuntimeState.Enemy.AvailableSand < 0)
        {
            Debug.LogWarning("[Combat] Invariant warning: AvailableSand < 0 detected.");
        }

        if (RuntimeState.Enemy.EnemyThreat > RuntimeState.ThreatCap)
        {
            Debug.LogWarning("[Combat] Invariant warning: EnemyThreat > ThreatCap detected.");
        }

        if (RuntimeState.Enemy.EnemyGuard < 0)
        {
            Debug.LogWarning("[Combat] Invariant warning: EnemyGuard < 0 detected.");
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
            RuntimeState.Enemy.EnemyThreat,
            RuntimeState.Enemy.EnemyGuard,
            RuntimeState.Enemy.GroggyPending,
            RuntimeState.Enemy.GroggyActive);
    }
}
