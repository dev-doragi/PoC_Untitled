using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Global combat feedback layer for popup text, damage numbers, and screen pulses.
/// </summary>
public class CombatFeedbackView : MonoBehaviour
{
    [SerializeField] private RectTransform _popupLayer;
    [SerializeField] private Image _screenFlashImage;
    [SerializeField] private string _popupPrefabName = "PopupToast";
    [SerializeField] private Color _defaultFlashColor = Color.white;
    [SerializeField] [Range(0f, 1f)] private float _flashPeakAlpha = 0.12f;
    [SerializeField] private float _flashFadeInDuration = 0.04f;
    [SerializeField] private float _flashFadeOutDuration = 0.14f;

    private Canvas _canvas;
    private readonly Queue<PopupRequest> _popupQueue = new();
    private bool _isPopupPlaying;
    private bool _poolMissingLogged;

    private struct PopupRequest
    {
        public RectTransform Anchor;
        public string Message;
        public Color Color;
        public float FontSize;
        public float LifeTime;
    }

    public void Initialize()
    {
        _canvas = GetComponentInParent<Canvas>();
    }

    public void PlayScreenPulse()
    {
        PlayScreenPulse(_defaultFlashColor, _flashPeakAlpha);
    }

    public void PlayScreenPulse(Color color)
    {
        PlayScreenPulse(color, _flashPeakAlpha);
    }

    public void PlayScreenPulse(Color color, float peakAlpha)
    {
        if (_screenFlashImage == null)
        {
            return;
        }

        _screenFlashImage.DOKill();
        _screenFlashImage.color = new Color(color.r, color.g, color.b, 0f);
        float safePeakAlpha = Mathf.Clamp01(peakAlpha);
        float fadeIn = Mathf.Max(0.01f, _flashFadeInDuration);
        float fadeOut = Mathf.Max(0.01f, _flashFadeOutDuration);
        _screenFlashImage.DOFade(safePeakAlpha, fadeIn).OnComplete(() => _screenFlashImage.DOFade(0f, fadeOut));
    }

    public void SpawnDamagePopup(RectTransform anchor, int value, Color c)
    {
        if (value <= 0)
        {
            return;
        }

        EnqueuePopup(new PopupRequest
        {
            Anchor = anchor,
            Message = value.ToString(),
            Color = c,
            FontSize = 42f,
            LifeTime = 0.42f
        });
    }

    public void ShowBreakText(RectTransform anchor)
    {
        EnqueuePopup(new PopupRequest
        {
            Anchor = anchor,
            Message = "BREAK",
            Color = new Color(1f, 0.9f, 0.2f, 1f),
            FontSize = 52f,
            LifeTime = 0.55f
        });
    }

    public void ShowGroggyText(RectTransform anchor)
    {
        EnqueuePopup(new PopupRequest
        {
            Anchor = anchor,
            Message = "GROGGY",
            Color = new Color(0.45f, 0.9f, 1f, 1f),
            FontSize = 44f,
            LifeTime = 0.55f
        });
    }

    private void EnqueuePopup(PopupRequest request)
    {
        _popupQueue.Enqueue(request);
        if (!_isPopupPlaying)
        {
            PlayNextPopup();
        }
    }

    private void PlayNextPopup()
    {
        if (_popupQueue.Count == 0)
        {
            _isPopupPlaying = false;
            return;
        }

        _isPopupPlaying = true;
        PopupRequest request = _popupQueue.Dequeue();
        Vector2 anchoredPosition = ResolveLocal(request.Anchor);

        if (_popupLayer == null)
        {
            _isPopupPlaying = false;
            PlayNextPopup();
            return;
        }

        if (!PoolManager.IsExisted || PoolManager.Instance == null)
        {
            if (!_poolMissingLogged)
            {
                Debug.LogWarning("[CombatFeedbackView] PoolManager instance is missing. Popup feedback will be skipped.");
                _poolMissingLogged = true;
            }

            _isPopupPlaying = false;
            PlayNextPopup();
            return;
        }

        GameObject popupObject = PoolManager.Instance.SpawnUI(_popupPrefabName, _popupLayer, anchoredPosition);
        if (popupObject == null)
        {
            _isPopupPlaying = false;
            PlayNextPopup();
            return;
        }

        PopupToast toast = popupObject.GetComponent<PopupToast>();
        if (toast == null)
        {
            PoolManager.Instance.Despawn(popupObject);
            _isPopupPlaying = false;
            PlayNextPopup();
            return;
        }

        toast.Play(
            request.Message,
            request.Color,
            request.FontSize,
            anchoredPosition,
            request.LifeTime,
            OnPopupPlayComplete);
    }

    private Vector2 ResolveLocal(RectTransform target)
    {
        if (target == null)
        {
            return Vector2.zero;
        }

        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
        }

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(_canvas != null ? _canvas.worldCamera : null, target.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_popupLayer, screen, _canvas != null ? _canvas.worldCamera : null, out Vector2 local);
        return local;
    }

    private void OnPopupPlayComplete(PopupToast toast)
    {
        if (toast != null && PoolManager.IsExisted && PoolManager.Instance != null)
        {
            PoolManager.Instance.Despawn(toast.gameObject);
        }

        _isPopupPlaying = false;
        PlayNextPopup();
    }
}
