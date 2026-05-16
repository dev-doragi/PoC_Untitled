/// <summary>
/// Runtime container for current combat flow state.
/// </summary>
public class CombatRuntimeState
{
    public int TurnIndex;
    public CombatTurnState TurnState;
    public CombatActorRuntime Player;
    public CombatActorRuntime Enemy;
    public bool IsCombatEnded;
    public int TotalSand;
    public int UpperSand;
    public int LowerSand;
    public int LockedSand;
    public int MinimumFall;
    public int MaxEnemyGuard;
    public int ThreatCap;
    public int EnemyThreatGainPerTurn;
    public int HexThreatDelta;
    public int BreakThreatDelta;
    public bool ResetThreatOnBreak;
    public int EnemyRecoverGuardAmount;
    public int EnemyHighSandRecoverGuardBonus;
    public int EnemyWeakDamage;
    public int EnemyHeavyDamage;
    public int EnemyHeavyPlusDamage;
    public int EnemyDesperationDamage;
    public int EnemyDoubleActionFirstDamage;
    public int EnemyDoubleActionSecondDamage;
    public bool AllowThreatMaxDoubleAction;

    public CombatActorRuntime GetActor(CombatTurnState turnState)
    {
        if (turnState == CombatTurnState.PlayerTurn)
        {
            return Player;
        }

        if (turnState == CombatTurnState.EnemyTurn)
        {
            return Enemy;
        }

        return null;
    }

    public CombatActorRuntime GetOpponent(CombatTurnState turnState)
    {
        if (turnState == CombatTurnState.PlayerTurn)
        {
            return Enemy;
        }

        if (turnState == CombatTurnState.EnemyTurn)
        {
            return Player;
        }

        return null;
    }

    public void SyncActorSand()
    {
        CombatActorRuntime current = GetActor(TurnState);
        CombatActorRuntime opponent = GetOpponent(TurnState);
        if (current != null)
        {
            current.AvailableSand = UpperSand;
            current.TransferredSand = LowerSand;
        }

        if (opponent != null)
        {
            opponent.AvailableSand = 0;
            opponent.TransferredSand = 0;
        }
    }
}
