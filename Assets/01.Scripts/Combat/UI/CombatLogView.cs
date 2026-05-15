using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Combat log renderer for readable in-screen flow without relying on console logs.
/// </summary>
public class CombatLogView : MonoBehaviour
{
    [SerializeField] private TMP_Text _logText;
    [SerializeField] private int _maxLines = 26;

    private readonly Queue<string> _lines = new Queue<string>();

    public void BuildRuntimeChildren(TMP_FontAsset font)
    {
        GameObject t = new GameObject("LogText", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rt = t.GetComponent<RectTransform>();
        rt.SetParent(transform as RectTransform, false);
        rt.anchorMin = new Vector2(0.04f, 0.06f);
        rt.anchorMax = new Vector2(0.96f, 0.94f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _logText = t.GetComponent<TMP_Text>();
        _logText.font = font != null ? font : TMP_Settings.defaultFontAsset;
        _logText.fontSize = 19f;
        _logText.alignment = TextAlignmentOptions.TopLeft;
        _logText.textWrappingMode = TextWrappingModes.Normal;
        _logText.overflowMode = TextOverflowModes.Overflow;
        _logText.color = new Color(0.92f, 0.95f, 0.98f, 1f);
        _logText.raycastTarget = false;
    }

    public void Initialize(TMP_FontAsset font)
    {
        if (_logText == null)
        {
            return;
        }

        _logText.font = font != null ? font : TMP_Settings.defaultFontAsset;
        _logText.text = string.Empty;
    }

    public void PushSystem(string msg, Color color)
    {
        PushLine(ColorTag(msg, color));
    }

    public void PushTurn(in CombatLogSnapshot s, Color gold)
    {
        PushLine(ColorTag($"[TURN] {s.turn_state} (T{s.turn_index})", gold));
    }

    public void PushAction(in CombatLogSnapshot s, Color playerColor, Color enemyColor, Color gold)
    {
        Color c = s.actor == CombatActorType.Player ? playerColor : enemyColor;
        if (s.action_type == CombatActionType.EndTurn)
        {
            c = gold;
        }

        string txt = $"[{s.actor}] {s.action_type}  dmg:{s.damage}  spent:{s.spent_sand}  P:{s.player_hp} E:{s.enemy_hp}";
        PushLine(ColorTag(txt, c));
    }

    public void PushHighlight(string msg, Color color)
    {
        PushLine(ColorTag(msg, color));
    }

    private void PushLine(string line)
    {
        _lines.Enqueue(line);
        while (_lines.Count > _maxLines)
        {
            _lines.Dequeue();
        }

        if (_logText != null)
        {
            _logText.text = string.Join("\n", _lines.ToArray());
        }
    }

    private static string ColorTag(string text, Color c)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{text}</color>";
    }
}
