using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel view for either player or enemy state with turn emphasis and hit/attack feedback.
/// </summary>
public class CombatActorPanelView : MonoBehaviour
{
    [SerializeField] private RectTransform _panelRoot;
    [SerializeField] private RectTransform _motionRoot;
    [SerializeField] private RectTransform _popupAnchor;
    [SerializeField] private Image _background;
    [SerializeField] private Image _border;
    [SerializeField] private Image _hitFlash;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _line1Text;
    [SerializeField] private TMP_Text _line2Text;
    [SerializeField] private TMP_Text _line3Text;
    [SerializeField] private TMP_Text _warningText;
    [SerializeField] private GameObject _groggyBadge;

    private bool _isPlayer;
    private Color _baseColor;
    private Color _accentColor;
    private Vector2 _basePos;

    public RectTransform PopupAnchor => _popupAnchor != null ? _popupAnchor : _panelRoot;

    public void BuildRuntimeChildren(TMP_FontAsset font)
    {
        _panelRoot = transform as RectTransform;

        GameObject border = CreateRect("Border", _panelRoot, Vector2.zero, Vector2.one);
        _border = border.AddComponent<Image>();

        GameObject motion = CreateRect("MotionRoot", _panelRoot, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f));
        _motionRoot = motion.transform as RectTransform;

        _background = motion.AddComponent<Image>();
        _hitFlash = CreateRect("HitFlash", _motionRoot, Vector2.zero, Vector2.one).AddComponent<Image>();

        _nameText = CreateText("Name", _motionRoot, new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.97f), font, 31f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "ACTOR");
        _hpText = CreateText("HP", _motionRoot, new Vector2(0.05f, 0.67f), new Vector2(0.95f, 0.82f), font, 27f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "HP");
        _line1Text = CreateText("Line1", _motionRoot, new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.66f), font, 23f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, "");
        _line2Text = CreateText("Line2", _motionRoot, new Vector2(0.05f, 0.34f), new Vector2(0.95f, 0.49f), font, 23f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, "");
        _line3Text = CreateText("Line3", _motionRoot, new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.33f), font, 23f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, "");
        _warningText = CreateText("Warning", _motionRoot, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.17f), font, 20f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "");

        GameObject badgeRoot = CreateRect("GroggyBadge", _motionRoot, new Vector2(0.58f, 0.84f), new Vector2(0.95f, 0.97f));
        Image badgeBg = badgeRoot.AddComponent<Image>();
        badgeBg.color = new Color(0.45f, 0.9f, 1f, 0.95f);
        TMP_Text badgeText = CreateText("BadgeText", badgeRoot.transform as RectTransform, Vector2.zero, Vector2.one, font, 20f, FontStyles.Bold, TextAlignmentOptions.Center, "GROGGY");
        badgeText.color = new Color(0.03f, 0.08f, 0.15f, 1f);
        _groggyBadge = badgeRoot;
        _popupAnchor = _motionRoot;

        _hitFlash.color = new Color(1f, 1f, 1f, 0f);
        _hitFlash.raycastTarget = false;
        _groggyBadge.SetActive(false);
    }

    public void Initialize(bool isPlayer, string actorLabel, Color baseColor, Color accentColor, TMP_FontAsset font)
    {
        _isPlayer = isPlayer;
        _baseColor = baseColor;
        _accentColor = accentColor;

        if (_nameText != null)
        {
            _nameText.font = font != null ? font : TMP_Settings.defaultFontAsset;
            _nameText.text = actorLabel;
        }

        if (_background != null)
        {
            _background.color = new Color(baseColor.r * 0.22f + 0.07f, baseColor.g * 0.22f + 0.07f, baseColor.b * 0.22f + 0.07f, 0.94f);
        }

        if (_border != null)
        {
            _border.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.22f);
        }

        _basePos = _motionRoot != null ? _motionRoot.anchoredPosition : Vector2.zero;
    }

    public void ApplySnapshot(in CombatLogSnapshot s, int maxHp, bool isCurrentTurn)
    {
        if (_isPlayer)
        {
            if (_hpText != null) _hpText.text = $"HP {s.player_hp}/{Mathf.Max(1, maxHp)}";
            if (_line1Text != null) _line1Text.text = $"GUARD {s.player_guard_value}";
            if (_line2Text != null) _line2Text.text = string.Empty;
            if (_line3Text != null) _line3Text.text = string.Empty;
            if (_warningText != null) _warningText.text = string.Empty;
            if (_groggyBadge != null) _groggyBadge.SetActive(false);
        }
        else
        {
            if (_hpText != null) _hpText.text = $"HP {s.enemy_hp}/{Mathf.Max(1, maxHp)}";
            if (_line1Text != null) _line1Text.text = $"PREP {s.enemy_prep_stack}";
            if (_line2Text != null) _line2Text.text = string.Empty;
            if (_line3Text != null) _line3Text.text = string.Empty;
            if (_groggyBadge != null) _groggyBadge.SetActive(s.enemy_groggy_active);
        }

        SetTurnHighlight(isCurrentTurn);
    }

    public void ApplyEnemyExtra(int breakThreshold, int prepCap, int prepStack, int breakProgress, bool groggyPending, bool groggyActive)
    {
        if (_isPlayer)
        {
            return;
        }

        if (_line1Text != null) _line1Text.text = $"PREP {prepStack}/{Mathf.Max(1, prepCap)}";
        if (_line2Text != null) _line2Text.text = $"BREAK {breakProgress}/{Mathf.Max(1, breakThreshold)}";
        if (_line3Text != null) _line3Text.text = groggyActive ? "GROGGY" : (groggyPending ? "GROGGY NEXT" : string.Empty);

        if (_warningText != null && prepStack >= prepCap)
        {
            _warningText.text = "DESPERATION DANGER";
            _warningText.color = new Color(1f, 0.38f, 0.25f, 1f);
        }
        else if (_warningText != null)
        {
            _warningText.text = "Prep Rising";
            _warningText.color = _accentColor;
        }

        if (_groggyBadge != null)
        {
            _groggyBadge.SetActive(groggyActive);
        }

        if (_background != null)
        {
            if (groggyActive)
            {
                _background.color = new Color(_background.color.r * 0.65f, _background.color.g * 0.65f, _background.color.b * 0.65f, _background.color.a);
            }
            else
            {
                _background.color = new Color(_baseColor.r * 0.22f + 0.07f, _baseColor.g * 0.22f + 0.07f, _baseColor.b * 0.22f + 0.07f, 0.94f);
            }
        }
    }

    public void SetTurnHighlight(bool active)
    {
        if (_border != null)
        {
            _border.color = active
                ? new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0.95f)
                : new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0.22f);
        }

        if (_motionRoot != null)
        {
            _motionRoot.localScale = active ? Vector3.one * 1.01f : Vector3.one;
        }
    }

    public void PlayAttackLunge(float direction)
    {
        if (_motionRoot == null)
        {
            return;
        }

        _motionRoot.DOKill();
        _motionRoot.DOAnchorPos(_basePos + new Vector2(34f * direction, 0f), 0.08f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => _motionRoot.DOAnchorPos(_basePos, 0.15f).SetEase(Ease.OutBack));
    }

    public void PlayHitReaction()
    {
        if (_hitFlash != null)
        {
            _hitFlash.DOKill();
            _hitFlash.color = new Color(1f, 1f, 1f, 0f);
            _hitFlash.DOFade(0.5f, 0.05f).OnComplete(() => _hitFlash.DOFade(0f, 0.12f));
        }

        if (_motionRoot != null)
        {
            _motionRoot.DOKill();
            _motionRoot.DOShakeAnchorPos(0.18f, new Vector2(18f, 0f), 20, 90f, false, true)
                .OnComplete(() => _motionRoot.anchoredPosition = _basePos);
        }
    }

    private static string BoolText(bool b)
    {
        return b ? "ON" : "OFF";
    }

    private static GameObject CreateRect(string n, RectTransform p, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject(n, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(p, false);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    private static TMP_Text CreateText(string n, RectTransform p, Vector2 min, Vector2 max, TMP_FontAsset f, float size, FontStyles style, TextAlignmentOptions align, string t)
    {
        GameObject go = new GameObject(n, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(p, false);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TMP_Text text = go.GetComponent<TMP_Text>();
        text.font = f != null ? f : TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = align;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.text = t;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
