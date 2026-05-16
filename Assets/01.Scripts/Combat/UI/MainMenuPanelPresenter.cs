using UnityEngine;

/// <summary>
/// Main menu panel button handlers for single-scene flow.
/// </summary>
public class MainMenuPanelPresenter : MonoBehaviour
{
    [Header("Quit")]
    [SerializeField] private bool _quitToDesktop = true;

    public void StartGame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[MainMenuPanelPresenter] GameManager instance is missing.", this);
            return;
        }

        GameManager.Instance.ChangeState(GameState.Playing);
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
