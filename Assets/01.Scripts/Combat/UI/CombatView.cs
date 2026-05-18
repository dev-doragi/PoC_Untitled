using DG.Tweening;
using UnityEngine;

[DefaultExecutionOrder(-39)]
public class CombatView : MonoBehaviour
{
    [SerializeField] private Transform playerPivot;
    [SerializeField] private Transform enemyPivot;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private SpriteRenderer enemySpriteRenderer;
    [SerializeField] private bool enemyGroggyVisualLocked;

    private HourglassCombatManager _combatManager;
    private CombatActorDataSO _playerData;
    private CombatActorDataSO _enemyData;
    private Vector3 _playerInitialLocalPosition;
    private Vector3 _enemyInitialLocalPosition;
    private Sequence _playerSequence;
    private Sequence _enemySequence;

    private void Awake()
    {
        ValidateReferences();
        CacheInitialLocalPositions();
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<CombatStartedEvent>(OnCombatStarted);
        EventBus.Instance.Subscribe<CombatTurnStartedEvent>(OnCombatTurnStarted);
        EventBus.Instance.Subscribe<CombatActionExecutedEvent>(OnCombatActionExecuted);
        EventBus.Instance.Subscribe<CombatActorDamagedEvent>(OnCombatActorDamaged);
        EventBus.Instance.Subscribe<CombatGroggyAppliedEvent>(OnCombatGroggyApplied);
        EventBus.Instance.Subscribe<CombatEndedEvent>(OnCombatEnded);
    }

    private void Start()
    {
        CacheCombatManager();
        CacheActorData();
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
        EventBus.Instance.Unsubscribe<CombatTurnStartedEvent>(OnCombatTurnStarted);
        EventBus.Instance.Unsubscribe<CombatActionExecutedEvent>(OnCombatActionExecuted);
        EventBus.Instance.Unsubscribe<CombatActorDamagedEvent>(OnCombatActorDamaged);
        EventBus.Instance.Unsubscribe<CombatGroggyAppliedEvent>(OnCombatGroggyApplied);
        EventBus.Instance.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
        KillAllSequences();
    }

    private void OnCombatStarted(CombatStartedEvent evt)
    {
        CacheCombatManager();
        CacheActorData();
        KillAllSequences();
        enemyGroggyVisualLocked = false;

        InitializeActorVisual(CombatActorType.Player);
        InitializeActorVisual(CombatActorType.Enemy);
    }

    private void OnCombatActionExecuted(CombatActionExecutedEvent evt)
    {
        if (evt.Snapshot.action_type == CombatActionType.EndTurn)
        {
            return;
        }

        if (evt.Snapshot.actor == CombatActorType.Player)
        {
            PlayAttackSequence(CombatActorType.Player, evt.Snapshot.player_hp > 0);
        }
        else if (evt.Snapshot.actor == CombatActorType.Enemy)
        {
            PlayAttackSequence(CombatActorType.Enemy, evt.Snapshot.enemy_hp > 0);
        }
    }

    private void OnCombatTurnStarted(CombatTurnStartedEvent evt)
    {
        if (enemyGroggyVisualLocked && evt.Snapshot.turn_state == CombatTurnState.EnemyTurn)
        {
            enemyGroggyVisualLocked = false;
            SpriteRenderer enemyRenderer = GetRenderer(CombatActorType.Enemy);
            if (enemyRenderer != null && _enemyData != null && evt.Snapshot.enemy_hp > 0)
            {
                ApplySpriteIfExists(enemyRenderer, _enemyData.idleSprite);
            }
        }

        if (enemyGroggyVisualLocked)
        {
            return;
        }

        UpdateEnemyGroggyVisualFromSnapshot(evt.Snapshot);
    }

    private void OnCombatActorDamaged(CombatActorDamagedEvent evt)
    {
        if (evt.Snapshot.actor == CombatActorType.Player)
        {
            PlayHitSequence(CombatActorType.Player, evt.Snapshot.player_hp <= 0);
        }
        else if (evt.Snapshot.actor == CombatActorType.Enemy)
        {
            PlayHitSequence(CombatActorType.Enemy, evt.Snapshot.enemy_hp <= 0);
        }
    }

    private void OnCombatGroggyApplied(CombatGroggyAppliedEvent evt)
    {
        if (evt.Snapshot.actor != CombatActorType.Enemy)
        {
            return;
        }

        SpriteRenderer renderer = GetRenderer(CombatActorType.Enemy);
        if (renderer == null || _enemyData == null)
        {
            return;
        }

        enemyGroggyVisualLocked = true;
        ApplySpriteIfExists(renderer, _enemyData.groggySprite);
    }

    private void OnCombatEnded(CombatEndedEvent evt)
    {
        enemyGroggyVisualLocked = false;
        if (evt.PlayerWon)
        {
            PlayDeathSequence(CombatActorType.Enemy);
            return;
        }

        PlayDeathSequence(CombatActorType.Player);
    }

    private void UpdateEnemyGroggyVisualFromSnapshot(CombatLogSnapshot snapshot)
    {
        SpriteRenderer renderer = GetRenderer(CombatActorType.Enemy);
        if (renderer == null || _enemyData == null)
        {
            return;
        }

        bool isGroggy = snapshot.enemy_groggy_pending || snapshot.enemy_groggy_active;
        if (isGroggy)
        {
            ApplySpriteIfExists(renderer, _enemyData.groggySprite);
            return;
        }

        if (snapshot.enemy_hp > 0)
        {
            ApplySpriteIfExists(renderer, _enemyData.idleSprite);
        }
    }

    private void InitializeActorVisual(CombatActorType actorType)
    {
        Transform pivot = GetPivot(actorType);
        SpriteRenderer renderer = GetRenderer(actorType);
        CombatActorDataSO data = GetActorData(actorType);
        Vector3 initialLocalPosition = GetInitialLocalPosition(actorType);

        if (renderer != null)
        {
            renderer.gameObject.SetActive(true);
            SetRendererAlpha(renderer, 1f);
        }

        if (pivot != null)
        {
            pivot.localPosition = initialLocalPosition;
        }

        if (renderer != null && data != null)
        {
            ApplySpriteIfExists(renderer, data.idleSprite);
        }
    }

    private void PlayAttackSequence(CombatActorType actorType, bool actorAliveAfterSequence)
    {
        Transform pivot = GetPivot(actorType);
        SpriteRenderer renderer = GetRenderer(actorType);
        CombatActorDataSO data = GetActorData(actorType);
        Vector3 initialLocalPosition = GetInitialLocalPosition(actorType);
        if (pivot == null || renderer == null || data == null)
        {
            return;
        }

        KillSequence(actorType);
        ApplySpriteIfExists(renderer, data.attackSprite);

        Vector3 directionOffset = actorType == CombatActorType.Player ? data.attackMoveOffset : -data.attackMoveOffset;
        Sequence sequence = DOTween.Sequence();
        sequence.Append(pivot.DOLocalMove(initialLocalPosition + directionOffset, Mathf.Max(0f, data.attackMoveDuration)).SetEase(Ease.OutQuad));
        sequence.Append(pivot.DOLocalMove(initialLocalPosition, Mathf.Max(0f, data.attackReturnDuration)).SetEase(Ease.InQuad));
        sequence.OnComplete(() =>
        {
            pivot.localPosition = initialLocalPosition;
            if (actorAliveAfterSequence)
            {
                ApplySpriteIfExists(renderer, data.idleSprite);
            }
        });

        SetSequence(actorType, sequence);
    }

    private void PlayHitSequence(CombatActorType actorType, bool isDead)
    {
        Transform pivot = GetPivot(actorType);
        SpriteRenderer renderer = GetRenderer(actorType);
        CombatActorDataSO data = GetActorData(actorType);
        Vector3 initialLocalPosition = GetInitialLocalPosition(actorType);
        if (pivot == null || renderer == null || data == null)
        {
            return;
        }

        KillSequence(actorType);
        ApplySpriteIfExists(renderer, data.hitSprite);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(pivot.DOShakePosition(
            Mathf.Max(0f, data.hitShakeDuration),
            data.hitShakeStrength,
            Mathf.Max(0, data.hitShakeVibrato),
            90f,
            false,
            true));
        sequence.OnComplete(() =>
        {
            pivot.localPosition = initialLocalPosition;
            if (isDead)
            {
                PlayDeathSequence(actorType);
            }
            else
            {
                if (actorType == CombatActorType.Enemy && enemyGroggyVisualLocked)
                {
                    ApplySpriteIfExists(renderer, data.groggySprite);
                }
                else
                {
                    ApplySpriteIfExists(renderer, data.idleSprite);
                }
            }
        });

        SetSequence(actorType, sequence);
    }

    private void PlayDeathSequence(CombatActorType actorType)
    {
        SpriteRenderer renderer = GetRenderer(actorType);
        CombatActorDataSO data = GetActorData(actorType);
        if (renderer == null || data == null)
        {
            return;
        }

        KillSequence(actorType);
        ApplySpriteIfExists(renderer, data.deathSprite);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(DOTween.To(
            () => renderer.color.a,
            alpha => SetRendererAlpha(renderer, alpha),
            0f,
            Mathf.Max(0f, data.deathFadeDuration)).SetEase(Ease.OutQuad));
        sequence.OnComplete(() =>
        {
            if (renderer != null)
            {
                renderer.gameObject.SetActive(false);
            }
        });

        SetSequence(actorType, sequence);
    }

    private void CacheCombatManager()
    {
        if (_combatManager != null)
        {
            return;
        }

        _combatManager = HourglassCombatManager.Instance;
        if (_combatManager == null)
        {
            Debug.LogError("[CombatView] HourglassCombatManager.Instance is null.", this);
        }
    }

    private void CacheActorData()
    {
        if (_combatManager == null)
        {
            Debug.LogError("[CombatView] Cannot cache actor data because combat manager is null.", this);
            return;
        }

        _playerData = _combatManager.PlayerData;
        _enemyData = _combatManager.EnemyData;

        if (_playerData == null)
        {
            Debug.LogError("[CombatView] PlayerData is null.", this);
        }

        if (_enemyData == null)
        {
            Debug.LogError("[CombatView] EnemyData is null.", this);
        }
    }

    private void CacheInitialLocalPositions()
    {
        if (playerPivot != null)
        {
            _playerInitialLocalPosition = playerPivot.localPosition;
        }
        else
        {
            Debug.LogError("[CombatView] playerPivot is not assigned.", this);
        }

        if (enemyPivot != null)
        {
            _enemyInitialLocalPosition = enemyPivot.localPosition;
        }
        else
        {
            Debug.LogError("[CombatView] enemyPivot is not assigned.", this);
        }
    }

    private void ValidateReferences()
    {
        if (playerPivot == null)
        {
            Debug.LogError("[CombatView] playerPivot is not assigned.", this);
        }

        if (enemyPivot == null)
        {
            Debug.LogError("[CombatView] enemyPivot is not assigned.", this);
        }

        if (playerSpriteRenderer == null)
        {
            Debug.LogError("[CombatView] playerSpriteRenderer is not assigned.", this);
        }

        if (enemySpriteRenderer == null)
        {
            Debug.LogError("[CombatView] enemySpriteRenderer is not assigned.", this);
        }
    }

    private Transform GetPivot(CombatActorType actorType)
    {
        if (actorType == CombatActorType.Player)
        {
            if (playerPivot == null)
            {
                Debug.LogError("[CombatView] playerPivot is not assigned.", this);
            }

            return playerPivot;
        }

        if (actorType == CombatActorType.Enemy)
        {
            if (enemyPivot == null)
            {
                Debug.LogError("[CombatView] enemyPivot is not assigned.", this);
            }

            return enemyPivot;
        }

        Debug.LogError($"[CombatView] Unsupported actor type: {actorType}", this);
        return null;
    }

    private SpriteRenderer GetRenderer(CombatActorType actorType)
    {
        if (actorType == CombatActorType.Player)
        {
            if (playerSpriteRenderer == null)
            {
                Debug.LogError("[CombatView] playerSpriteRenderer is not assigned.", this);
            }

            return playerSpriteRenderer;
        }

        if (actorType == CombatActorType.Enemy)
        {
            if (enemySpriteRenderer == null)
            {
                Debug.LogError("[CombatView] enemySpriteRenderer is not assigned.", this);
            }

            return enemySpriteRenderer;
        }

        Debug.LogError($"[CombatView] Unsupported actor type: {actorType}", this);
        return null;
    }

    private CombatActorDataSO GetActorData(CombatActorType actorType)
    {
        if (actorType == CombatActorType.Player)
        {
            if (_playerData == null)
            {
                Debug.LogError("[CombatView] Cached player actor data is null.", this);
            }

            return _playerData;
        }

        if (actorType == CombatActorType.Enemy)
        {
            if (_enemyData == null)
            {
                Debug.LogError("[CombatView] Cached enemy actor data is null.", this);
            }

            return _enemyData;
        }

        Debug.LogError($"[CombatView] Unsupported actor type: {actorType}", this);
        return null;
    }

    private Vector3 GetInitialLocalPosition(CombatActorType actorType)
    {
        if (actorType == CombatActorType.Player)
        {
            return _playerInitialLocalPosition;
        }

        if (actorType == CombatActorType.Enemy)
        {
            return _enemyInitialLocalPosition;
        }

        Debug.LogError($"[CombatView] Unsupported actor type: {actorType}", this);
        return Vector3.zero;
    }

    private void SetSequence(CombatActorType actorType, Sequence sequence)
    {
        if (actorType == CombatActorType.Player)
        {
            _playerSequence = sequence;
            return;
        }

        if (actorType == CombatActorType.Enemy)
        {
            _enemySequence = sequence;
            return;
        }

        Debug.LogError($"[CombatView] Unsupported actor type: {actorType}", this);
    }

    private void KillSequence(CombatActorType actorType)
    {
        if (actorType == CombatActorType.Player)
        {
            if (_playerSequence != null)
            {
                _playerSequence.Kill(false);
                _playerSequence = null;
            }

            return;
        }

        if (actorType == CombatActorType.Enemy)
        {
            if (_enemySequence != null)
            {
                _enemySequence.Kill(false);
                _enemySequence = null;
            }

            return;
        }

        Debug.LogError($"[CombatView] Unsupported actor type: {actorType}", this);
    }

    private void KillAllSequences()
    {
        KillSequence(CombatActorType.Player);
        KillSequence(CombatActorType.Enemy);
    }

    private static void SetRendererAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null)
        {
            Debug.LogError("[CombatView] SpriteRenderer is null while setting alpha.");
            return;
        }

        Color color = renderer.color;
        color.a = Mathf.Clamp01(alpha);
        renderer.color = color;
    }

    private static void ApplySpriteIfExists(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer == null)
        {
            Debug.LogError("[CombatView] SpriteRenderer is null while applying sprite.");
            return;
        }

        if (sprite == null)
        {
            return;
        }

        renderer.sprite = sprite;
    }
}
