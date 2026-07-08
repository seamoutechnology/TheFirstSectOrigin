using UnityEngine;

namespace GameClient.UI
{
    [RequireComponent(typeof(Canvas))]
    public class UIRootRegistrar : MonoBehaviour
    {
        private void Awake()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetCanvasRoot(transform);
                Debug.Log($"[UIRootRegistrar] Đã gán '{gameObject.name}' vào UIManager.canvasRoot.");
            }
            else
            {
                Debug.LogWarning("[UIRootRegistrar] UIManager.Instance chưa sẵn sàng khi UIRoot Awake.");
            }
        }
    }
}
