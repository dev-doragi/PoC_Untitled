using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Global combat feedback layer for popup text, damage numbers, and screen pulses.
/// </summary>
public class CombatFeedbackView : MonoBehaviour
{
    [SerializeField] private RectTransform _popupLayer;
    [SerializeField] private Image _screenFlashImage;

    private TMP_FontAsset _font;
    private Canvas _canvas;

    public void BuildRuntimeChildren(TMP_FontAsset font)
    {
        _font = font != null ? font : TMP_Settings.defaultFontAsset;

        GameObject flash = new GameObject("ScreenFlash", typeof(RectTransform), typeof(Image));
        RectTransform fr = flash.GetComponent<RectTransform>();
        fr.SetParent(transform as RectTransform, false);
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = Vector2.zero;
        fr.offsetMax = Vector2.zero;
        _screenFlashImage = flash.GetComponent<Image>();
        _screenFlashImage.color = new Color(1f, 1f, 1f, 0f);
        _screenFlashImage.raycastTarget = false;

        GameObject popup = new GameObject("PopupLayer", typeof(RectTransform));
        RectTransform pr = popup.GetComponent<RectTransform>();
        pr.SetParent(transform as RectTransform, false);
        pr.anchorMin = Vector2.zero;
        pr.anchorMax = Vector2.one;
        pr.offsetMin = Vector2.zero;
        pr.offsetMax = Vector2.zero;
        _popupLayer = pr;
    }

    public void Initialize(TMP_FontAsset font)
    {
        _font = font != null ? font : TMP_Settings.defaultFontAsset;
        _canvas = GetComponentInParent<Canvas>();
    }

    public void PlayScreenPulse(Color c)
    {
        if (_screenFlashImage == null)
        {
            return;
        }

        _screenFlashImage.DOKill();
        _screenFlashImage.color = new Color(c.r, c.g, c.b, 0f);
        _screenFlashImage.DOFade(0.18f, 0.05f).OnComplete(() => _screenFlashImage.DOFade(0f, 0.18f));
    }

    public void SpawnDamagePopup(RectTransform anchor, int value, Color c)
    {
        if (_popupLayer == null || value <= 0)
        {
            return;
        }

        SpawnPopup(anchor, value.ToString(), c, 42f, 0.42f);
    }

    public void ShowBreakText(RectTransform anchor)
    {
        SpawnPopup(anchor, "BREAK", new Color(1f, 0.9f, 0.2f, 1f), 45f, 0.5f);
    }

    public void ShowGroggyText(RectTransform anchor)
    {
        SpawnPopup(anchor, "GROGGY", new Color(0.45f, 0.9f, 1f, 1f), 42f, 0.5f);
    }

    private void SpawnPopup(RectTransform anchor, string textValue, Color color, float size, float life)
    {
        if (_popupLayer == null)
        {
            return;
        }

        Vector2 basePos = ResolveLocal(anchor);

        GameObject go = new GameObject("Popup", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(_popupLayer, false);
        rt.sizeDelta = new Vector2(280f, 90f);
        rt.anchoredPosition = basePos;

        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        cg.alpha = 0f;

        TMP_Text t = go.GetComponent<TMP_Text>();
        t.font = _font != null ? _font : TMP_Settings.defaultFontAsset;
        t.text = textValue;
        t.color = color;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;

        Sequence seq = DOTween.Sequence();
        seq.Append(cg.DOFade(1f, 0.06f));
        seq.Join(rt.DOScale(1.15f, 0.12f).SetEase(Ease.OutBack));
        seq.Join(rt.DOAnchorPosY(basePos.y + 70f, life).SetEase(Ease.OutQuad));
        seq.Append(cg.DOFade(0f, 0.18f));
        seq.OnComplete(() => Destroy(go));
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
}
