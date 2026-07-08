using UnityEngine;

namespace GameClient.Core
{
    public abstract class BaseBehaviour : MonoBehaviour
    {
        protected bool IsInitialized { get; private set; }

        protected virtual void Awake()
        {
            OnInit();
            IsInitialized = true;
        }

        protected virtual void Start()
        {
            OnStart();
        }

        protected virtual void OnDestroy()
        {
            OnCleanup();
        }

        protected virtual void OnInit() { }
        protected virtual void OnStart() { }
        protected virtual void OnCleanup() { }

        protected void Log(string message) => Debug.Log($"[{GetType().Name}] {message}");
        protected void LogWarning(string message) => Debug.LogWarning($"[{GetType().Name}] {message}");
        protected void LogError(string message) => Debug.LogError($"[{GetType().Name}] {message}");

    }
}
