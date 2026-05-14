using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[DefaultExecutionOrder(-790)]
public class SceneLoader : Singleton<SceneLoader>
{
    private bool _isLoading;

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
        EventBus.Instance.Publish(new SceneLoadRequestedEvent { SceneName = sceneName });
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
    }
}
