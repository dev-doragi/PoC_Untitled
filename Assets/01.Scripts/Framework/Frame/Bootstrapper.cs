using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class Bootstrapper : MonoBehaviour
{
    [Header("Strict Validation")]
    [SerializeField] private bool _strictMode = true;

    [Header("Core Managers (DDOL)")]
    [SerializeField] private GameManager _gameManagerPrefab;
    [SerializeField] private GameFlowManager _gameFlowManagerPrefab;
    [SerializeField] private InputReader _inputReaderPrefab;
    [SerializeField] private SceneLoader _sceneLoaderPrefab;
    [SerializeField] private TimeManager _timeManagerPrefab;
    [SerializeField] private PauseManager _pauseManagerPrefab;
    [SerializeField] private UIManager _uiManagerPrefab;
    [SerializeField] private SoundManager _soundManagerPrefab;
    [SerializeField] private CameraManager _cameraManagerPrefab;
    [SerializeField] private PoolManager _poolManagerPrefab;

    private void Awake()
    {
        ValidateRequiredPrefabs();

        EnsureInstance(_gameManagerPrefab);
        EnsureInstance(_gameFlowManagerPrefab);
        EnsureInstance(_inputReaderPrefab);
        EnsureInstance(_sceneLoaderPrefab);
        EnsureInstance(_timeManagerPrefab);
        EnsureInstance(_pauseManagerPrefab);
        EnsureInstance(_uiManagerPrefab);
        EnsureInstance(_soundManagerPrefab);
        EnsureInstance(_cameraManagerPrefab);
        EnsureInstance(_poolManagerPrefab);

        BootstrapManagers();
    }

    private void BootstrapManagers()
    {
        BootstrapRequired(GameManager.Instance, nameof(GameManager));
        BootstrapRequired(GameFlowManager.Instance, nameof(GameFlowManager));
        BootstrapRequired(InputReader.Instance, nameof(InputReader));
        BootstrapRequired(SceneLoader.Instance, nameof(SceneLoader));
        BootstrapRequired(TimeManager.Instance, nameof(TimeManager));
        BootstrapRequired(PauseManager.Instance, nameof(PauseManager));
        BootstrapRequired(UIManager.Instance, nameof(UIManager));
        BootstrapRequired(SoundManager.Instance, nameof(SoundManager));
        BootstrapRequired(CameraManager.Instance, nameof(CameraManager));
        BootstrapRequired(PoolManager.Instance, nameof(PoolManager));
    }

    private void BootstrapRequired(ISingletonBootstrap manager, string managerName)
    {
        if (manager == null)
        {
            Debug.LogError($"[Bootstrapper] Missing required manager instance: {managerName}", this);
            return;
        }

        manager.BootstrapIfNeeded();
    }

    private void ValidateRequiredPrefabs()
    {
        if (!_strictMode)
        {
            return;
        }

        ValidateRequiredPrefab(_gameManagerPrefab, nameof(_gameManagerPrefab));
        ValidateRequiredPrefab(_gameFlowManagerPrefab, nameof(_gameFlowManagerPrefab));
        ValidateRequiredPrefab(_inputReaderPrefab, nameof(_inputReaderPrefab));
        ValidateRequiredPrefab(_sceneLoaderPrefab, nameof(_sceneLoaderPrefab));
        ValidateRequiredPrefab(_timeManagerPrefab, nameof(_timeManagerPrefab));
        ValidateRequiredPrefab(_pauseManagerPrefab, nameof(_pauseManagerPrefab));
        ValidateRequiredPrefab(_uiManagerPrefab, nameof(_uiManagerPrefab));
        ValidateRequiredPrefab(_soundManagerPrefab, nameof(_soundManagerPrefab));
        ValidateRequiredPrefab(_cameraManagerPrefab, nameof(_cameraManagerPrefab));
        ValidateRequiredPrefab(_poolManagerPrefab, nameof(_poolManagerPrefab));
    }

    private void ValidateRequiredPrefab(Object prefab, string fieldName)
    {
        if (prefab == null)
        {
            Debug.LogError($"[Bootstrapper] Required prefab is missing: {fieldName}", this);
        }
    }

    private void EnsureInstance<T>(T prefab) where T : MonoBehaviour
    {
        if (prefab == null)
        {
            return;
        }

        if (FindAnyObjectByType<T>() != null)
        {
            return;
        }

        Instantiate(prefab);
    }
}
