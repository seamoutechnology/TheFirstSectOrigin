using UnityEngine;

namespace GameClient
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object Lock = new object();

        public static T Instance
        {
            get
            {
                lock (Lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindFirstObjectByType<T>();
                        if (_instance == null)
                        {
                            var obj = new GameObject(typeof(T).Name);
                            _instance = obj.AddComponent<T>();
                        }
                    }
                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (_instance.gameObject.name == typeof(T).Name && _instance.gameObject.transform.childCount == 0)
                {
                    Destroy(_instance.gameObject);
                    _instance = this as T;
                }
                else
                {
                    Destroy(gameObject);
                    return;
                }
            }
            _instance = this as T;
            
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            
            DontDestroyOnLoad(gameObject);
        }
    }
}
