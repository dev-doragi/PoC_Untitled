using UnityEngine;

[DefaultExecutionOrder(-80)]
public class GameManager : Singleton<GameManager>
{
    public GameState CurrentState { get; private set; } = GameState.None;

    protected override void OnBootstrap()
    {
        ChangeState(GameState.Booting);
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        GameState previous = CurrentState;
        CurrentState = newState;
        EventBus.Instance.Publish(new GameStateChangedEvent { PreviousState = previous, NewState = newState });
    }
}
