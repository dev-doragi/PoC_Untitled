using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[DefaultExecutionOrder(-740)]
public class PoolManager : Singleton<PoolManager>
{
    [SerializeField] private Transform _poolRoot;
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
                instance.transform.SetParent(_poolRoot);
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
}
