using UnityEngine;

/// <summary>
/// Shared combat configuration values for hourglass combat.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Hourglass Combat Config", fileName = "HourglassCombatConfig")]
public class HourglassCombatConfigSO : ScriptableObject
{
    public int maxTransferSand = 10;
    public int flipTransfer = 1;
    public int minimumTurnSand = 1;
    public int maxActionSand = 9;
    public int defaultPlayerSand = 5;
    public int defaultEnemySand = 5;
    public int breakThreshold = 8;
    public int prepCap = 3;
    [Range(0f, 1f)] public float groggyIncomingSandMultiplier = 0.5f;
}
