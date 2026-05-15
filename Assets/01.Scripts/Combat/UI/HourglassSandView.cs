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

    private bool _isFlipped;
    private Coroutine _flipRoutine;
    private Slider.Direction _upperBaseDirection;
    private Slider.Direction _downerBaseDirection;
    private bool _isFlipTransitionRunning;
    private bool _hasPendingFlipPreview;
    private CombatTurnState _previewTurnState;
    private CombatRuntimeState _queuedStateDuringFlip;

    private void Awake()
    {
        _upperBaseDirection = _upperSlider != null ? _upperSlider.direction : Slider.Direction.BottomToTop;
        _downerBaseDirection = _downerSlider != null ? _downerSlider.direction : Slider.Direction.BottomToTop;
        ApplySliderDirection();
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

        if (_hasPendingFlipPreview && IsCombatTurn(_previewTurnState) && IsCombatTurn(state.TurnState) && _previewTurnState != state.TurnState)
        {
            if (_flipRoutine != null)
            {
                StopCoroutine(_flipRoutine);
            }

            _flipRoutine = StartCoroutine(FlipRoutine(state));
            return;
        }

        ApplyState(state);
    }

    public void SetFlipDuration(float duration)
    {
        _flipDuration = Mathf.Clamp(duration, 0.1f, 1.5f);
    }

    public void PrepareFlipTransferPreview(in CombatLogSnapshot snapshot, int maxActionSand, int flipTransfer)
    {
        if (!IsCombatTurn(snapshot.turn_state))
        {
            return;
        }

        _hasPendingFlipPreview = true;
        _previewTurnState = snapshot.turn_state;
        _queuedStateDuringFlip = null;

        int transferred = snapshot.turn_state == CombatTurnState.PlayerTurn
            ? snapshot.player_transferred_sand
            : snapshot.enemy_transferred_sand;
        int available = snapshot.turn_state == CombatTurnState.PlayerTurn
            ? snapshot.player_available_sand
            : snapshot.enemy_available_sand;

        int safeMax = Mathf.Max(1, maxActionSand);
        int previewValue = Mathf.Max(0, transferred + Mathf.Max(0, flipTransfer));

        if (_upperSlider != null)
        {
            _upperSlider.minValue = 0f;
            _upperSlider.maxValue = safeMax;
            _upperSlider.value = Mathf.Clamp(available, 0, safeMax);
            _upperSlider.interactable = false;
        }

        if (_downerSlider != null)
        {
            _downerSlider.minValue = 0f;
            _downerSlider.maxValue = Mathf.Max(safeMax, previewValue);
            _downerSlider.value = Mathf.Clamp(transferred, 0, _downerSlider.maxValue);
            _downerSlider.interactable = false;
            _downerSlider.DOKill();
            _downerSlider.DOValue(previewValue, 0.2f).SetEase(Ease.OutQuad);
        }

        if (_downerText != null)
        {
            _downerText.text = transferred.ToString();
        }

        if (_nextSandText != null)
        {
            _nextSandText.text = $"FLIP +{Mathf.Max(0, flipTransfer)}";
        }
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

        if (_upperSlider != null)
        {
            _upperSlider.minValue = 0f;
            _upperSlider.maxValue = maxActionSand;
            _upperSlider.value = available;
            _upperSlider.interactable = false;
        }

        if (_downerSlider != null)
        {
            _downerSlider.minValue = 0f;
            _downerSlider.maxValue = maxActionSand;
            _downerSlider.value = transferred;
            _downerSlider.interactable = false;
        }

        if (_upperText != null)
        {
            _upperText.text = available.ToString();
        }

        if (_downerText != null)
        {
            _downerText.text = transferred.ToString();
        }

        SetTurnText(state.TurnState);
        SetNextSandText(state, transferred);
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

        if (_hourglassTextCanvasGroup != null)
        {
            _hourglassTextCanvasGroup.alpha = 0f;
        }

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
        if (stateToApply != null)
        {
            ApplyState(stateToApply);
        }

        if (_hourglassTextCanvasGroup != null)
        {
            _hourglassTextCanvasGroup.alpha = 1f;
        }

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
        if (_nextSandText == null)
        {
            return;
        }

        if (state.TurnState != CombatTurnState.PlayerTurn && state.TurnState != CombatTurnState.EnemyTurn)
        {
            _nextSandText.text = "NEXT : -";
            return;
        }

        int previewBase = Mathf.Max(0, currentTransferred + state.FlipTransfer);
        bool nextIsEnemy = state.TurnState == CombatTurnState.PlayerTurn;
        bool groggyReduced = nextIsEnemy && state.Enemy != null && (state.Enemy.GroggyPending || state.Enemy.GroggyActive);
        int previewReduced = groggyReduced
            ? Mathf.Max(0, Mathf.CeilToInt(previewBase * state.GroggyIncomingSandMultiplier))
            : previewBase;

        string nextActor = nextIsEnemy ? "ENEMY" : "PLAYER";
        if (groggyReduced)
        {
            _nextSandText.text = $"NEXT {nextActor} : {previewBase} -> {previewReduced}";
        }
        else
        {
            _nextSandText.text = $"NEXT {nextActor} : {previewReduced}";
        }
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
}
