using UnityEngine;

[DefaultExecutionOrder(-74)]
public class UIManager : Singleton<UIManager>
{
    [Header("Panels")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private GameObject _gameOverPanel;

    protected override void OnBootstrap()
    {
        EventBus.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        if (GameManager.Instance != null)
        {
            OnGameStateChanged(new GameStateChangedEvent { NewState = GameManager.Instance.CurrentState });
        }
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        HideAllPanels();

        switch (evt.NewState)
        {
            case GameState.MainMenu:
                if (_mainMenuPanel != null) _mainMenuPanel.SetActive(true);
                break;
            case GameState.Paused:
                if (_pausePanel != null) _pausePanel.SetActive(true);
                break;
            case GameState.Loading:
                if (_loadingPanel != null) _loadingPanel.SetActive(true);
                break;
            case GameState.GameOver:
                if (_gameOverPanel != null) _gameOverPanel.SetActive(true);
                break;
        }
    }

    public void HideAllPanels()
    {
        if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
        if (_pausePanel != null) _pausePanel.SetActive(false);
        if (_loadingPanel != null) _loadingPanel.SetActive(false);
        if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
    }
}
