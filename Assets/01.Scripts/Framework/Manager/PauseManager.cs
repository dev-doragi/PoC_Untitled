using UnityEngine;

[DefaultExecutionOrder(-75)]
public class PauseManager : Singleton<PauseManager>
{
    protected override void OnBootstrap()
    {
        EventBus.Instance.Subscribe<PauseInputEvent>(OnPauseInput);
        EventBus.Instance.Subscribe<PauseRequestedEvent>(OnPauseRequested);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<PauseInputEvent>(OnPauseInput);
        EventBus.Instance.Unsubscribe<PauseRequestedEvent>(OnPauseRequested);
    }

    private void OnPauseInput(PauseInputEvent evt)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[PauseManager] GameManager instance is missing.", this);
            return;
        }

        if (GameManager.Instance.CurrentState == GameState.Playing)
        {
            EventBus.Instance.Publish(new PauseRequestedEvent { Pause = true });
        }
        else if (GameManager.Instance.CurrentState == GameState.Paused)
        {
            EventBus.Instance.Publish(new PauseRequestedEvent { Pause = false });
        }
    }

    private void OnPauseRequested(PauseRequestedEvent evt)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[PauseManager] GameManager instance is missing.", this);
            return;
        }

        GameManager.Instance.ChangeState(evt.Pause ? GameState.Paused : GameState.Playing);
    }
}
