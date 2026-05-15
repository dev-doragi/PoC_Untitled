using UnityEngine;

/// <summary>
/// Data container for combat actor base stats.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Combat Actor Data", fileName = "CombatActorData")]
public class CombatActorDataSO : ScriptableObject
{
    public CombatActorType actorType;
    public int maxHp = 100;
    public int initialSand = 10;
    public int baseGuard;
}
