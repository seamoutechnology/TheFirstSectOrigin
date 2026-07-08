using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace GameClient.Managers
{
    public class ObjectPoolManager : GameClient.Singleton<ObjectPoolManager>
    {
        private readonly Dictionary<string, ObjectPool<GameObject>> _pools = new();
        private readonly Dictionary<string, GameObject> _prefabs = new();

        private Transform _poolRoot;

        protected override void Awake()
        {
            base.Awake();
            _poolRoot = new GameObject("ObjectPoolRoot").transform;
            _poolRoot.SetParent(this.transform);
        }

        public void RegisterPool(string poolKey, GameObject prefab, int defaultCapacity = 10, int maxSize = 100)
        {
            if (_pools.ContainsKey(poolKey)) return;

            _prefabs[poolKey] = prefab;
            
            var pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var obj = Instantiate(prefab, _poolRoot);
                    obj.name = $"{prefab.name}_{System.Guid.NewGuid().ToString().Substring(0, 5)}";
                    return obj;
                },
                actionOnGet: (obj) =>
                {
                    obj.SetActive(true);
                },
                actionOnRelease: (obj) =>
                {
                    obj.SetActive(false);
                    obj.transform.SetParent(_poolRoot);
                },
                actionOnDestroy: (obj) =>
                {
                    Destroy(obj);
                },
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );

            _pools[poolKey] = pool;
        }

        public GameObject Get(string poolKey, Transform parent = null)
        {
            if (!_pools.TryGetValue(poolKey, out var pool))
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool {poolKey} is not registered!");
                return null;
            }

            var obj = pool.Get();
            if (parent != null)
            {
                obj.transform.SetParent(parent, false);
                obj.transform.localScale = Vector3.one; // Khắc phục lỗi UI bị scale khổng lồ
            }
            return obj;
        }

        public void Release(string poolKey, GameObject obj)
        {
            if (!_pools.TryGetValue(poolKey, out var pool))
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool {poolKey} is not registered, destroying object instead.");
                Destroy(obj);
                return;
            }

            pool.Release(obj);
        }

        public void ClearPool(string poolKey)
        {
            if (_pools.TryGetValue(poolKey, out var pool))
            {
                pool.Clear();
                _pools.Remove(poolKey);
                _prefabs.Remove(poolKey);
            }
        }
    }
}
