using System.Collections.Generic;
using UnityEngine;

namespace TFSO.Core
{
    public class ObjectPool : Singleton<ObjectPool>
    {
        private Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();

        public GameObject Get(GameObject prefab)
        {
            string key = prefab.name;
            if (!_pools.ContainsKey(key))
            {
                _pools[key] = new Queue<GameObject>();
            }

            if (_pools[key].Count > 0)
            {
                GameObject obj = _pools[key].Dequeue();
                obj.SetActive(true);
                return obj;
            }

            return Instantiate(prefab);
        }

        public void ReturnToPool(GameObject obj)
        {
            string key = obj.name.Replace("(Clone)", "").Trim();
            obj.SetActive(false);
            if (!_pools.ContainsKey(key))
            {
                _pools[key] = new Queue<GameObject>();
            }
            _pools[key].Enqueue(obj);
        }
    }
}
