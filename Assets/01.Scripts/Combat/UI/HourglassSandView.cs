using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds pre-placed hourglass sliders and texts to runtime combat sand state and turn flips.
/// </summary>
public class HourglassSandView : MonoBehaviour
{
    [SerializeField] private RectTransform _rotatingVisualRoot;
    [SerializeField] private CanvasGroup _hourglassTextCanvasGroup;
    [SerializeField] private Slider _upperSlider;
    [SerializeField] private Slider _downerSlider;
    [SerializeField] private TMP_Text _upperText;
    [SerializeField] private TMP_Text _downerText;
    [SerializeField] private TMP_Text _turnText;
    [SerializeField] private TMP_Text _nextSandText;
    [SerializeField] private float _flipDuration = 0.45f;
    [SerializeField] private float _flipAnglePerTurn = -180f;
    [SerializeField] private float _textFadeDuration = 0.08f;
    [Header("Sand Tween")]
    [SerializeField] private float _sandTweenDuration = 0.16f;
    [SerializeField] private Ease _sandTweenEase = Ease.Linear;

    private bool _isFlipped;
    private Coroutine _flipRoutine;
    private Slider.Direction _upperBaseDirection;
    private Slider.Direction _downerBaseDirection;
    private bool _isFlipTransitionRunning;
    private bool _hasPendingFlipPreview;
    private bool _forceFlipPending;
    private CombatTurnState _previewTurnState;
    private CombatRuntimeState _queuedStateDuringFlip;
    private bool _freezeSandUntilStateProgress;
    private CombatTurnState _freezeTurnState;
    private int _freezeAvailable;
    private int _freezeTransferred;
    public bool IsTransitioning => _isFlipTransitionRunning;

    private void Awake()
    {
        _upperBaseDirection = _upperSlider != null ? _upperSlider.direction : Slider.Direction.BottomToTop;
        _downerBaseDirection = _downerSlider != null ? _downerSlider.direction : Slider.Direction.BottomToTop;
        ApplySliderDirection();
        if (_hourglassTextCanvasGroup != null)
        {
            _hourglassTextCanvasGroup.alpha = 1f;
        }
    }

    private void OnDestroy()
    {
        _upperSlider?.DOKill();
        _downerSlider?.DOKill();
        _rotatingVisualRoot?.DOKill();
    }

    public void Refresh(CombatRuntimeState state)
    {
        if (state == null)
        {
            return;
        }

        if (_isFlipTransitionRunning)
        {
            _queuedStateDuringFlip = state;
            return;
        }

        bool shouldFlipByTurnSwap = IsCombatTurn(_previewTurnState) && IsCombatTurn(state.TurnState) && _previewTurnState != state.TurnState;
        bool shouldFlipByForce = _forceFlipPending && IsCombatTurn(state.TurnState);
        if (_hasPendingFlipPreview && (shouldFlipByTurnSwap || shouldFlipByForce))
        {
            if (_flipRoutine != null)
            {
                StopCoroutine(_flipRoutine);
            }

            _flipRoutine = StartCoroutine(FlipRoutine(state));
            return;
        }

        if (_freezeSandUntilStateProgress)
        {
            if (state.TurnState != _freezeTurnState)
            {
                _freezeSandUntilStateProgress = false;
            }
            else
            {
                CombatActorRuntime frozenActor = state.GetActor(state.TurnState);
                if (frozenActor != null && frozenActor.AvailableSand == _freezeAvailable && frozenActor.TransferredSand == _freezeTransferred)
                {
                    ApplyTextState(state, frozenActor.TransferredSand);
                    return;
                }

                _freezeSandUntilStateProgress = false;
            }
        }

        ApplyState(state);
    }

    public void SetFlipDuration(float duration)
    {
        _flipDuration = Mathf.Clamp(duration, 0.1f, 1.5f);
    }

    public void QueueFlipPreview(in CombatLogSnapshot snapshot, CombatRuntimeState state)
    {
        if (!IsCombatTurn(snapshot.turn_state) || state == null)
        {
            return;
        }

        _hasPendingFlipPreview = true;
        _forceFlipPending = true;
        _previewTurnState = snapshot.turn_state;
        _queuedStateDuringFlip = null;

        int transferred = snapshot.turn_state == CombatTurnState.PlayerTurn
            ? snapshot.player_transferred_sand
            : snapshot.enemy_transferred_sand;
        int available = snapshot.turn_state == CombatTurnState.PlayerTurn
            ? snapshot.player_available_sand
            : snapshot.enemy_available_sand;
        bool nextIsEnemy = snapshot.turn_state == CombatTurnState.PlayerTurn;
        if (nextIsEnemy && snapshot.enemy_groggy_pending)
        {
            nextIsEnemy = false;
        }

        int safeMax = Mathf.Max(1, state.TotalSand - state.LockedSand);
        int previewValue = ComputeNextUpperAfterMinimumFall(available, transferred, state.MinimumFall);

        ApplySandState(available, transferred, safeMax, true);

        if (_upperText != null)
        {
            _upperText.text = available.ToString();
        }

        if (_downerText != null)
        {
            _downerText.text = transferred.ToString();
        }

        SetNextText(nextIsEnemy, previewValue, previewValue, false);
    }

    public void SetResultText(bool playerWon)
    {
        if (_turnText == null)
        {
            return;
        }

        _turnText.text = playerWon ? "YOU WIN!" : "YOU LOSE!";
    }

    private void ApplyState(CombatRuntimeState state)
    {
        if (state == null)
        {
            return;
        }

        if (state.IsCombatEnded)
        {
            if (state.Player != null && state.Player.CurrentHp <= 0)
            {
                SetResultText(false);
            }
            else
            {
                SetResultText(true);
            }
        }

        CombatActorRuntime currentActor = state.GetActor(state.TurnState);
        if (currentActor == null)
        {
            if (!state.IsCombatEnded)
            {
                SetTurnText(state.TurnState);
            }
            return;
        }

        int maxActionSand = Mathf.Max(1, currentActor.MaxActionSand);
        int available = Mathf.Clamp(currentActor.AvailableSand, 0, maxActionSand);
        int transferred = Mathf.Clamp(currentActor.TransferredSand, 0, maxActionSand);

        ApplySandState(available, transferred, maxActionSand, false);
        ApplyTextState(state, transferred);
    }

    public void PlayFlipAnimation()
    {
        if (_flipRoutine != null)
        {
            StopCoroutine(_flipRoutine);
        }

        _flipRoutine = StartCoroutine(FlipRoutine(null));
    }

    private IEnumerator FlipRoutine(CombatRuntimeState nextState)
    {
        _isFlipTransitionRunning = true;

        yield return FadeStaticTexts(0f);

        if (_rotatingVisualRoot != null)
        {
            _rotatingVisualRoot.DOKill();
            Tween tween = _rotatingVisualRoot.DOLocalRotate(new Vector3(0f, 0f, _flipAnglePerTurn), Mathf.Clamp(_flipDuration, 0.1f, 1f), RotateMode.LocalAxisAdd)
                .SetEase(Ease.OutCubic);
            yield return tween.WaitForCompletion();
        }

        _isFlipped = !_isFlipped;
        ApplySliderDirection();

        CombatRuntimeState stateToApply = _queuedStateDuringFlip != null ? _queuedStateDuringFlip : nextState;
        _queuedStateDuringFlip = null;
        _hasPendingFlipPreview = false;
        _forceFlipPending = false;
        if (stateToApply != null)
        {
            CombatActorRuntime actor = stateToApply.GetActor(stateToApply.TurnState);
            if (actor != null)
            {
                _freezeSandUntilStateProgress = true;
                _freezeTurnState = stateToApply.TurnState;
                _freezeAvailable = actor.AvailableSand;
                _freezeTransferred = actor.TransferredSand;
                ApplyTextState(stateToApply, Mathf.Clamp(actor.TransferredSand, 0, Mathf.Max(1, actor.MaxActionSand)));
            }
            else
            {
                _freezeSandUntilStateProgress = false;
                ApplyTextState(stateToApply, 0);
            }
        }

        yield return FadeStaticTexts(1f);

        _isFlipTransitionRunning = false;
        _flipRoutine = null;
    }

    private void SetTurnText(CombatTurnState turnState)
    {
        if (_turnText == null)
        {
            return;
        }

        if (turnState == CombatTurnState.PlayerTurn)
        {
            _turnText.text = "PLAYER TURN";
        }
        else if (turnState == CombatTurnState.EnemyTurn)
        {
            _turnText.text = "ENEMY TURN";
        }
        else
        {
            _turnText.text = "-";
        }
    }

    private void SetNextSandText(CombatRuntimeState state, int currentTransferred)
    {
        if (state.TurnState != CombatTurnState.PlayerTurn && state.TurnState != CombatTurnState.EnemyTurn)
        {
            if (_nextSandText != null)
            {
                _nextSandText.text = "NEXT -";
            }
            return;
        }

        CombatActorRuntime actor = state.GetActor(state.TurnState);
        int currentAvailable = actor != null ? actor.AvailableSand : 0;
        int previewBase = ComputeNextUpperAfterMinimumFall(currentAvailable, currentTransferred, state.MinimumFall);
        bool nextIsEnemy = state.TurnState == CombatTurnState.PlayerTurn;
        bool bonusTurn = nextIsEnemy && state.Enemy != null && (state.Enemy.GroggyPending || state.Enemy.GroggyActive);
        if (bonusTurn)
        {
            nextIsEnemy = false;
        }

        SetNextText(nextIsEnemy, previewBase, previewBase, false);
    }

    private void SetNextText(bool nextIsEnemy, int previewBase, int previewReduced, bool groggyReduced)
    {
        if (_nextSandText == null)
        {
            return;
        }

        if (groggyReduced)
        {
            _nextSandText.text = $"GROGGY {previewBase} -> {previewReduced}";
            return;
        }

        string nextActor = nextIsEnemy ? "ENEMY" : "PLAYER";
        _nextSandText.text = $"NEXT {nextActor} {previewReduced}";
    }

    private void ApplySliderDirection()
    {
        if (_upperSlider != null)
        {
            _upperSlider.direction = _isFlipped ? ToggleDirection(_upperBaseDirection) : _upperBaseDirection;
        }

        if (_downerSlider != null)
        {
            _downerSlider.direction = _isFlipped ? ToggleDirection(_downerBaseDirection) : _downerBaseDirection;
        }
    }

    private static Slider.Direction ToggleDirection(Slider.Direction source)
    {
        switch (source)
        {
            case Slider.Direction.BottomToTop:
                return Slider.Direction.TopToBottom;
            case Slider.Direction.TopToBottom:
                return Slider.Direction.BottomToTop;
            case Slider.Direction.LeftToRight:
                return Slider.Direction.RightToLeft;
            case Slider.Direction.RightToLeft:
                return Slider.Direction.LeftToRight;
            default:
                return source;
        }
    }

    private static bool IsCombatTurn(CombatTurnState turnState)
    {
        return turnState == CombatTurnState.PlayerTurn || turnState == CombatTurnState.EnemyTurn;
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

    private void ApplySandState(int available, int transferred, int maxActionSand, bool immediate)
    {
        Slider topVisibleSlider = GetTopVisibleSlider();
        Slider bottomVisibleSlider = GetBottomVisibleSlider();

        if (topVisibleSlider != null)
        {
            topVisibleSlider.minValue = 0f;
            topVisibleSlider.maxValue = maxActionSand;
            topVisibleSlider.interactable = false;
            TweenSliderValue(topVisibleSlider, available, immediate);
        }

        if (bottomVisibleSlider != null)
        {
            bottomVisibleSlider.minValue = 0f;
            bottomVisibleSlider.maxValue = maxActionSand;
            bottomVisibleSlider.interactable = false;
            TweenSliderValue(bottomVisibleSlider, transferred, immediate);
        }
    }

    private void TweenSliderValue(Slider slider, int targetValue, bool immediate)
    {
        if (slider == null)
        {
            return;
        }

        float clampedTarget = Mathf.Clamp(targetValue, slider.minValue, slider.maxValue);
        slider.DOKill();

        float duration = immediate ? 0f : Mathf.Max(0f, _sandTweenDuration);
        if (duration <= 0f || Mathf.Approximately(slider.value, clampedTarget))
        {
            slider.value = clampedTarget;
            return;
        }

        slider.DOValue(clampedTarget, duration).SetEase(_sandTweenEase);
    }

    private Slider GetTopVisibleSlider()
    {
        return _isFlipped ? _downerSlider : _upperSlider;
    }

    private Slider GetBottomVisibleSlider()
    {
        return _isFlipped ? _upperSlider : _downerSlider;
    }

    private void ApplyTextState(CombatRuntimeState state, int transferred)
    {
        CombatActorRuntime actor = state != null ? state.GetActor(state.TurnState) : null;
        if (_upperText != null)
        {
            _upperText.text = actor != null ? Mathf.Clamp(actor.AvailableSand, 0, Mathf.Max(1, actor.MaxActionSand)).ToString() : "0";
        }

        if (_downerText != null)
        {
            _downerText.text = Mathf.Max(0, transferred).ToString();
        }

        if (state != null)
        {
            SetTurnText(state.TurnState);
            SetNextSandText(state, transferred);
        }
    }

    private IEnumerator FadeStaticTexts(float targetAlpha)
    {
        float duration = Mathf.Clamp(_textFadeDuration, 0.01f, 0.2f);

        if (_hourglassTextCanvasGroup != null)
        {
            _hourglassTextCanvasGroup.DOKill();
            _hourglassTextCanvasGroup.DOFade(targetAlpha, duration).SetEase(Ease.OutQuad);
        }

        FadeLabel(_turnText, targetAlpha, duration);
        FadeLabel(_nextSandText, targetAlpha, duration);

        yield return new WaitForSeconds(duration);
    }

    private static void FadeLabel(TMP_Text text, float targetAlpha, float duration)
    {
        if (text == null)
        {
            return;
        }

        text.DOKill();
        text.DOFade(targetAlpha, duration).SetEase(Ease.OutQuad);
    }
}
