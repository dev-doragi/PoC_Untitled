using UnityEngine;

/// <summary>
/// Shared combat configuration values for hourglass combat.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Hourglass Combat Config", fileName = "HourglassCombatConfig")]
public class HourglassCombatConfigSO : ScriptableObject
{
    public int maxActionSand = 3;
    public int flipTransfer = 1;
    public int defaultPlayerSand = 10;
    public int defaultEnemySand = 10;
    public int breakThreshold = 3;
    public int prepCap = 3;
    [Range(0f, 1f)] public float groggyIncomingSandMultiplier = 0.5f;
}
