using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[DefaultExecutionOrder(-77)]
public class SceneLoader : Singleton<SceneLoader>
{
    private bool _isLoading;
    private bool _hasPendingPostLoadState;
    private GameState _pendingPostLoadState = GameState.None;

    protected override void OnBootstrap()
    {
        EventBus.Instance.Subscribe<SceneLoadRequestedEvent>(OnSceneLoadRequested);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<SceneLoadRequestedEvent>(OnSceneLoadRequested);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public bool IsLoading => _isLoading;

    public void RequestLoad(string sceneName)
    {
        EventBus.Instance.Publish(new SceneLoadRequestedEvent
        {
            SceneName = sceneName,
            HasPostLoadState = false,
            PostLoadState = GameState.None
        });
    }

    public void RequestLoad(string sceneName, GameState postLoadState)
    {
        EventBus.Instance.Publish(new SceneLoadRequestedEvent
        {
            SceneName = sceneName,
            HasPostLoadState = true,
            PostLoadState = postLoadState
        });
    }

    private void OnSceneLoadRequested(SceneLoadRequestedEvent evt)
    {
        if (string.IsNullOrWhiteSpace(evt.SceneName))
        {
            Debug.LogError("[SceneLoader] Scene name is null or empty.", this);
            return;
        }

        if (_isLoading)
        {
            Debug.LogError($"[SceneLoader] Already loading another scene. Request ignored: {evt.SceneName}", this);
            return;
        }

        _hasPendingPostLoadState = evt.HasPostLoadState;
        _pendingPostLoadState = evt.PostLoadState;

        StartCoroutine(LoadSceneAsyncRoutine(evt.SceneName));
    }

    private IEnumerator LoadSceneAsyncRoutine(string sceneName)
    {
        _isLoading = true;
        TimeManager.Instance?.ResetTime();
        GameManager.Instance?.ChangeState(GameState.Loading);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"[SceneLoader] Failed to start loading scene: {sceneName}", this);
            _isLoading = false;
            yield break;
        }

        while (!op.isDone)
        {
            yield return null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TimeManager.Instance?.ResetTime();
        _isLoading = false;
        EventBus.Instance.Publish(new SceneLoadedEvent { SceneName = scene.name });

        if (_hasPendingPostLoadState && GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(_pendingPostLoadState);
        }

        _hasPendingPostLoadState = false;
        _pendingPostLoadState = GameState.None;
    }
}
