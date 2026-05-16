using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Pooled popup toast animation controller.
/// </summary>
public class PopupToast : MonoBehaviour
{
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _text;

    private Sequence _sequence;

    private void Awake()
    {
        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_text == null)
        {
            _text = GetComponent<TMP_Text>();
            if (_text == null)
            {
                _text = GetComponentInChildren<TMP_Text>();
            }
        }

        if (_rectTransform == null)
        {
            Debug.LogError("[PopupToast] RectTransform reference is missing.", this);
        }

        if (_canvasGroup == null)
        {
            Debug.LogError("[PopupToast] CanvasGroup reference is missing.", this);
        }

        if (_text == null)
        {
            Debug.LogError("[PopupToast] TMP_Text reference is missing.", this);
        }
    }

    public void Play(
        string message,
        Color color,
        float fontSize,
        Vector2 anchoredPosition,
        float lifeTime,
        System.Action<PopupToast> onComplete)
    {
        if (_sequence != null)
        {
            _sequence.Kill();
            _sequence = null;
        }

        gameObject.SetActive(true);

        if (_rectTransform != null)
        {
            _rectTransform.anchoredPosition = anchoredPosition;
            _rectTransform.localScale = Vector3.one * 0.8f;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }

        if (_text != null)
        {
            _text.text = message;
            _text.color = color;
            _text.fontSize = fontSize;
        }

        Vector2 targetPos = anchoredPosition + new Vector2(0f, 70f);
        float safeLife = Mathf.Max(0.05f, lifeTime);

        _sequence = DOTween.Sequence();
        if (_canvasGroup != null)
        {
            _sequence.Append(_canvasGroup.DOFade(1f, 0.06f));
        }

        if (_rectTransform != null)
        {
            _sequence.Join(_rectTransform.DOScale(1.15f, 0.12f).SetEase(Ease.OutBack));
            _sequence.Append(_rectTransform.DOScale(1f, 0.08f).SetEase(Ease.OutQuad));
            _sequence.Join(_rectTransform.DOAnchorPos(targetPos, safeLife).SetEase(Ease.OutQuad));
        }

        if (_canvasGroup != null)
        {
            _sequence.Append(_canvasGroup.DOFade(0f, 0.18f));
        }

        _sequence.OnComplete(() =>
        {
            _sequence = null;
            onComplete?.Invoke(this);
        });
    }

    private void OnDisable()
    {
        if (_sequence != null)
        {
            _sequence.Kill();
            _sequence = null;
        }
    }
}
