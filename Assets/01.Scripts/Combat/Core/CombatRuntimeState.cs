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
    public int FlipTransfer;
    public int BreakThreshold;
    public int PrepCap;
    public float GroggyIncomingSandMultiplier;

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
}
