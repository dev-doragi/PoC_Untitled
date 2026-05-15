using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders the central hourglass UI with static text and independently rotating visual layers.
/// </summary>
public class HourglassSandView : MonoBehaviour
{
    [SerializeField] private RectTransform _staticTextRoot;
    [SerializeField] private RectTransform _rotatingVisualRoot;

    [SerializeField] private HourglassShapeGraphic _hourglassFrame;
    [SerializeField] private RectTransform _topChamber;
    [SerializeField] private RectTransform _bottomChamber;
    [SerializeField] private HourglassSandChamberGraphic _topSandFill;
    [SerializeField] private HourglassSandChamberGraphic _bottomSandFill;
    [SerializeField] private Image _sandStream;

    [SerializeField] private TMP_Text _turnText;
    [SerializeField] private TMP_Text _availableText;
    [SerializeField] private TMP_Text _spentText;
    [SerializeField] private TMP_Text _nextText;
    [SerializeField] private TMP_Text _warningText;

    private int _maxActionSand = 9;
    private int _flipTransfer = 1;
    private int _prepCap = 3;
    private float _groggyIncomingSandMultiplier = 0.5f;
    private float _rotationZ;

    private Color _playerColor;
    private Color _enemyColor;
    private Color _warningColor;

    private Tween _streamMoveTween;
    private Tween _streamFadeTween;

    public void BuildRuntimeChildren(TMP_FontAsset font)
    {
        RectTransform root = transform as RectTransform;

        GameObject textRootObj = CreateRect("StaticTextRoot", root, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f));
        _staticTextRoot = textRootObj.transform as RectTransform;

        _turnText = CreateText("TurnText", _staticTextRoot, new Vector2(0.1f, 0.88f), new Vector2(0.9f, 0.98f), font, 36f, FontStyles.Bold, TextAlignmentOptions.Center, "PLAYER TURN");
        _availableText = CreateText("AvailableText", _staticTextRoot, new Vector2(0.1f, 0.78f), new Vector2(0.48f, 0.87f), font, 24f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, "AVAILABLE 0");
        _spentText = CreateText("SpentText", _staticTextRoot, new Vector2(0.52f, 0.78f), new Vector2(0.9f, 0.87f), font, 24f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, "SPENT 0");
        _nextText = CreateText("NextText", _staticTextRoot, new Vector2(0.1f, 0.12f), new Vector2(0.9f, 0.22f), font, 24f, FontStyles.Bold, TextAlignmentOptions.Center, "NEXT ENEMY 0");
        _warningText = CreateText("WarningText", _staticTextRoot, new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.11f), font, 20f, FontStyles.Bold, TextAlignmentOptions.Center, "");

        GameObject visualRootObj = CreateRect("RotatingVisualRoot", root, new Vector2(0.11f, 0.18f), new Vector2(0.89f, 0.78f));
        _rotatingVisualRoot = visualRootObj.transform as RectTransform;

        GameObject frameObj = CreateRect("HourglassFrame", _rotatingVisualRoot, Vector2.zero, Vector2.one);
        if (frameObj.GetComponent<CanvasRenderer>() == null)
        {
            frameObj.AddComponent<CanvasRenderer>();
        }
        _hourglassFrame = frameObj.AddComponent<HourglassShapeGraphic>();
        _hourglassFrame.raycastTarget = false;

        GameObject topObj = CreateRect("TopChamber", _rotatingVisualRoot, Vector2.zero, Vector2.one);
        _topChamber = topObj.transform as RectTransform;
        Image topChamberBg = topObj.AddComponent<Image>();
        topChamberBg.color = new Color(1f, 1f, 1f, 0f);
        topChamberBg.raycastTarget = false;

        GameObject bottomObj = CreateRect("BottomChamber", _rotatingVisualRoot, Vector2.zero, Vector2.one);
        _bottomChamber = bottomObj.transform as RectTransform;
        Image bottomChamberBg = bottomObj.AddComponent<Image>();
        bottomChamberBg.color = new Color(1f, 1f, 1f, 0f);
        bottomChamberBg.raycastTarget = false;

        GameObject topFillObj = CreateRect("TopSandFill", _topChamber, Vector2.zero, Vector2.one);
        _topSandFill = topFillObj.AddComponent<HourglassSandChamberGraphic>();
        _topSandFill.raycastTarget = false;
        _topSandFill.SetChamberKind(HourglassSandChamberGraphic.ChamberKind.Top);

        GameObject bottomFillObj = CreateRect("BottomSandFill", _bottomChamber, Vector2.zero, Vector2.one);
        _bottomSandFill = bottomFillObj.AddComponent<HourglassSandChamberGraphic>();
        _bottomSandFill.raycastTarget = false;
        _bottomSandFill.SetChamberKind(HourglassSandChamberGraphic.ChamberKind.Bottom);

        GameObject streamObj = CreateRect("SandStream", _rotatingVisualRoot, new Vector2(0.492f, 0.43f), new Vector2(0.508f, 0.57f));
        _sandStream = streamObj.AddComponent<Image>();
        _sandStream.color = new Color(1f, 0.9f, 0.35f, 0f);
        _sandStream.raycastTarget = false;

        DisableDecorativeRaycastTargets(root);
        _rotationZ = _rotatingVisualRoot.localEulerAngles.z;
    }

    public void Initialize(TMP_FontAsset font, Color playerColor, Color enemyColor, Color warningColor)
    {
        _playerColor = playerColor;
        _enemyColor = enemyColor;
        _warningColor = warningColor;

        ApplyText(_turnText, font, 36f, FontStyles.Bold, TextAlignmentOptions.Center);
        ApplyText(_availableText, font, 24f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        ApplyText(_spentText, font, 24f, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
        ApplyText(_nextText, font, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
        ApplyText(_warningText, font, 20f, FontStyles.Bold, TextAlignmentOptions.Center);

        _rotationZ = _rotatingVisualRoot != null ? _rotatingVisualRoot.localEulerAngles.z : 0f;
    }

    public void Configure(int maxActionSand, int flipTransfer, float groggyIncomingSandMultiplier, int prepCap = 3)
    {
        _maxActionSand = Mathf.Max(1, maxActionSand);
        _flipTransfer = flipTransfer;
        _groggyIncomingSandMultiplier = Mathf.Clamp01(groggyIncomingSandMultiplier <= 0f ? 0.5f : groggyIncomingSandMultiplier);
        _prepCap = Mathf.Max(1, prepCap);
    }

    public void ApplySnapshot(in CombatLogSnapshot s)
    {
        bool isPlayerTurn = s.turn_state == CombatTurnState.PlayerTurn;
        bool isEnemyTurn = s.turn_state == CombatTurnState.EnemyTurn;

        int currentAvailable = isPlayerTurn ? s.player_available_sand : s.enemy_available_sand;
        int currentSpent = isPlayerTurn ? s.player_transferred_sand : s.enemy_transferred_sand;

        if (!isPlayerTurn && !isEnemyTurn)
        {
            currentAvailable = 0;
            currentSpent = 0;
        }

        Color actorColor = isEnemyTurn ? _enemyColor : _playerColor;
        _hourglassFrame.SetActorTone(actorColor);
        _topSandFill.SetTint(new Color(actorColor.r, actorColor.g, actorColor.b, 0.9f));
        _bottomSandFill.SetTint(new Color(actorColor.r, actorColor.g, actorColor.b, 0.86f));

        int previewBefore = currentSpent + _flipTransfer;
        previewBefore = Mathf.Max(0, previewBefore);

        bool groggyReduce = isPlayerTurn && (s.enemy_groggy_pending || s.enemy_groggy_active);
        int previewAfter = groggyReduce ? Mathf.CeilToInt(previewBefore * _groggyIncomingSandMultiplier) : previewBefore;

        int denom = Mathf.Max(1, _maxActionSand, currentAvailable, currentSpent, previewBefore, previewAfter);
        _topSandFill.SetFill(Mathf.Clamp01((float)currentAvailable / denom));
        _bottomSandFill.SetFill(Mathf.Clamp01((float)currentSpent / denom));

        _turnText.text = isEnemyTurn ? "ENEMY TURN" : (isPlayerTurn ? "PLAYER TURN" : "TURN -");
        _turnText.color = actorColor;
        _availableText.text = $"AVAILABLE {currentAvailable}";
        _spentText.text = $"SPENT {currentSpent}";

        if (isPlayerTurn)
        {
            if (groggyReduce)
            {
                _nextText.text = $"NEXT ENEMY {previewBefore} -> {previewAfter}";
            }
            else
            {
                _nextText.text = $"NEXT ENEMY {previewAfter}";
            }
        }
        else if (isEnemyTurn)
        {
            _nextText.text = $"NEXT PLAYER {previewAfter}";
        }
        else
        {
            _nextText.text = "NEXT -";
        }

        if (s.enemy_prep_stack >= _prepCap)
        {
            _warningText.text = $"PREP DANGER {s.enemy_prep_stack}/{_prepCap}";
            _warningText.color = _warningColor;
        }
        else
        {
            _warningText.text = string.Empty;
        }

        UpdateSandStream(currentAvailable >= 1 && (isPlayerTurn || isEnemyTurn), actorColor);
    }

    public void PlayFlipAnimation()
    {
        if (_rotatingVisualRoot == null)
        {
            return;
        }

        _rotationZ += 180f;
        _rotatingVisualRoot.DOKill();
        _rotatingVisualRoot.DOLocalRotate(new Vector3(0f, 0f, _rotationZ), 0.34f, RotateMode.FastBeyond360).SetEase(Ease.OutCubic);
    }

    private void UpdateSandStream(bool active, Color actorColor)
    {
        if (_sandStream == null)
        {
            return;
        }

        _streamMoveTween?.Kill();
        _streamFadeTween?.Kill();

        if (!active)
        {
            Color off = _sandStream.color;
            off.a = 0f;
            _sandStream.color = off;
            return;
        }

        Color c = new Color(actorColor.r * 0.6f + 0.4f, actorColor.g * 0.6f + 0.3f, actorColor.b * 0.3f + 0.2f, 0.7f);
        _sandStream.color = c;

        RectTransform rt = _sandStream.rectTransform;
        float baseY = rt.anchoredPosition.y;
        _streamMoveTween = rt.DOAnchorPosY(baseY - 10f, 0.28f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
        _streamFadeTween = _sandStream.DOFade(0.18f, 0.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
    }

    private static void ApplyText(TMP_Text text, TMP_FontAsset font, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        if (text == null)
        {
            return;
        }

        text.font = font != null ? font : TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static GameObject CreateRect(string name, RectTransform parent, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        return go;
    }

    private static TMP_Text CreateText(string name, RectTransform parent, Vector2 min, Vector2 max, TMP_FontAsset font, float size, FontStyles style, TextAlignmentOptions alignment, string value)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TMP_Text text = go.GetComponent<TMP_Text>();
        text.font = font != null ? font : TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.text = value;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void DisableDecorativeRaycastTargets(RectTransform root)
    {
        if (root == null)
        {
            return;
        }

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = false;
        }
    }
}
