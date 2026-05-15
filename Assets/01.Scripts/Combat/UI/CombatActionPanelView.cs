using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Holds inspector references for action card buttons and their text widgets.
/// </summary>
public class CombatActionPanelView : MonoBehaviour
{
    [Header("Strike")]
    [SerializeField] private Button _strikeButton;
    [SerializeField] private TMP_Text _strikeNameText;
    [SerializeField] private TMP_Text _strikeCostText;
    [SerializeField] private TMP_Text _strikeEffectText;

    [Header("Pierce")]
    [SerializeField] private Button _pierceButton;
    [SerializeField] private TMP_Text _pierceNameText;
    [SerializeField] private TMP_Text _pierceCostText;
    [SerializeField] private TMP_Text _pierceEffectText;

    [Header("Hex")]
    [SerializeField] private Button _hexButton;
    [SerializeField] private TMP_Text _hexNameText;
    [SerializeField] private TMP_Text _hexCostText;
    [SerializeField] private TMP_Text _hexEffectText;

    [Header("Guard")]
    [SerializeField] private Button _guardButton;
    [SerializeField] private TMP_Text _guardNameText;
    [SerializeField] private TMP_Text _guardCostText;
    [SerializeField] private TMP_Text _guardEffectText;

    [Header("End Turn")]
    [SerializeField] private Button _endTurnButton;
    [SerializeField] private TMP_Text _endTurnNameText;
    [SerializeField] private TMP_Text _endTurnCostText;
    [SerializeField] private TMP_Text _endTurnEffectText;

    public Button StrikeButton => _strikeButton;
    public Button PierceButton => _pierceButton;
    public Button HexButton => _hexButton;
    public Button GuardButton => _guardButton;
    public Button EndTurnButton => _endTurnButton;

    public void SetStaticTexts()
    {
        SetTexts(_strikeNameText, _strikeCostText, _strikeEffectText, "STRIKE", "Cost 3", "DMG 6");
        SetTexts(_pierceNameText, _pierceCostText, _pierceEffectText, "PIERCE", "Cost 3", "GUARD -4");
        SetTexts(_hexNameText, _hexCostText, _hexEffectText, "HEX", "Cost 2", "PREP -1");
        SetTexts(_guardNameText, _guardCostText, _guardEffectText, "GUARD", "Cost 2", "BLOCK +4");
        SetTexts(_endTurnNameText, _endTurnCostText, _endTurnEffectText, "FLIP", "End Turn", "NEXT +0");
    }

    public void SetEndTurnPreview(int nextSand)
    {
        if (_endTurnEffectText != null)
        {
            _endTurnEffectText.text = $"NEXT +{Mathf.Max(0, nextSand)}";
        }
    }

    public void SetInteractable(bool strike, bool pierce, bool hex, bool guard, bool endTurn)
    {
        SetButtonState(_strikeButton, strike);
        SetButtonState(_pierceButton, pierce);
        SetButtonState(_hexButton, hex);
        SetButtonState(_guardButton, guard);
        SetButtonState(_endTurnButton, endTurn);
    }

    private static void SetTexts(TMP_Text name, TMP_Text cost, TMP_Text effect, string nameValue, string costValue, string effectValue)
    {
        if (name != null) name.text = nameValue;
        if (cost != null) cost.text = costValue;
        if (effect != null) effect.text = effectValue;
    }

    private static void SetButtonState(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }
}
