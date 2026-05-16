public readonly struct CombatLogSnapshot
{
    public readonly int turn_index;
    public readonly CombatTurnState turn_state;
    public readonly CombatActorType actor;
    public readonly CombatActionType action_type;
    public readonly int spent_sand;
    public readonly int damage;
    public readonly int player_hp;
    public readonly int enemy_hp;
    public readonly int player_available_sand;
    public readonly int enemy_available_sand;
    public readonly int player_transferred_sand;
    public readonly int enemy_transferred_sand;
    public readonly int player_guard_value;
    public readonly int enemy_prep_stack;
    public readonly int enemy_guard_value;
    public readonly bool enemy_groggy_pending;
    public readonly bool enemy_groggy_active;

    public CombatLogSnapshot(
        int turnIndex,
        CombatTurnState turnState,
        CombatActorType actorType,
        CombatActionType actionType,
        int spentSand,
        int damageValue,
        int playerHp,
        int enemyHp,
        int playerAvailableSand,
        int enemyAvailableSand,
        int playerTransferredSand,
        int enemyTransferredSand,
        int playerGuardValue,
        int enemyPrepStack,
        int enemyGuardValue,
        bool enemyGroggyPending,
        bool enemyGroggyActive)
    {
        turn_index = turnIndex;
        turn_state = turnState;
        actor = actorType;
        action_type = actionType;
        spent_sand = spentSand;
        damage = damageValue;
        player_hp = playerHp;
        enemy_hp = enemyHp;
        player_available_sand = playerAvailableSand;
        enemy_available_sand = enemyAvailableSand;
        player_transferred_sand = playerTransferredSand;
        enemy_transferred_sand = enemyTransferredSand;
        player_guard_value = playerGuardValue;
        enemy_prep_stack = enemyPrepStack;
        enemy_guard_value = enemyGuardValue;
        enemy_groggy_pending = enemyGroggyPending;
        enemy_groggy_active = enemyGroggyActive;
    }
}

public readonly struct CombatStartedEvent
{
    public readonly CombatLogSnapshot Snapshot;

    public CombatStartedEvent(CombatLogSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}

public readonly struct CombatTurnStartedEvent
{
    public readonly CombatLogSnapshot Snapshot;

    public CombatTurnStartedEvent(CombatLogSnapshot snapshot)
    {
        Snapshot = snapshot;
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
    public readonly CombatLogSnapshot Snapshot;

    public CombatActionExecutedEvent(CombatLogSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}

public readonly struct CombatTurnEndedEvent
{
    public readonly CombatLogSnapshot Snapshot;

    public CombatTurnEndedEvent(CombatLogSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}

public readonly struct CombatActorDamagedEvent
{
    public readonly CombatLogSnapshot Snapshot;

    public CombatActorDamagedEvent(CombatLogSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}

public readonly struct CombatBreakTriggeredEvent
{
    public readonly CombatLogSnapshot Snapshot;

    public CombatBreakTriggeredEvent(CombatLogSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}

public readonly struct CombatGroggyAppliedEvent
{
    public readonly CombatLogSnapshot Snapshot;

    public CombatGroggyAppliedEvent(CombatLogSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}

public readonly struct CombatEndedEvent
{
    public readonly bool PlayerWon;
    public readonly CombatLogSnapshot Snapshot;

    public CombatEndedEvent(bool playerWon, CombatLogSnapshot snapshot)
    {
        PlayerWon = playerWon;
        Snapshot = snapshot;
    }
}
