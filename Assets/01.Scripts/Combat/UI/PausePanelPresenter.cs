using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pause panel button handlers.
/// Attach this to the pause panel root and bind button OnClick to these public methods.
/// </summary>
public class PausePanelPresenter : MonoBehaviour
{
    [Header("Quit")]
    [SerializeField] private bool _quitToDesktop = true;

    public void ContinueGame()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
        {
            EventBus.Instance.Publish(new PauseRequestedEvent { Pause = false });
        }
    }

    public void RetryGame()
    {
        if (SceneLoader.Instance == null)
        {
            Debug.LogError("[PausePanelPresenter] SceneLoader instance is missing.", this);
            return;
        }

        if (SceneLoader.Instance.IsLoading)
        {
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        SceneLoader.Instance.RequestLoad(sceneName, GameState.MainMenu);
    }

    public void ExitGame()
    {
        if (_quitToDesktop)
        {
            Application.Quit();
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.MainMenu);
        }
    }
}
