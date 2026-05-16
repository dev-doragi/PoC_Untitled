using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Renders player or enemy status widgets and lightweight actor feedback animations.
/// </summary>
public class CombatActorPanelView : MonoBehaviour
{
    [SerializeField] private RectTransform _motionRoot;
    [SerializeField] private RectTransform _popupAnchor;
    [SerializeField] private Image _border;
    [SerializeField] private Image _hitFlash;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private Slider _hpBar;
    [SerializeField] private TMP_Text _guardText;
    [SerializeField] private Slider _guardBar;
    [FormerlySerializedAs("_prepBar")]
    [SerializeField] private Slider _threatBar;
    [SerializeField] private TMP_Text _groggyText;
    [SerializeField] private TMP_Text _warningText;
    [SerializeField] private TMP_Text[] _turnTintTexts;
    [SerializeField] private Color _turnHighlightColor = new Color(1f, 0.88f, 0.2f, 1f);

    private Vector2 _baseMotionPos;
    private Color _normalBorderColor = Color.white;
    private Color _normalTextColor = Color.white;
    private bool _visualCached;

    public RectTransform PopupAnchor => _popupAnchor != null ? _popupAnchor : transform as RectTransform;

    private void Awake()
    {
        CacheVisualDefaults();
    }

    public void ApplyPlayerState(CombatActorRuntime player, bool isCurrentTurn, float guardBarMax)
    {
        CacheVisualDefaults();
        if (player == null)
        {
            return;
        }

        if (_hpText != null)
        {
            _hpText.text = $"{player.CurrentHp}/{Mathf.Max(1, player.MaxHp)}";
        }

        if (_hpBar != null)
        {
            _hpBar.minValue = 0f;
            _hpBar.maxValue = Mathf.Max(1f, player.MaxHp);
            _hpBar.value = Mathf.Clamp(player.CurrentHp, 0, player.MaxHp);
            _hpBar.interactable = false;
        }

        if (_guardText != null)
        {
            _guardText.text = $"{player.GuardValue}";
        }

        if (_guardBar != null)
        {
            _guardBar.minValue = 0f;
            _guardBar.maxValue = Mathf.Max(1f, guardBarMax);
            _guardBar.value = Mathf.Clamp(player.GuardValue, 0f, _guardBar.maxValue);
            _guardBar.interactable = false;
        }

        if (_threatBar != null)
        {
            _threatBar.minValue = 0f;
            _threatBar.maxValue = 1f;
            _threatBar.value = 0f;
            _threatBar.interactable = false;
        }

        if (_groggyText != null)
        {
            _groggyText.text = string.Empty;
        }

        if (_warningText != null)
        {
            _warningText.text = string.Empty;
        }

        ApplyTurnVisual(isCurrentTurn);
    }

    public void ApplyEnemyState(CombatActorRuntime enemy, int maxEnemyGuard, int threatCap, bool isCurrentTurn)
    {
        CacheVisualDefaults();
        if (enemy == null)
        {
            return;
        }

        int safeMaxEnemyGuard = Mathf.Max(1, maxEnemyGuard);
        int safeThreatCap = Mathf.Max(1, threatCap);
        int enemyGuardRemaining = Mathf.Clamp(enemy.EnemyGuard, 0, safeMaxEnemyGuard);

        if (_hpText != null)
        {
            _hpText.text = $"{enemy.CurrentHp}/{Mathf.Max(1, enemy.MaxHp)}";
        }

        if (_hpBar != null)
        {
            _hpBar.minValue = 0f;
            _hpBar.maxValue = Mathf.Max(1f, enemy.MaxHp);
            _hpBar.value = Mathf.Clamp(enemy.CurrentHp, 0, enemy.MaxHp);
            _hpBar.interactable = false;
        }

        if (_guardText != null)
        {
            _guardText.text = $"{enemyGuardRemaining}/{safeMaxEnemyGuard}";
        }

        if (_guardBar != null)
        {
            _guardBar.minValue = 0f;
            _guardBar.maxValue = safeMaxEnemyGuard;
            _guardBar.value = enemyGuardRemaining;
            _guardBar.interactable = false;
        }

        if (_threatBar != null)
        {
            _threatBar.minValue = 0f;
            _threatBar.maxValue = safeThreatCap;
            _threatBar.value = Mathf.Clamp(enemy.EnemyThreat, 0, safeThreatCap);
            _threatBar.interactable = false;
        }

        if (_groggyText != null)
        {
            if (enemy.GroggyActive)
            {
                _groggyText.text = "!!GROGGY!!";
            }
            else if (enemy.GroggyPending)
            {
                _groggyText.text = "GROGGY NEXT";
            }
            else
            {
                _groggyText.text = string.Empty;
            }
        }

        if (_warningText != null)
        {
            _warningText.text = enemy.EnemyThreat >= safeThreatCap ? "!!THREAT MAX!!" : string.Empty;
        }

        ApplyTurnVisual(isCurrentTurn);
    }

    public void PlayAttackLunge(float direction)
    {
        if (_motionRoot == null)
        {
            return;
        }

        _motionRoot.DOKill();
        _motionRoot.DOAnchorPos(_baseMotionPos + new Vector2(34f * direction, 0f), 0.08f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => _motionRoot.DOAnchorPos(_baseMotionPos, 0.15f).SetEase(Ease.OutBack));
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
                .OnComplete(() => _motionRoot.anchoredPosition = _baseMotionPos);
        }
    }

    private void ApplyTurnVisual(bool isCurrentTurn)
    {
        if (_border != null)
        {
            _border.color = isCurrentTurn ? _turnHighlightColor : _normalBorderColor;
        }

        Color targetTextColor = isCurrentTurn ? _turnHighlightColor : _normalTextColor;
        if (_turnTintTexts != null)
        {
            for (int i = 0; i < _turnTintTexts.Length; i++)
            {
                if (_turnTintTexts[i] != null)
                {
                    _turnTintTexts[i].color = targetTextColor;
                }
            }
        }
    }

    private void CacheVisualDefaults()
    {
        if (_visualCached)
        {
            return;
        }

        _baseMotionPos = _motionRoot != null ? _motionRoot.anchoredPosition : Vector2.zero;
        if (_border != null)
        {
            _normalBorderColor = _border.color;
        }

        TMP_Text sample = _hpText;
        if (sample == null && _turnTintTexts != null && _turnTintTexts.Length > 0)
        {
            sample = _turnTintTexts[0];
        }

        if (sample != null)
        {
            _normalTextColor = sample.color;
        }

        _visualCached = true;
    }
}
