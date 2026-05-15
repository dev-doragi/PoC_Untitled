using UnityEngine;

/// <summary>
/// Data container for combat actions.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Combat Action Data", fileName = "CombatActionData")]
public class CombatActionDataSO : ScriptableObject
{
    public CombatActionType actionType;
    public int baseDamage;
    public int sandCost;
    public int breakPower;
    public int prepGain;
    public int guardValue;
}
