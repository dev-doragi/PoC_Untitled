using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Shared combat configuration values for hourglass combat.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Hourglass Combat Config", fileName = "HourglassCombatConfig")]
public class HourglassCombatConfigSO : ScriptableObject
{
    [Header("Hourglass")]
    [FormerlySerializedAs("maxTransferSand")] public int totalSand = 10;
    [FormerlySerializedAs("minimumTurnSand")] public int minimumFall = 3;
    [Range(0, 10)] public int lockedSand = 0;

    [Header("Initial Upper Sand")]
    public int defaultPlayerSand = 5;
    public int defaultEnemySand = 5;

    [Header("Break")]
    public int breakThreshold = 8;

    [Header("Threat")]
    [FormerlySerializedAs("prepCap")] public int threatCap = 3;
    public int enemyThreatGainPerTurn = 1;
    public int hexThreatDelta = -1;
    public int breakThreatDelta = -2;
    public bool resetThreatOnBreak = false;

    [Header("Enemy Intent Effects")]
    public int enemyRecoverGuardAmount = 2;
    public int enemyHighSandRecoverGuardBonus = 1;
    public int enemyWeakDamage = 4;
    public int enemyHeavyDamage = 8;
    public int enemyHeavyPlusDamage = 11;
    public int enemyDesperationDamage = 6;
    public bool allowThreatMaxDoubleAction = true;
    public int enemyDoubleActionFirstDamage = 4;
    public int enemyDoubleActionSecondDamage = 8;
}
