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

    public Sprite idleSprite;
    public Sprite attackSprite;
    public Sprite hitSprite;
    public Sprite deathSprite;

    public Vector3 attackMoveOffset = new Vector3(0.35f, 0f, 0f);
    public float attackMoveDuration = 0.12f;
    public float attackReturnDuration = 0.14f;
    public float hitShakeDuration = 0.18f;
    public float hitShakeStrength = 0.12f;
    public int hitShakeVibrato = 12;
    public float deathFadeDuration = 0.7f;
}
