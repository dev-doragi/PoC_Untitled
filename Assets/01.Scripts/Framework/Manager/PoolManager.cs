using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[DefaultExecutionOrder(-71)]
public class PoolManager : Singleton<PoolManager>
{
    [System.Serializable]
    private struct PoolEntry
    {
        public string Key;
        public GameObject Prefab;
        public int InitialSize;
        public int MaxSize;
    }

    [SerializeField] private Transform _poolRoot;
    [SerializeField] private RectTransform _uiPoolRoot;
    [SerializeField] private PoolEntry[] _globalPools;
    [SerializeField] private bool _clearPoolsOnSceneLoaded;

    private readonly Dictionary<string, IObjectPool<GameObject>> _pools = new();
    private readonly Dictionary<string, GameObject> _prefabs = new();

    protected override void OnBootstrap()
    {
        if (_poolRoot == null)
        {
            GameObject root = new GameObject("PoolRoot");
            DontDestroyOnLoad(root);
            _poolRoot = root.transform;
        }

        if (_uiPoolRoot == null)
        {
            GameObject uiRoot = new GameObject("UIPoolRoot", typeof(RectTransform));
            RectTransform rect = uiRoot.GetComponent<RectTransform>();
            rect.SetParent(_poolRoot, false);
            _uiPoolRoot = rect;
        }

        RegisterGlobalPools();

        EventBus.Instance.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<SceneLoadedEvent>(OnSceneLoaded);
    }

    public void RegisterPool(string key, GameObject prefab, int defaultCapacity = 8, int maxSize = 64)
    {
        if (string.IsNullOrWhiteSpace(key) || prefab == null)
        {
            Debug.LogError("[PoolManager] Invalid pool registration request.", this);
            return;
        }

        if (_pools.ContainsKey(key))
        {
            return;
        }

        _prefabs[key] = prefab;
        _pools[key] = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject instance = Instantiate(prefab, _poolRoot);
                instance.name = prefab.name;
                instance.SetActive(false);
                return instance;
            },
            actionOnGet: instance => instance.SetActive(true),
            actionOnRelease: instance =>
            {
                instance.SetActive(false);
                RectTransform rect = instance.GetComponent<RectTransform>();
                if (rect != null && _uiPoolRoot != null)
                {
                    rect.SetParent(_uiPoolRoot, false);
                }
                else
                {
                    instance.transform.SetParent(_poolRoot, false);
                }
            },
            actionOnDestroy: Destroy,
            collectionCheck: false,
            defaultCapacity: Mathf.Max(1, defaultCapacity),
            maxSize: Mathf.Max(defaultCapacity, maxSize));
    }

    public GameObject Spawn(string key, Vector3 position, Quaternion rotation)
    {
        if (!_pools.TryGetValue(key, out IObjectPool<GameObject> pool))
        {
            Debug.LogError($"[PoolManager] Pool key not found: {key}", this);
            return null;
        }

        GameObject instance = pool.Get();
        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }

    public GameObject SpawnUI(string prefabName, RectTransform parent, Vector2 anchoredPosition)
    {
        if (parent == null)
        {
            Debug.LogError("[PoolManager] SpawnUI failed: parent is null.", this);
            return null;
        }

        if (!_pools.TryGetValue(prefabName, out IObjectPool<GameObject> pool))
        {
            Debug.LogError($"[PoolManager] SpawnUI failed: pool key not found ({prefabName}).", this);
            return null;
        }

        GameObject instance = pool.Get();
        RectTransform rect = instance.GetComponent<RectTransform>();
        if (rect == null)
        {
            pool.Release(instance);
            Debug.LogError($"[PoolManager] SpawnUI failed: pooled object is not UI ({prefabName}).", this);
            return null;
        }

        rect.SetParent(parent, false);
        rect.anchoredPosition = anchoredPosition;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        instance.SetActive(true);
        return instance;
    }

    public void Despawn(string key, GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (!_pools.TryGetValue(key, out IObjectPool<GameObject> pool))
        {
            Debug.LogError($"[PoolManager] Pool key not found for despawn: {key}", this);
            return;
        }

        pool.Release(instance);
    }

    public void Despawn(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        string key = instance.name.Replace("(Clone)", string.Empty).Trim();
        if (!_pools.TryGetValue(key, out IObjectPool<GameObject> pool))
        {
            Debug.LogError($"[PoolManager] Pool key not found for despawn: {key}", this);
            return;
        }

        pool.Release(instance);
    }

    private void OnSceneLoaded(SceneLoadedEvent evt)
    {
        if (_clearPoolsOnSceneLoaded)
        {
            ClearAllPools();
        }
    }

    public void ClearAllPools()
    {
        foreach (IObjectPool<GameObject> pool in _pools.Values)
        {
            pool.Clear();
        }

        _pools.Clear();
        _prefabs.Clear();
    }

    private void RegisterGlobalPools()
    {
        if (_globalPools == null)
        {
            return;
        }

        for (int i = 0; i < _globalPools.Length; i++)
        {
            PoolEntry entry = _globalPools[i];
            if (string.IsNullOrWhiteSpace(entry.Key) || entry.Prefab == null)
            {
                continue;
            }

            RegisterPool(entry.Key, entry.Prefab, Mathf.Max(1, entry.InitialSize), Mathf.Max(1, entry.MaxSize));
        }
    }
}
