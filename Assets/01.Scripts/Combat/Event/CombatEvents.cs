public readonly struct CombatStartedEvent
{
    public readonly CombatActorRuntime Player;
    public readonly CombatActorRuntime Enemy;

    public CombatStartedEvent(CombatActorRuntime player, CombatActorRuntime enemy)
    {
        Player = player;
        Enemy = enemy;
    }
}

public readonly struct CombatTurnStartedEvent
{
    public readonly CombatTurnState TurnState;

    public CombatTurnStartedEvent(CombatTurnState turnState)
    {
        TurnState = turnState;
    }
}

public readonly struct CombatActionRequestedEvent
{
    public readonly CombatActionType ActionType;

    public CombatActionRequestedEvent(CombatActionType actionType)
    {
        ActionType = actionType;
    }
}

public readonly struct CombatActionExecutedEvent
{
    public readonly CombatActorType Source;
    public readonly CombatActionType ActionType;
    public readonly int DamageDealt;
    public readonly int SpentSand;

    public CombatActionExecutedEvent(CombatActorType source, CombatActionType actionType, int damageDealt, int spentSand)
    {
        Source = source;
        ActionType = actionType;
        DamageDealt = damageDealt;
        SpentSand = spentSand;
    }
}

public readonly struct CombatTurnEndedEvent
{
    public readonly CombatTurnState EndedTurn;

    public CombatTurnEndedEvent(CombatTurnState endedTurn)
    {
        EndedTurn = endedTurn;
    }
}

public readonly struct CombatActorDamagedEvent
{
    public readonly CombatActorType Target;
    public readonly int Damage;

    public CombatActorDamagedEvent(CombatActorType target, int damage)
    {
        Target = target;
        Damage = damage;
    }
}

public readonly struct CombatBreakTriggeredEvent
{
    public readonly CombatActorType Target;

    public CombatBreakTriggeredEvent(CombatActorType target)
    {
        Target = target;
    }
}

public readonly struct CombatGroggyAppliedEvent
{
    public readonly CombatActorType Target;

    public CombatGroggyAppliedEvent(CombatActorType target)
    {
        Target = target;
    }
}

public readonly struct CombatEndedEvent
{
    public readonly bool PlayerWon;

    public CombatEndedEvent(bool playerWon)
    {
        PlayerWon = playerWon;
    }
}
