using UnityEngine;
using UnityEngine.Serialization;

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
    [FormerlySerializedAs("prepGain")] public int threatDelta;
    public int guardValue;
}
