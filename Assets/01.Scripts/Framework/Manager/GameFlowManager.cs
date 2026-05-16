using UnityEngine;

[DefaultExecutionOrder(-79)]
public class GameFlowManager : Singleton<GameFlowManager>
{
    public InGameState CurrentState { get; private set; } = InGameState.None;

    protected override void OnBootstrap()
    {
        EventBus.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Instance.Subscribe<CombatEndedEvent>(OnCombatEnded);

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
        {
            ChangeState(InGameState.Initializing);
        }
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Instance.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
    }

    public bool TryChangeState(InGameState newState)
    {
        if (!CanEnterState(newState))
        {
            return false;
        }

        ChangeState(newState);
        return true;
    }

    public void ChangeState(InGameState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        InGameState previous = CurrentState;
        CurrentState = newState;
        Debug.Log($"[GameFlowManager] InGameState Changed: {previous} -> {newState}");
        EventBus.Instance.Publish(new InGameStateChangedEvent { PreviousState = previous, NewState = newState });

        if (newState == InGameState.Initializing)
        {
            if (HourglassCombatManager.Instance == null)
            {
                Debug.LogError("[GameFlowManager] HourglassCombatManager instance is missing.", this);
                return;
            }

            HourglassCombatManager.Instance.StartCombat();
        }
    }

    private bool CanEnterState(InGameState nextState)
    {
        if (nextState == InGameState.None)
        {
            return true;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[GameFlowManager] GameManager instance is missing.", this);
            return false;
        }

        if (nextState == InGameState.Running && GameManager.Instance.CurrentState != GameState.Playing)
        {
            Debug.LogError("[GameFlowManager] Cannot enter Running when GameState is not Playing.", this);
            return false;
        }

        return true;
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        if (evt.NewState == GameState.Playing)
        {
            if (CurrentState == InGameState.None)
            {
                ChangeState(InGameState.Initializing);
            }

            return;
        }

        if (evt.NewState == GameState.Paused)
        {
            return;
        }

        if (evt.NewState == GameState.Loading)
        {
            if (CurrentState != InGameState.None)
            {
                ChangeState(InGameState.None);
            }

            return;
        }

        if (CurrentState != InGameState.None)
        {
            ChangeState(InGameState.None);
        }
    }

    private void OnCombatEnded(CombatEndedEvent evt)
    {
        if (CurrentState == InGameState.None)
        {
            return;
        }

        ChangeState(evt.PlayerWon ? InGameState.Completed : InGameState.Failed);
    }
}
