using UnityEngine;

[DefaultExecutionOrder(-100)]
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
    [SerializeField] private SoundManager _soundManagerPrefab;
    [SerializeField] private PoolManager _poolManagerPrefab;
    [SerializeField] private HourglassCombatManager _hourglassCombatManagerPrefab;

    private void Awake()
    {
        ValidateRequiredPrefabs();

        EnsureInstance(_gameManagerPrefab);
        EnsureInstance(_gameFlowManagerPrefab);
        EnsureInstance(_inputReaderPrefab);
        EnsureInstance(_sceneLoaderPrefab);
        EnsureInstance(_timeManagerPrefab);
        EnsureInstance(_pauseManagerPrefab);
        EnsureInstance(_soundManagerPrefab);
        EnsureInstance(_poolManagerPrefab);
        EnsureInstance(_hourglassCombatManagerPrefab);
    }

    private void Start()
    {
        BootstrapManagers();
    }

    private void BootstrapManagers()
    {
        bool success = true;

        success &= BootstrapRequired(GameManager.Instance, nameof(GameManager));
        success &= BootstrapRequired(GameFlowManager.Instance, nameof(GameFlowManager));
        success &= BootstrapRequired(InputReader.Instance, nameof(InputReader));
        success &= BootstrapRequired(SceneLoader.Instance, nameof(SceneLoader));
        success &= BootstrapRequired(TimeManager.Instance, nameof(TimeManager));
        success &= BootstrapRequired(PauseManager.Instance, nameof(PauseManager));
        success &= BootstrapOptional(FindAnyObjectByType<UIManager>(), nameof(UIManager));
        success &= BootstrapRequired(SoundManager.Instance, nameof(SoundManager));
        success &= BootstrapRequired(PoolManager.Instance, nameof(PoolManager));
        success &= BootstrapRequired(HourglassCombatManager.Instance, nameof(HourglassCombatManager));

        if (success)
        {
            Debug.Log("<color=green>[Bootstrapper]</color> manager bootstrapping completed.");
        }
        else
        {
            Debug.LogWarning("<color=yellow>[Bootstrapper]</color> manager bootstrapping completed with missing managers.");
        }
    }

    private bool BootstrapRequired(ISingletonBootstrap manager, string managerName)
    {
        if (manager == null)
        {
            Debug.LogError($"[Bootstrapper] Missing required manager instance: {managerName}", this);
            return false;
        }

        manager.BootstrapIfNeeded();
        return true;
    }

    private bool BootstrapOptional(ISingletonBootstrap manager, string managerName)
    {
        if (manager == null)
        {
            Debug.LogWarning($"[Bootstrapper] Optional manager instance not found: {managerName}", this);
            return true;
        }

        manager.BootstrapIfNeeded();
        return true;
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
        //ValidateRequiredPrefab(_uiManagerPrefab, nameof(_uiManagerPrefab));
        ValidateRequiredPrefab(_soundManagerPrefab, nameof(_soundManagerPrefab));
        //ValidateRequiredPrefab(_cameraManagerPrefab, nameof(_cameraManagerPrefab));
        ValidateRequiredPrefab(_poolManagerPrefab, nameof(_poolManagerPrefab));
        ValidateRequiredPrefab(_hourglassCombatManagerPrefab, nameof(_hourglassCombatManagerPrefab));
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
