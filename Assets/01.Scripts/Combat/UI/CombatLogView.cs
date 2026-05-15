using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a capped list of combat log lines in a scroll view text field.
/// </summary>
public class CombatLogView : MonoBehaviour
{
    [SerializeField] private TMP_Text _logText;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private int _maxLines = 30;

    private readonly Queue<string> _lines = new Queue<string>();

    public void AddLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _lines.Enqueue(message);
        while (_lines.Count > Mathf.Max(1, _maxLines))
        {
            _lines.Dequeue();
        }

        if (_logText != null)
        {
            _logText.text = string.Join("\n", _lines.ToArray());
        }

        Canvas.ForceUpdateCanvases();
        if (_scrollRect != null)
        {
            _scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void Clear()
    {
        _lines.Clear();
        if (_logText != null)
        {
            _logText.text = string.Empty;
        }
    }
}
