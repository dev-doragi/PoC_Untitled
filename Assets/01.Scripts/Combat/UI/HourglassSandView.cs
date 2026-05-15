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
    [SerializeField] private float _flipDuration = 0.34f;

    private bool _isFlipped;
    private float _rotationZ;
    private Coroutine _flipRoutine;
    private Slider.Direction _upperBaseDirection;
    private Slider.Direction _downerBaseDirection;

    private void Awake()
    {
        if (_rotatingVisualRoot != null)
        {
            _rotationZ = _rotatingVisualRoot.localEulerAngles.z;
        }

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

    public void SetResultText(bool playerWon)
    {
        if (_turnText == null)
        {
            return;
        }

        _turnText.text = playerWon ? "YOU WIN!" : "YOU LOSE!";
    }

    public void PlayFlipAnimation()
    {
        if (_flipRoutine != null)
        {
            StopCoroutine(_flipRoutine);
        }

        _flipRoutine = StartCoroutine(FlipRoutine());
    }

    private IEnumerator FlipRoutine()
    {
        if (_hourglassTextCanvasGroup != null)
        {
            _hourglassTextCanvasGroup.alpha = 0f;
        }

        if (_rotatingVisualRoot != null)
        {
            _rotationZ += 180f;
            Tween tween = _rotatingVisualRoot.DOLocalRotate(new Vector3(0f, 0f, _rotationZ), Mathf.Clamp(_flipDuration, 0.1f, 1f), RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic);
            yield return tween.WaitForCompletion();
        }

        _isFlipped = !_isFlipped;
        ApplySliderDirection();

        if (_hourglassTextCanvasGroup != null)
        {
            _hourglassTextCanvasGroup.alpha = 1f;
        }

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
}
